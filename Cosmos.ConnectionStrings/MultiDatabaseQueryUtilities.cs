// <copyright file="ConnectionStringProvider.cs" company="Moonrise Software, LLC">
// Copyright (c) Moonrise Software, LLC. All rights reserved.
// Licensed under the GNU Public License, Version 3.0 (https://www.gnu.org/licenses/gpl-3.0.html)
// See https://github.com/MoonriseSoftwareCalifornia/CosmosCMS
// for more information concerning the license and the contributors participating to this project.
// </copyright>

namespace Cosmos.DynamicConfig
{
    using Microsoft.Azure.Cosmos;
    using Microsoft.EntityFrameworkCore;

    /// <summary>
    /// Utility class for querying and updating multiple Cosmos DB databases based on the configured connections.
    /// </summary>
    public class MultiDatabaseManagementUtilities
    {
        private readonly DynamicConfigDbContext dbContext;

        /// <summary>
        /// Initializes a new instance of the <see cref="MultiDatabaseManagementUtilities"/> class with the provided database context.
        /// </summary>
        /// <param name="dbContext"></param>
        /// <exception cref="ArgumentNullException"></exception>
        public MultiDatabaseManagementUtilities(DynamicConfigDbContext dbContext)
        {
            this.dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        }

        /// <summary>
        /// Retrieves a list of unique domains associated with the provided email address across all configured Cosmos DB connections.
        /// </summary>
        /// <param name="emailAddress">Email address to search.</param>
        /// <returns>Domains where email address was found.</returns>
        public async Task<List<string>> GetDomainsByEmailAddress(string emailAddress)
        {
            if(string.IsNullOrWhiteSpace(emailAddress))
            {
                throw new ArgumentException("Email address cannot be null or empty.", nameof(emailAddress));
            }
            // Normalize the email address by trimming whitespace and converting to uppercase
            emailAddress = emailAddress.Trim().ToUpper();
            var domains = new List<string>();
            // Retrieve all connections from the database context
            var connections = await GetConnections();
            // Iterate through each connection to query the Cosmos DB
            foreach (var connection in connections)
            {
                using var client = GetCosmosClient(connection);
                var container = client.GetContainer(connection.DbName, "Identity");
                var query = new QueryDefinition($"SELECT TOP 1 c.Id FROM c WHERE c.NormalizedEmail = '{emailAddress}'");
                using var iterator = container.GetItemQueryIterator<dynamic>(query);
                if (iterator.HasMoreResults)
                {
                    var response = await iterator.ReadNextAsync();
                    if (response.Any())
                    {
                        domains.AddRange(connection.WebsiteUrl);
                    }
                }
            }
            return domains.Distinct().ToList();
        }

        private async Task<List<Connection>> GetConnections()
        {
            return await dbContext.Connections.AsNoTracking().ToListAsync();
        }

        private CosmosClient GetCosmosClient(Connection connection)
        {
            return new CosmosClient(connection.DbConn, connection.DbName);
        }
    }
}

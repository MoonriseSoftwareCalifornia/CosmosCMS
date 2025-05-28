// <copyright file="Class.cs" company="Moonrise Software, LLC">
// Copyright (c) Moonrise Software, LLC. All rights reserved.
// Licensed under the GNU Public License, Version 3.0 (https://www.gnu.org/licenses/gpl-3.0.html)
// See https://github.com/MoonriseSoftwareCalifornia/CosmosCMS
// for more information concerning the license and the contributors participating to this project.
// </copyright>

namespace Cosmos.Editor.Data
{
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Microsoft.Azure.Cosmos;

    /// <summary>
    /// A service class to interact with Cosmos DB.
    /// </summary>
    public class CosmosDbService
    {
        /// <summary>
        /// The Cosmos DB container.
        /// </summary>
        private readonly Container container;

        /// <summary>
        /// Initializes a new instance of the <see cref="CosmosDbService"/> class.
        /// </summary>
        /// <param name="cosmosClient">Cosmos DB Client.</param>
        /// <param name="databaseName">Database name.</param>
        /// <param name="containerName">Container name.</param>
        public CosmosDbService(CosmosClient cosmosClient, string databaseName, string containerName)
        {
            container = cosmosClient.GetContainer(databaseName, containerName);
        }

        /// <summary>  
        /// Query the Cosmos DB container and returns a dynamic list of results.  
        /// </summary>  
        /// <param name="sqlQuery">SQL Query.</param>  
        /// <returns>Returned dataset.</returns>  
        public async Task<List<dynamic>> QueryWithGroupByAsync(string sqlQuery)
        {
            var queryDefinition = new QueryDefinition(sqlQuery);
            var queryResultSetIterator = container.GetItemQueryIterator<dynamic>(queryDefinition);

            var results = new List<dynamic>();
            while (queryResultSetIterator.HasMoreResults)
            {
                var response = await queryResultSetIterator.ReadNextAsync();
                results.AddRange(response);
            }

            return results;
        }

        /// <summary>  
        /// Query the Cosmos DB container and returns a dynamic list of results.  
        /// </summary>
        /// <typeparam name="T">The type of the object to be returned.</typeparam>
        /// <param name="sqlQuery">SQL Query.</param>  
        /// <returns>Returned dataset.</returns>
        public async Task<List<T>> QueryWithGroupByAsync<T>(string sqlQuery)
        {
            var queryDefinition = new QueryDefinition(sqlQuery);
            var queryResultSetIterator = container.GetItemQueryIterator<T>(queryDefinition);

            var results = new List<T>();
            while (queryResultSetIterator.HasMoreResults)
            {
                var response = await queryResultSetIterator.ReadNextAsync();
                results.AddRange(response);
            }

            return results;
        }
    }
}

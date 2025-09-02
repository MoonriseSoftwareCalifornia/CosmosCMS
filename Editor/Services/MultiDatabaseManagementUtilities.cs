// <copyright file="MultiDatabaseManagementUtilities.cs" company="Moonrise Software, LLC">
// Copyright (c) Moonrise Software, LLC. All rights reserved.
// Licensed under the GNU Public License, Version 3.0 (https://www.gnu.org/licenses/gpl-3.0.html)
// See https://github.com/MoonriseSoftwareCalifornia/CosmosCMS
// for more information concerning the license and the contributors participating to this project.
// </copyright>

namespace Cosmos.Editor.Services
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Cosmos.Common.Data;
    using Cosmos.DynamicConfig;
    using Microsoft.AspNetCore.Identity;
    using Microsoft.Azure.Cosmos;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.Configuration;

    /// <summary>
    /// Utility class for querying and updating multiple Cosmos DB databases based on the configured connections.
    /// </summary>
    public class MultiDatabaseManagementUtilities
    {
        private readonly string connectionString;
        private readonly bool isMultiTenant;

        /// <summary>
        /// Gets a value indicating whether the application is configured for multi-tenancy.
        /// </summary>
        public bool IsMultiTenant => isMultiTenant;

        /// <summary>
        /// Initializes a new instance of the <see cref="MultiDatabaseManagementUtilities"/> class with the provided database context.
        /// </summary>
        /// <param name="configuration">Application configuration.</param>
        public MultiDatabaseManagementUtilities(IConfiguration configuration)
        {
            this.connectionString = configuration.GetConnectionString("ConfigDbConnectionString");
            this.isMultiTenant = configuration.GetValue<bool?>("MultiTenantEditor") ?? false;
        }

        /// <summary>
        /// Retrieves a list of unique domains associated with the provided email address across all configured Cosmos DB connections.
        /// </summary>
        /// <param name="emailAddress">Email address to search.</param>
        /// <returns>Domains where email address was found.</returns>
        public async Task<List<string>> GetDomainsByEmailAddress(string emailAddress)
        {
            if (!isMultiTenant)
            {
                return new List<string>(); // Go no further if not multi-tenant.
            }

            if (string.IsNullOrWhiteSpace(emailAddress))
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
                using var dbContext = new ApplicationDbContext(connection.DbConn);
                var user = await dbContext.Users.FirstOrDefaultAsync(u => u.NormalizedEmail == emailAddress);

                if (user == null)
                {
                    continue; // No user found in this database, skip to the next connection.
                }

                domains.Add(connection.WebsiteUrl);
            }

            return domains.Distinct().ToList();
        }

        /// <summary>
        /// Updates the identity user across all Cosmos DB connections where the user exists.
        /// </summary>
        /// <param name="identityUser">Identity user entity.</param>
        /// <returns>Task.</returns>
        /// <exception cref="ArgumentNullException">Identity user is null.</exception>
        public async Task UpdateIdentityUser(IdentityUser identityUser)
        {
            if (!isMultiTenant)
            {
                return; // Go no further.
            }

            if (identityUser == null)
            {
                throw new ArgumentNullException(nameof(identityUser), "Identity user cannot be null.");
            }

            // Retrieve all connections from the database context for the user.
            var connections = await GetConnectionsForEmailAddress(identityUser.Email);

            // Iterate through each connection to update the user in Cosmos DB
            foreach (var connection in connections)
            {
                using var applicationDbContext = new ApplicationDbContext(connection.DbConn);

                var identity = await applicationDbContext.Users
                    .FirstOrDefaultAsync(u => u.NormalizedEmail == identityUser.NormalizedEmail);

                if (identity != null && identity.Id != identityUser.Id)
                {
                    identity.UserName = identityUser.UserName;
                    identity.NormalizedUserName = identityUser.NormalizedUserName;
                    identity.Email = identityUser.Email;
                    identity.NormalizedEmail = identityUser.NormalizedEmail;
                    identity.PhoneNumber = identityUser.PhoneNumber;
                    identity.PhoneNumberConfirmed = identityUser.PhoneNumberConfirmed;
                    identity.TwoFactorEnabled = identityUser.TwoFactorEnabled;
                    identity.LockoutEnabled = identityUser.LockoutEnabled;
                    identity.LockoutEnd = identityUser.LockoutEnd;
                    identity.AccessFailedCount = identityUser.AccessFailedCount;
                    identity.SecurityStamp = identityUser.SecurityStamp;
                    identity.ConcurrencyStamp = identityUser.ConcurrencyStamp;
                    identity.EmailConfirmed = identityUser.EmailConfirmed;
                    identity.PasswordHash = identityUser.PasswordHash;

                    await applicationDbContext.SaveChangesAsync();
                }
            }
        }

        /// <summary>
        /// Retrieves all connections from the dynamic configuration database.
        /// </summary>
        /// <returns>Gets the complete list of connections.</returns>
        public async Task<List<Connection>> GetConnections()
        {
            var options = new DbContextOptionsBuilder<DynamicConfigDbContext>()
                    .UseCosmos(connectionString, databaseName: "configs")
                    .Options;
            using var dbContext = new DynamicConfigDbContext(options);
            return await dbContext.Connections.ToListAsync();
        }

        /// <summary>
        ///  Ensures that all databases are configured and created if they do not exist.
        /// </summary>
        /// <returns>Task.</returns>
        public async Task EnsureDatabasesAreConfigured()
        {
            if (!isMultiTenant)
            {
                return; // Go no further if not multi-tenant.
            }

            var connections = await GetConnections();
            foreach (var connection in connections)
            {
                using var dbContext = new ApplicationDbContext(connection.DbConn);

                try
                {
                    //var test = await dbContext.Users.CountAsync();
                }
                catch (CosmosException ex)
                {
                    // Handle exceptions as needed, e.g., log them.
                    Console.WriteLine($"Error configuring database for {connection.WebsiteUrl}: {ex.Message}");
                }
            }
        }

        private async Task<List<Connection>> GetConnectionsForEmailAddress(string emailAddress)
        {
            if (string.IsNullOrWhiteSpace(emailAddress))
            {
                throw new ArgumentException("Email address cannot be null or empty.", nameof(emailAddress));
            }

            // Normalize the email address by trimming whitespace and converting to uppercase.
            emailAddress = emailAddress.Trim().ToUpper();

            // Create a list to hold connections where the email address is found.
            var userConnections = new List<Connection>();

            // Retrieve all connections from the database context.
            var connections = await GetConnections();

            // Iterate through each connection to query the Cosmos DB.
            foreach (var connection in connections)
            {
                using var applicationDbContext = new ApplicationDbContext(connection.DbConn);
                var user = await applicationDbContext.Users.FirstOrDefaultAsync(u => u.NormalizedEmail == emailAddress);
                if (user == null)
                {
                    continue; // No user found in this database, skip to the next connection.
                }

                userConnections.Add(connection);
            }

            return userConnections;
        }
    }
}

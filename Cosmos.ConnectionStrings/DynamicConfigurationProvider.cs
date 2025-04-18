// <copyright file="ConnectionStringProvider.cs" company="Moonrise Software, LLC">
// Copyright (c) Moonrise Software, LLC. All rights reserved.
// Licensed under the GNU Public License, Version 3.0 (https://www.gnu.org/licenses/gpl-3.0.html)
// See https://github.com/MoonriseSoftwareCalifornia/CosmosCMS
// for more information concerning the license and the contributors participating to this project.
// </copyright>

namespace Cosmos.DynamicConfig
{
    using Microsoft.AspNetCore.Http;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.Configuration;

    /// <summary>
    /// Gets connection strings and configuration values from the configuration file.
    /// </summary>
    /// <remarks>
    /// If in a multi-tenant environment, the connection string names are prefixed by the domain name.
    /// </remarks>
    public class DynamicConfigurationProvider : IDynamicConfigurationProvider
    {
        private readonly IConfiguration configuration;
        private readonly IHttpContextAccessor httpContextAccessor;
        private readonly bool isMultiTenantEditor;
        /// <summary>
        /// Gets the database connection
        /// </summary>
        private readonly Connection? connection;

        /// <summary>
        /// Gets a value indicating whether the connection is configured for multi-tenant.
        /// </summary>
        public bool IsMultiTenantConfigured { get { return connection != null; } }

        /// <summary>
        /// Initializes a new instance of the <see cref="ConnectionStringProvider"/> class.
        /// </summary>
        /// <param name="configuration">Connection configuration.</param>
        /// <param name="httpContextAccessor">HTTP context accessor.</param>
        /// <param name="logger">Log service.</param>
        public DynamicConfigurationProvider(
            IConfiguration configuration,
            IHttpContextAccessor httpContextAccessor)
        {
            this.configuration = configuration;
            this.httpContextAccessor = httpContextAccessor;
            isMultiTenantEditor = this.configuration.GetValue<bool?>("MultiTenantEditor") ?? false;
            if (isMultiTenantEditor && Domain != null)
            {
                var connectionString = this.configuration.GetConnectionString("ConfigDbConnectionString");
                if (string.IsNullOrEmpty(connectionString))
                {
                    throw new Exception("connectionString is not set.");
                }
                var options = new DbContextOptionsBuilder<DynamicConfigDbContext>()
                    .UseCosmos(connectionString, databaseName: "configs")
                    .Options;

                using var dbContext = new DynamicConfigDbContext(options);
                var result = dbContext.Connections.FirstOrDefaultAsync(w => w.DomainName == httpContextAccessor.HttpContext.Request.Host.Host.ToLower());
                result.Wait();
                if (result.Result != null)
                {
                    this.connection = result.Result;
                }
            }
        }

        /// <summary>
        /// Gets the domain name from the HTTP context.
        /// </summary>
        private string Domain
        {
            get
            {
                return httpContextAccessor.HttpContext.Request.Host.Host ?? string.Empty;
            }
        }

        /// <summary>
        /// Gets the database name.
        /// </summary>
        /// <returns>Database name.</returns>
        public string? GetDatabaseName()
        {
            if (this.isMultiTenantEditor)
            {
                return this.connection?.DbName;
            }
            return configuration.GetValue<string>($"CosmosIdentityDbName") ?? "cosmoscms";
        }

        /// <summary>
        /// Gets the database connection string.
        /// </summary>
        /// <returns>Database connection string.</returns>
        public string? GetDatabaseConnectionString()
        {
            if (this.isMultiTenantEditor)
            {
                return this.connection?.DbConn;
            }
            return configuration.GetConnectionString("ApplicationDbContextConnection");
        }

        /// <summary>
        /// Gets the storage connection string.
        /// </summary>
        /// <returns>Database connection string.</returns>
        public string? GetStorageConnectionString()
        {
            if (this.isMultiTenantEditor)
            {
                return this.connection?.StorageConn;
            }
            return configuration.GetConnectionString("AzureBlobStorageConnectionString");
        }

        /// <summary>
        /// Gets the configuration value.
        /// </summary>
        /// <param name="key">Key name.</param>
        /// <returns>Key value.</returns>
        public string? GetConfigurationValue(string key)
        {
            return configuration.GetValue<string>(key);
        }

        /// <summary>
        /// Gets the connection string by its name.
        /// </summary>
        /// <param name="name">Connection string name.</param>
        /// <returns>Database connection string.</returns>
        public string? GetConnectionStringByName(string name)
        {
            return configuration.GetConnectionString(name);
        }

        /// <summary>
        /// Saves the connection information to the database.
        /// </summary>
        /// <param name="connection"></param>
        public void SaveConnection(Connection connection)
        {
            var options = new DbContextOptionsBuilder<DynamicConfigDbContext>()
                .UseCosmos(connection.DbConn, databaseName: "configs")
                .Options;
            using var dbContext = new DynamicConfigDbContext(options);
            var existingConnection = dbContext.Connections.FirstOrDefault(w => w.DomainName == connection.DomainName);
            if (existingConnection != null)
            {
                dbContext.Connections.Remove(existingConnection);
            }
            connection.DomainName = Domain.ToLower(); // Set the domain name to the current domain
            dbContext.Connections.Add(connection);
            dbContext.SaveChanges();
        }
    }
}

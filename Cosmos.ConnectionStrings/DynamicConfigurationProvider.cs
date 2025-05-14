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
    using Microsoft.Extensions.Caching.Memory;
    using Microsoft.Extensions.Configuration;
    using System.Text;

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
        private readonly IMemoryCache memoryCache;
        private readonly bool isMultiTenantEditor;
        private readonly StringBuilder errorMessages = new StringBuilder();
        private readonly string? connectionString;

        /// <summary>
        /// Gets the database connection
        /// </summary>
        //private readonly Connection? connection;

        /// <summary>
        /// Gets a value indicating whether the connection is configured for multi-tenant.
        /// </summary>
        public bool IsMultiTenantConfigured { get { return GetTenantConnection() != null; } }

        /// <summary>
        /// Gets a value indicating the error messages that may exist.
        /// </summary>
        public string ErrorMesages => errorMessages.ToString();

        /// <summary>
        /// Initializes a new instance of the <see cref="ConnectionStringProvider"/> class.
        /// </summary>
        /// <param name="configuration">Connection configuration.</param>
        /// <param name="httpContextAccessor">HTTP context accessor.</param>
        /// <param name="logger">Log service.</param>
        /// <param name="memoryCache">Memory cache.</param>
        public DynamicConfigurationProvider(
            IConfiguration configuration,
            IHttpContextAccessor httpContextAccessor,
            IMemoryCache memoryCache)
        {
            this.configuration = configuration;
            this.httpContextAccessor = httpContextAccessor;
            this.memoryCache = memoryCache;
            isMultiTenantEditor = this.configuration.GetValue<bool?>("MultiTenantEditor") ?? false;
            var domainName = Domain.ToLower(); // Get the domain name from the HTTP context.
            if (isMultiTenantEditor && !string.IsNullOrWhiteSpace(domainName))
            {
                connectionString = this.configuration.GetConnectionString("ConfigDbConnectionString") ?? string.Empty;
            }
        }

        /// <summary>
        /// Gets the domain name from the HTTP context.
        /// </summary>
        private string Domain
        {
            get
            {
                // Attempt to retrieve the domain name from a custom cookie
                var cookieName = "CosmosWebsiteDomain"; // Replace with your custom cookie name
                var cookieValue = httpContextAccessor.HttpContext?.Request.Cookies[cookieName];

                if (!string.IsNullOrWhiteSpace(cookieValue))
                {
                    return cookieValue.ToLower();
                }

                // Fallback to retrieving the domain name from the HTTP context host
                return httpContextAccessor.HttpContext.Request.Host.Host?.ToLower() ?? string.Empty;
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
                return GetTenantConnection()?.DbName;
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
                return GetTenantConnection()?.DbConn;
            }
            return configuration.GetConnectionString("ApplicationDbContextConnection");
        }

        /// <summary>
        /// Gets the storage connection string.
        /// </summary>
        /// <returns>Database connection string.</returns>
        public string? GetStorageConnectionString()
        {
            var con =  GetTenantConnection();
            if (this.isMultiTenantEditor && con != null && !string.IsNullOrEmpty(con.StorageConn))
            {
                return con?.StorageConn;
            }
            return configuration.GetConnectionString("DataProtectionStorage");
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

        private Connection? GetTenantConnection()
        {
            if (string.IsNullOrEmpty(Domain))
            {
                var connectionString = configuration.GetConnectionString("ConfigDbConnectionString");
                var storageConnection = configuration.GetConnectionString("AzureBlobStorageConnectionString");
                return new Connection()
                {
                    Customer = "Moonrise Software LLC",
                    DomainNames = new[] { "www.moonrise.net" },
                    DbConn = connectionString,
                    DbName = "cosmoscms",
                    StorageConn = storageConnection
                };
            }

            if (string.IsNullOrWhiteSpace(connectionString))
            {
                return null;
            }

            memoryCache.TryGetValue<Connection>(Domain, out var connection);

            if (connection == null)
            {
                var options = new DbContextOptionsBuilder<DynamicConfigDbContext>()
                        .UseCosmos(connectionString, databaseName: "configs")
                        .Options;

                using var dbContext = new DynamicConfigDbContext(options);
                var result = dbContext.Connections.FirstOrDefaultAsync(c => c.DomainNames.Any(a => a == Domain));
                result.Wait();
                connection = result.Result;
                if (connection != null)
                {
                    memoryCache.Set(Domain, connection, TimeSpan.FromMinutes(20));
                }
            }

            return connection;
        }
    }
}

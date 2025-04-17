// <copyright file="ConnectionStringProvider.cs" company="Moonrise Software, LLC">
// Copyright (c) Moonrise Software, LLC. All rights reserved.
// Licensed under the GNU Public License, Version 3.0 (https://www.gnu.org/licenses/gpl-3.0.html)
// See https://github.com/MoonriseSoftwareCalifornia/CosmosCMS
// for more information concerning the license and the contributors participating to this project.
// </copyright>

namespace Cosmos.DynamicConfig
{
    using Microsoft.AspNetCore.Http;
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
        }

        /// <summary>
        /// Gets the domain name from the HTTP context.
        /// </summary>
        private string? Domain
        {
            get
            {
                return (httpContextAccessor.HttpContext.Request.Host.Host)?.ToLower().Replace('.', '-');
            }
        }

        /// <summary>
        /// Gets the database name.
        /// </summary>
        /// <returns>Database name.</returns>
        public string GetDatabaseName()
        {
            if (this.isMultiTenantEditor)
            {
                return configuration.GetValue<string>($"{Domain}-dbname") ?? "cosmoscms";
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
                var connString = configuration.GetConnectionString($"{Domain}-db");
                if (string.IsNullOrEmpty(connString))
                {
                    var keys = configuration.AsEnumerable().Select(keys => keys.Key).Where(w => w.StartsWith("ConnectionStrings", StringComparison.CurrentCultureIgnoreCase)).ToArray();
                    var keyString = string.Join(", ", keys);

                    throw new Exception($"Connection string for domain connection string '{Domain}-db' not found. These keys were found: ${keyString}");
                }
                return connString;
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
                var connString = configuration.GetConnectionString($"{Domain}-st");
                if (string.IsNullOrEmpty(connString))
                {
                    throw new Exception($"Connection string for domain connection string '{Domain}-st' not found.");
                }
                return connString;
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
    }
}

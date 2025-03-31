// <copyright file="ConnectionStringProvider.cs" company="Moonrise Software, LLC">
// Copyright (c) Moonrise Software, LLC. All rights reserved.
// Licensed under the GNU Public License, Version 3.0 (https://www.gnu.org/licenses/gpl-3.0.html)
// See https://github.com/MoonriseSoftwareCalifornia/CosmosCMS
// for more information concerning the license and the contributors participating to this project.
// </copyright>

namespace Cosmos.Common.Services
{
    using Microsoft.Extensions.Configuration;

    /// <summary>
    /// Connection string provider.
    /// </summary>
    public class ConnectionStringProvider : IConnectionStringProvider
    {
        private readonly IConfiguration configuration;

        /// <summary>
        /// Initializes a new instance of the <see cref="ConnectionStringProvider"/> class.
        /// </summary>
        /// <param name="configuration">Connection configuration.</param>
        public ConnectionStringProvider(IConfiguration configuration)
        {
            this.configuration = configuration;
        }

        /// <summary>
        /// Gets the configuration value.
        /// </summary>
        /// <param name="key">Key name.</param>
        /// <returns>Key value.</returns>
        public string GetConfigurationValue(string key)
        {
            return configuration.GetValue<string>(key);
        }

        /// <summary>
        /// Gets the database name by domain name.
        /// </summary>
        /// <param name="domain">Domain name.</param>
        /// <returns>Database name.</returns>
        public string GetDatabaseNameByDomain(string domain)
        {
            return configuration.GetValue<string>($"{domain.Replace('.', '-')}-dbname") ?? "cosmoscms";
        }

        /// <summary>
        /// Gets the connection string based on the domain.
        /// </summary>
        /// <param name="domain">domain name.</param>
        /// <returns>Database connection string.</returns>
        public string GetDatabaseConnectionStringByDomain(string domain)
        {
            // Example: Fetch connection string from configuration based on domain
            return configuration.GetConnectionString($"{domain.Replace('.', '-')}-db");
        }

        /// <summary>
        /// Gets the connection string based on the domain.
        /// </summary>
        /// <param name="domain">domain name.</param>
        /// <returns>Database connection string.</returns>
        public string GetStorageConnectionStringByDomain(string domain)
        {
            // Example: Fetch connection string from configuration based on domain
            return configuration.GetConnectionString($"{domain.Replace('.', '-')}-st");
        }

        /// <summary>
        /// Gets the connection string by its name.
        /// </summary>
        /// <param name="name">Connection string name.</param>
        /// <returns>Database connection string.</returns>
        public string GetConnectionStringByName(string name)
        {
            return configuration.GetConnectionString(name);
        }
    }
}

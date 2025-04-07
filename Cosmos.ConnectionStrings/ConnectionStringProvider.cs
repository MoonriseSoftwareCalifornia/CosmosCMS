// <copyright file="ConnectionStringProvider.cs" company="Moonrise Software, LLC">
// Copyright (c) Moonrise Software, LLC. All rights reserved.
// Licensed under the GNU Public License, Version 3.0 (https://www.gnu.org/licenses/gpl-3.0.html)
// See https://github.com/MoonriseSoftwareCalifornia/CosmosCMS
// for more information concerning the license and the contributors participating to this project.
// </copyright>

namespace Cosmos.ConnectionStrings
{
    using Microsoft.AspNetCore.Http;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.Logging;

    /// <summary>
    /// Connection string provider.
    /// </summary>
    public class ConnectionStringProvider : IConnectionStringProvider
    {
        private readonly IConfiguration configuration;
        private readonly IHttpContextAccessor httpContextAccessor;
        private readonly ILogger<ConnectionStringProvider> logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="ConnectionStringProvider"/> class.
        /// </summary>
        /// <param name="configuration">Connection configuration.</param>
        /// <param name="httpContextAccessor">HTTP context accessor.</param>
        /// <param name="logger">Log service.</param>
        public ConnectionStringProvider(IConfiguration configuration, IHttpContextAccessor httpContextAccessor, ILogger<ConnectionStringProvider> logger)
        {
            this.configuration = configuration;
            this.httpContextAccessor = httpContextAccessor;
            this.logger = logger;
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
        /// Gets the database name by domain name.
        /// </summary>
        /// <returns>Database name.</returns>
        public string? GetDatabaseNameByDomain()
        {
            return configuration.GetValue<string>($"{Domain}-dbname") ?? "cosmoscms";
        }

        /// <summary>
        /// Gets the connection string based on the domain.
        /// </summary>
        /// <returns>Database connection string.</returns>
        public string? GetDatabaseConnectionStringByDomain()
        {
            var connString = configuration.GetConnectionString($"{Domain}-db");
            if (string.IsNullOrEmpty(connString))
            {
                throw new Exception($"Connection string for domain connection string '{Domain}-db' not found.");
            }
            return connString;
        }

        /// <summary>
        /// Gets the connection string based on the domain.
        /// </summary>
        /// <returns>Database connection string.</returns>
        public string? GetStorageConnectionStringByDomain()
        {
            var connString = configuration.GetConnectionString($"{Domain}-st");
            if (string.IsNullOrEmpty(connString))
            {
                throw new Exception($"Connection string for domain connection string '{Domain}-st' not found.");
            }
            return connString;
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

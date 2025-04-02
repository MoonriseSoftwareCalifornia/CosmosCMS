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

    /// <summary>
    /// Connection string provider.
    /// </summary>
    public class ConnectionStringProvider : IConnectionStringProvider
    {
        private readonly IConfiguration configuration;
        private readonly IHttpContextAccessor httpContextAccessor;

        /// <summary>
        /// Initializes a new instance of the <see cref="ConnectionStringProvider"/> class.
        /// </summary>
        /// <param name="configuration">Connection configuration.</param>
        /// <param name="httpContextAccessor">HTTP context accessor.</param>
        public ConnectionStringProvider(IConfiguration configuration, IHttpContextAccessor httpContextAccessor)
        {
            this.configuration = configuration;
            this.httpContextAccessor = httpContextAccessor;
        }

        /// <summary>
        /// Gets the domain name from the HTTP context.
        /// </summary>
        private string? Domain
        {
            get
            {
                return (httpContextAccessor.HttpContext.Items["Domain"] as string)?.ToLower().Replace('.', '-');
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
            // Example: Fetch connection string from configuration based on domain
            return configuration.GetConnectionString($"{Domain}-db");
        }

        /// <summary>
        /// Gets the connection string based on the domain.
        /// </summary>
        /// <returns>Database connection string.</returns>
        public string? GetStorageConnectionStringByDomain()
        {
            // Example: Fetch connection string from configuration based on domain
            return configuration.GetConnectionString($"{Domain}-st");
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

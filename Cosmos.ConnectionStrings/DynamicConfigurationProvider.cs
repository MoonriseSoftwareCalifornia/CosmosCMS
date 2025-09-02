// <copyright file="ConnectionStringProvider.cs" company="Moonrise Software, LLC">
// Copyright (c) Moonrise Software, LLC. All rights reserved.
// Licensed under the GNU Public License, Version 3.0 (https://www.gnu.org/licenses/gpl-3.0.html)
// See https://github.com/MoonriseSoftwareCalifornia/CosmosCMS
// for more information concerning the license and the contributors participating to this project.
// </copyright>

namespace Cosmos.DynamicConfig
{
    using Azure.Core;
    using Microsoft.AspNetCore.Http;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.EntityFrameworkCore.Infrastructure;
    using Microsoft.Extensions.Caching.Memory;
    using Microsoft.Extensions.Configuration;
    using System.Text;
    using System.Web;

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
        private readonly StringBuilder errorMessages = new();
        private readonly string connectionString;

        /// <summary>
        /// Gets the database connection
        /// </summary>
        //private readonly Connection? connection;

        /// <summary>
        /// Gets a value indicating whether the connection is configured for multi-tenant.
        /// </summary>
        public bool IsMultiTenantConfigured { get { return GetTenantConnection("") != null; } }

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
            if (this.httpContextAccessor == null || this.httpContextAccessor.HttpContext == null)
            {
                throw new ArgumentNullException(nameof(httpContextAccessor));
            }
            // Replace this line:
            // connectionString = this.configuration.GetConnectionString("ConfigDbConnectionString");
            // With the following to ensure non-null assignment:
            connectionString = this.configuration.GetConnectionString("ConfigDbConnectionString") ?? string.Empty;
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new ArgumentException("Connection string 'ConfigDbConnectionString' not found or is empty.");
            }
            this.memoryCache = memoryCache;
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new ArgumentException("Connection string 'ConfigDbConnectionString' not found.");
            }
        }

        /// <summary>
        /// Gets the domain name from the HTTP context.
        /// </summary>
        /// <remarks>
        /// <para>Returns the domain name by looking at the incomming request.  Here is the order:</para>
        /// <list type="number">
        /// <item>Query string value 'website'.</item>
        /// <item>Standard cookie.</item>
        /// <item>Referer request header value.</item>
        /// </list>
        /// <para>Note: This should ONLY be used for multi-tenant, single editor website setup.</para>
        /// </remarks>
        public string DomainName
        {
            get
            {
                return GetTenantDomainNameFromRequest();
            }
        }

        /// <summary>
        /// Gets the database connection string.
        /// </summary>
        /// <param name="domainName">Domain name</param>
        /// <returns>Database connection string.</returns>
        public string? GetDatabaseConnectionString(string domainName)
        {
            if (string.IsNullOrWhiteSpace(domainName))
            {
                domainName = GetTenantDomainNameFromRequest();
            }
            var connection = GetTenantConnection(domainName);
            return connection?.DbConn;
        }

        /// <summary>
        /// Gets the storage connection string.
        /// </summary>
        /// <param name="domainName">Domain name</param>
        /// <returns>Database connection string.</returns>
        public string? GetStorageConnectionString(string domainName = "")
        {
            if (string.IsNullOrWhiteSpace(domainName))
            {
                domainName = GetTenantDomainNameFromRequest();
            }
            var connection = GetTenantConnection(domainName);
            return connection?.StorageConn;
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
        /// Gets the tenant website domain name from the request.
        /// </summary>
        /// <param name="configuration">App configuration.</param>
        /// <param name="httpContext">Current HTTP request context.</param>
        /// <param name="includeCookie">Include cookie value in the search.</param>
        /// <returns>Domain Name.</returns>
        /// <remarks>
        /// <para>Returns the domain name by looking at the incomming request.  Here is the order:</para>
        /// <list type="number">
        /// <item>Query string value 'website'. Sets the standard cookie if it exists.</item>
        /// <item>Standard cookie.</item>
        /// <item>Referer request header value.</item>
        /// <item>Otherwise returns the host name of the request.</item>
        /// </list>
        /// <para>Note: This should ONLY be used for multi-tenant, single editor website setup.</para>
        /// </remarks>
        public string GetTenantDomainNameFromRequest()
        {
            var httpContext = httpContextAccessor.HttpContext ?? throw new InvalidOperationException("HTTP context is not available.");
            var request = httpContext.Request;
            return request == null ? throw new InvalidOperationException("HTTP request is not available.") : request.Host.Host;
        }

        /// <summary>
        /// Handles possibility that a user entered a URI instead of a domain name, and returns just the host name.
        /// </summary>
        /// <param name="value">URI or domain value.</param>
        /// <returns>Host name only.</returns>
        public static string CleanUpDomainName(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return value;
            }

            if (Uri.TryCreate(value, UriKind.Absolute, out var referrerUri))
            {
                return referrerUri.Host.ToLower();
            }

            return value.ToLower();
        }

        /// <summary>
        /// Tests to see if there is a connection defined for the specified domain name.
        /// </summary>
        /// <param name="configuration"></param>
        /// <param name="domainName"></param>
        /// <returns>Domain is valid (true) or not (false).</returns>
        /// <exception cref="ArgumentException"></exception>
        public async Task<bool> ValidateDomainName(string domainName)
        {
            if (string.IsNullOrWhiteSpace(domainName))
            {
                return false;
            }

            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new ArgumentException("Connection string 'ConfigDbConnectionString' not found.");
            }
            using var dbContext = GetDbContext();
            var result = await dbContext.Connections.FirstOrDefaultAsync(c => c.DomainNames.Any(a => a == domainName));
            return result != null;
        }

        private DynamicConfigDbContext GetDbContext()
        {
            var options = AspNetCore.Identity.FlexDb.CosmosDbOptionsBuilder.GetDbOptions<DynamicConfigDbContext>(this.connectionString);
            return new DynamicConfigDbContext(options);
        }

        private Connection? GetTenantConnection(string domainName)
        {

            if (string.IsNullOrEmpty(domainName))
            {
                return null;
            }

            //memoryCache.TryGetValue<Connection>(domainName, out var connection);

            //if (connection != null)
            //{
            //    return connection;
            //}

            var dbContext = GetDbContext();

            var connections = dbContext.Connections.ToListAsync().Result;

            var connection = dbContext.Connections.FirstOrDefaultAsync(c => c.DomainNames.Any(a => a == domainName)).Result;

            if (connection != null)
            {
                memoryCache.Set<Connection>(domainName, connection, TimeSpan.FromMinutes(10));
            }

            return connection;
        }

    }
}

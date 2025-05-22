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
        public bool IsMultiTenantConfigured { get { return GetTenantConnection("") != null; } }

        /// <summary>
        /// Gets a value indicating the error messages that may exist.
        /// </summary>
        public string ErrorMesages => errorMessages.ToString();

        /// <summary>
        ///  Gets the standard cookie name.
        /// </summary>
        public static string StandardCookieName => "CosmosWebsiteDomain";

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
                return GetTenantDomainNameFromCookieOrHost(configuration, httpContextAccessor.HttpContext);
            }
        }

        /// <summary>
        /// Gets the database name.
        /// </summary>
        /// <param name="domainName">Domain name</param>
        /// <returns>Database name.</returns>
        public string? GetDatabaseName(string domainName = "")
        {
            if (this.isMultiTenantEditor)
            {
                return GetTenantConnection(domainName)?.DbName;
            }
            return configuration.GetValue<string>($"CosmosIdentityDbName") ?? "cosmoscms";
        }

        /// <summary>
        /// Gets the database connection string.
        /// </summary>
        /// <param name="domainName">Domain name</param>
        /// <returns>Database connection string.</returns>
        public string? GetDatabaseConnectionString(string domainName = "")
        {
            if (this.isMultiTenantEditor)
            {
                return GetTenantConnection(domainName)?.DbConn;
            }
            return configuration.GetConnectionString("ApplicationDbContextConnection");
        }

        /// <summary>
        /// Gets the storage connection string.
        /// </summary>
        /// <param name="domainName">Domain name</param>
        /// <returns>Database connection string.</returns>
        public string? GetStorageConnectionString(string domainName = "")
        {
            var con = GetTenantConnection(domainName);
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

        /// <summary>
        /// Gets the domain name from the cookie or query string, or default to the host name.
        /// </summary>
        /// <param name="configuration"></param>
        /// <param name="httpContext"></param>
        /// <returns></returns>
        public static string GetTenantDomainNameFromCookieOrHost(IConfiguration configuration, HttpContext httpContext)
        {
            var cookieValue = httpContext.Request.Cookies[StandardCookieName];
            if (!string.IsNullOrWhiteSpace(cookieValue))
            {
                return cookieValue.ToLower();
            }
            return httpContext.Request.Host.Host?.ToLower() ?? string.Empty;
        }

        /// <summary>
        /// Gets the tenant website domain name from the request.
        /// </summary>
        /// <param name="configuration">App configuration.</param>
        /// <param name="httpContext">Current HTTP request context.</param>
        /// <returns>Domain Name.</returns>
        /// <remarks>
        /// <para>Returns the domain name by looking at the incomming request.  Here is the order:</para>
        /// <list type="number">
        /// <item>Standard cookie.</item>
        /// <item>Query string value 'website'.</item>
        /// <item>Referer request header value.</item>
        /// </list>
        /// <para>Note: This should ONLY be used for login and register functionality with HTTP GET method.</para>
        /// </remarks>
        public static string GetTenantDomainNameFromRequest(IConfiguration configuration, HttpContext httpContext)
        {
            // In order of priority
            // 1. A set cookie
            var cookieValue = httpContext.Request.Cookies[StandardCookieName];
            if (!string.IsNullOrWhiteSpace(cookieValue))
            {
                return CleanUpDomainName(cookieValue);
            }

            // 2. Query string
            var queryValue = (string?) httpContext.Request.Query["website"];
            if (!string.IsNullOrWhiteSpace(queryValue))
            {
                return CleanUpDomainName(queryValue);
            }

            // 3. referer header value
            var refererValue = (string?) httpContext.Request.Headers[HttpHeader.Names.Referer];
            refererValue = CleanUpDomainName(refererValue);
            var host = httpContext.Request.Host.Host;
            if (!string.IsNullOrWhiteSpace(refererValue) && refererValue.ToLower() != host.ToLower())
            {
                return CleanUpDomainName(refererValue);
            }

            return string.Empty;
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
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        public static async Task<bool> ValidateDomainName(IConfiguration configuration, string domainName)
        {
            if (string.IsNullOrWhiteSpace(domainName))
            {
                return false;
            }

            var connectionString = configuration.GetConnectionString("ConfigDbConnectionString");

            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new ArgumentException("Connection string 'ConfigDbConnectionString' not found.");
            }

            var options = new DbContextOptionsBuilder<DynamicConfigDbContext>()
                    .UseCosmos(connectionString, databaseName: "configs")
                    .Options;
            using var dbContext = new DynamicConfigDbContext(options);
            var result = await dbContext.Connections.FirstOrDefaultAsync(c => c.DomainNames.Any(a => a == domainName));
            return result != null;
        }

        public static async Task<bool> ValidateDomainName(IConfiguration configuration, string domainName, string emailAddress)
        {
            if (string.IsNullOrWhiteSpace(domainName))
            {
                return false;
            }

            var connectionString = configuration.GetConnectionString("ConfigDbConnectionString");

            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new ArgumentException("Connection string 'ConfigDbConnectionString' not found.");
            }

            var options = new DbContextOptionsBuilder<DynamicConfigDbContext>()
                    .UseCosmos(connectionString, databaseName: "configs")
                    .Options;
            using var dbContext = new DynamicConfigDbContext(options);
            var result = await dbContext.Connections.FirstOrDefaultAsync(c => c.DomainNames.Any(a => a == domainName));

            return result != null;
        }

        private Connection? GetTenantConnection(string domainName)
        {
            if (string.IsNullOrWhiteSpace(domainName))
            {
                domainName = Domain;
            }

            if (string.IsNullOrEmpty(domainName))
            {
                var connectionString = configuration.GetConnectionString("ConfigDbConnectionString");

                if (string.IsNullOrWhiteSpace(connectionString))
                {
                    throw new ArgumentException("Connection string 'ConfigDbConnectionString' not found.");
                }

                var storageConnection = configuration.GetConnectionString("AzureBlobStorageConnectionString");

                if (string.IsNullOrWhiteSpace(storageConnection))
                {
                    throw new ArgumentException("Connection string 'AzureBlobStorageConnectionString' not found.");
                }

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

            memoryCache.TryGetValue<Connection>(domainName, out var connection);

            if (connection == null)
            {
                var options = new DbContextOptionsBuilder<DynamicConfigDbContext>()
                        .UseCosmos(connectionString, databaseName: "configs")
                        .Options;

                using var dbContext = new DynamicConfigDbContext(options);
                var result = dbContext.Connections.FirstOrDefaultAsync(c => c.DomainNames.Any(a => a == domainName));
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

// <copyright file="ApplicationDbContextUtilities.cs" company="Moonrise Software, LLC">
// Copyright (c) Moonrise Software, LLC. All rights reserved.
// Licensed under the GNU Public License, Version 3.0 (https://www.gnu.org/licenses/gpl-3.0.html)
// See https://github.com/MoonriseSoftwareCalifornia/CosmosCMS
// for more information concerning the license and the contributors participating to this project.
// </copyright>

namespace Sky.Editor.Data
{
    using System;
    using Cosmos.Common.Data;
    using Cosmos.DynamicConfig;
    using Microsoft.Extensions.DependencyInjection;

    /// <summary>
    /// Utilities for the ApplicationDbContext class.
    /// </summary>
    internal static class ApplicationDbContextUtilities
    {
        /// <summary>
        /// Get an ApplicationDbContext from a connection.
        /// </summary>
        /// <param name="connection">Website connection.</param>
        /// <returns>ApplicationDbContext.</returns>
        internal static ApplicationDbContext GetApplicationDbContext(Connection connection)
        {
            return new ApplicationDbContext(connection.DbConn);
        }

        /// <summary>
        /// Gets a new instance of the ApplicationDbContext for a specific domain name.
        /// </summary>
        /// <param name="domainName">Domain name.</param>
        /// <param name="services">Services provider.</param>
        /// <returns>ApplicationDbContext</returns>
        internal static ApplicationDbContext GetDbContextForDomain(string domainName, IServiceProvider services)
        {
            if (string.IsNullOrEmpty(domainName))
            {
                throw new ArgumentException("Domain name cannot be null or empty.", nameof(domainName));
            }

            var provider = services.GetRequiredService<IDynamicConfigurationProvider>();

            if (provider == null)
            {
                throw new InvalidOperationException("Dynamic configuration provider is not registered.");
            }

            if (!provider.IsMultiTenantConfigured)
            {
                throw new InvalidOperationException("Dynamic configuration provider is not configured for multi-tenancy.");
            }

            var connectionString = provider.GetDatabaseConnectionString(domainName);

            if (string.IsNullOrEmpty(connectionString))
            {
                throw new InvalidOperationException($"No connection string found for domain '{domainName}'.");
            }

            // Create a new instance of ApplicationDbContext with the same options but for the specified domain.
            return new ApplicationDbContext(connectionString);
        }
    }
}

// <copyright file="IConnectionStringProvider.cs" company="Moonrise Software, LLC">
// Copyright (c) Moonrise Software, LLC. All rights reserved.
// Licensed under the GNU Public License, Version 3.0 (https://www.gnu.org/licenses/gpl-3.0.html)
// See https://github.com/MoonriseSoftwareCalifornia/CosmosCMS
// for more information concerning the license and the contributors participating to this project.
// </copyright>
namespace Cosmos.Common.Services
{
    /// <summary>
    /// Connection string provider interface.
    /// </summary>
    public interface IConnectionStringProvider
    {
        /// <summary>
        /// Get configuration value.
        /// </summary>
        /// <param name="key">Key name.</param>
        /// <returns>Key value.</returns>
        string GetConfigurationValue(string key);

        /// <summary>
        /// Get database name by domain name.
        /// </summary>
        /// <param name="domain">Domain name.</param>
        /// <returns>Database name.</returns>
        string GetDatabaseNameByDomain(string domain);

        /// <summary>
        /// Get database connection string based on domain.
        /// </summary>
        /// <param name="domain">domain name.</param>
        /// <returns>Connection string.</returns>
        string GetDatabaseConnectionStringByDomain(string domain);

        /// <summary>
        /// Get storage connection string based on domain.
        /// </summary>
        /// <param name="domain">Domain name.</param>
        /// <returns>Connection string.</returns>
        string GetStorageConnectionStringByDomain(string domain);

        /// <summary>
        /// Get connection string by its name.
        /// </summary>
        /// <param name="name">Connection string name.</param>
        /// <returns>Connection string.</returns>
        string GetConnectionStringByName(string name);
    }
}

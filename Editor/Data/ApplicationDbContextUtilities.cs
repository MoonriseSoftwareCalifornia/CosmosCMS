// <copyright file="ApplicationDbContextUtilities.cs" company="Moonrise Software, LLC">
// Copyright (c) Moonrise Software, LLC. All rights reserved.
// Licensed under the GNU Public License, Version 3.0 (https://www.gnu.org/licenses/gpl-3.0.html)
// See https://github.com/MoonriseSoftwareCalifornia/CosmosCMS
// for more information concerning the license and the contributors participating to this project.
// </copyright>

namespace Cosmos.Editor.Data
{
    using Cosmos.Common.Data;
    using Cosmos.DynamicConfig;
    using Microsoft.EntityFrameworkCore;

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
            DbContextOptionsBuilder<ApplicationDbContext> dbContextOptionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
            dbContextOptionsBuilder.UseCosmos(connection.DbConn, connection.DbName);
            return new ApplicationDbContext(dbContextOptionsBuilder.Options);
        }
    }
}

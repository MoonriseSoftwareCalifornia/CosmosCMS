// <copyright file="CosmosLinqExtensions.cs" company="Moonrise Software, LLC">
// Copyright (c) Moonrise Software, LLC. All rights reserved.
// Licensed under the GNU Public License, Version 3.0 (https://www.gnu.org/licenses/gpl-3.0.html)
// See https://github.com/MoonriseSoftwareCalifornia/CosmosCMS
// for more information concerning the license and the contributors participating to this project.
// </copyright>

namespace Cosmos.Common.Data
{
    using System.Linq;
    using System.Threading.Tasks;
    using Microsoft.EntityFrameworkCore;

    /// <summary>
    /// Cosmos custom Any LINQ method.
    /// </summary>
    public static class CosmosLinqExtensions
    {
        /// <summary>
        /// Determines if any rows exist with the given query.
        /// </summary>
        /// <typeparam name="T">Dynamic type that maps to a table.</typeparam>
        /// <param name="query">Query.</param>
        /// <returns>Indicates the existence of any entities as a <see cref="bool"/>.</returns>
        public static async Task<bool> CosmosAnyAsync<T>(this IQueryable<T> query)
        {
            return (await query.CountAsync()) > 0;
        }
    }
}

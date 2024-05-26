// <copyright file="CosmosMemoryCache.cs" company="Moonrise Software, LLC">
// Copyright (c) Moonrise Software, LLC. All rights reserved.
// Licensed under the GNU Public License, Version 3.0 (https://www.gnu.org/licenses/gpl-3.0.html)
// See https://github.com/MoonriseSoftwareCalifornia/CosmosCMS
// for more information concerning the license and the contributors participating to this project.
// </copyright>

namespace Cosmos.Cms.Common.Services
{
    using Microsoft.Extensions.Caching.Memory;

    /// <summary>
    /// Short term memory cache to take pressure off of Redis and SQL.
    /// </summary>
    /// <remarks>See: <seealso href="https://docs.microsoft.com/en-us/aspnet/core/performance/caching/memory?view=aspnetcore-5.0#use-setsize-size-and-sizelimit-to-limit-cache-size"/> (Microsoft documentation).</remarks>
    public class CosmosMemoryCache
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CosmosMemoryCache"/> class.
        /// </summary>
        /// <param name="bytesmaxsize">Maximum number of bytes to use for cache (default is 64 mb).</param>
        public CosmosMemoryCache(long bytesmaxsize = 64000000)
        {
            Cache = new MemoryCache(new MemoryCacheOptions
            {
                SizeLimit = bytesmaxsize
            });
        }

        /// <summary>
        /// Gets or sets memory cache.
        /// </summary>
        public MemoryCache Cache { get; set; }
    }
}

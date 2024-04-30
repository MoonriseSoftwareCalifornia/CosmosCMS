// <copyright file="PubControllerBase.cs" company="Cosmos Website Solutions, Inc.">
// Copyright (c) Cosmos Website Solutions, Inc.. All rights reserved.
// Licensed under the GNU Public License, Version 3.0 (https://www.gnu.org/licenses/gpl-3.0.html)
// See https://github.com/MoonriseSoftwareCalifornia/CosmosCMS
// for more information concerning the license and the contributors participating to this project.
// </copyright>

namespace Cosmos.Publisher.Controllers
{
    using System;
    using System.Threading.Tasks;
    using Cosmos.BlobService;
    using Cosmos.Cms.Common.Services.Configurations;
    using Cosmos.Common;
    using Cosmos.Common.Data;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Extensions.Options;
    using Microsoft.Net.Http.Headers;

    /// <summary>
    /// Secure file access controller and proxy.
    /// </summary>
    public class PubControllerBase : Controller
    {
        private readonly IOptions<CosmosConfig> options;
        private readonly ApplicationDbContext dbContext;
        private readonly StorageContext storageContext;

        /// <summary>
        /// Initializes a new instance of the <see cref="PubControllerBase"/> class.
        /// Constructor.
        /// </summary>
        /// <param name="options">Cosmos options.</param>
        /// <param name="dbContext">Database context.</param>
        /// <param name="storageContext">Storage context.</param>
        public PubControllerBase(IOptions<CosmosConfig> options, ApplicationDbContext dbContext, StorageContext storageContext)
        {
            this.options = options;
            this.dbContext = dbContext;
            this.storageContext = storageContext;
        }

        /// <summary>
        /// Gets a file and validates user authentication.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public async Task<IActionResult> Index()
        {
            if (options.Value.SiteSettings.CosmosRequiresAuthentication)
            {
                // If the user is not logged in, have them login first.
                if (User.Identity == null || User.Identity?.IsAuthenticated == false)
                {
                    return Unauthorized();
                }

                // Gets a path to file.
                var path = HttpContext.Request.Path.ToString().ToLower();

                // See if the article is in protected storage.
                if (path.StartsWith("/pub/articles/"))
                {
                    var id = path.TrimStart('/').Split('/')[2];

                    if (int.TryParse(id, out var articleNumber))
                    {
                        // Check for user authorization.
                        if (!await CosmosUtilities.AuthUser(dbContext, User, articleNumber))
                        {
                            return Unauthorized();
                        }
                    }
                }

                Response.Headers.Expires = DateTimeOffset.UtcNow.ToString("ddd, dd MMM yyyy HH:mm:ss 'GMT'");
            }

            var client = storageContext.GetAppendBlobClient(HttpContext.Request.Path);
            var properties = await client.GetPropertiesAsync();

            return File(fileStream: await client.OpenReadAsync(), contentType: properties.Value.ContentType, lastModified: properties.Value.LastModified, entityTag: new EntityTagHeaderValue(properties.Value.ETag.ToString()));
        }
    }
}

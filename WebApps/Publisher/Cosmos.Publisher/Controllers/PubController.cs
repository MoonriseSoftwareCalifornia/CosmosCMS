// <copyright file="PubController.cs" company="Moonrise Software, LLC">
// Copyright (c) Moonrise Software, LLC. All rights reserved.
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

    /// <summary>
    /// Secure file access controller and proxy.
    /// </summary>
    public class PubController : Controller
    {
        private readonly IOptions<CosmosConfig> options;
        private readonly ApplicationDbContext dbContext;
        private readonly StorageContext storageContext;

        /// <summary>
        /// Initializes a new instance of the <see cref="PubController"/> class.
        /// </summary>
        /// <param name="options">Cosmos options.</param>
        /// <param name="dbContext">Database context.</param>
        /// <param name="storageContext">File storage context.</param>
        public PubController(IOptions<CosmosConfig> options, ApplicationDbContext dbContext, StorageContext storageContext)
        {
            this.options = options;
            this.dbContext = dbContext;
            this.storageContext = storageContext;
        }

        /// <summary>
        /// Gets a file and validates user authentication.
        /// </summary>
        /// <returns>Returns an <see cref="IActionResult"/>.</returns>
        public async Task<IActionResult> Index()
        {
            if (options.Value.SiteSettings.PublisherRequiresAuthentication)
            {
                // If the user is not logged in, have them login first.
                if (User.Identity == null || User.Identity?.IsAuthenticated == false)
                {
                    return Redirect("~/Identity/Account/Login?returnUrl=" + Request.Path);
                }

                if (User.IsInRole(options.Value.SiteSettings.CosmosRequiredPublisherRole) == false)
                {
                    return Unauthorized();
                }

                var path = HttpContext.Request.Path.ToString().ToLower();

                if (path.StartsWith("/pub/articles/"))
                {
                    var id = path.TrimStart('/').Split('/')[2];

                    if (int.TryParse(id, out var articleNumber))
                    {
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

            return File(await client.OpenReadAsync(), properties.Value.ContentType);
        }
    }
}

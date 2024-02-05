// <copyright file="HomeController.cs" company="Moonrise Software, LLC">
// Copyright (c) Moonrise Software, LLC. All rights reserved.
// Licensed under the GNU Public License, Version 3.0 (https://www.gnu.org/licenses/gpl-3.0.html)
// See https://github.com/MoonriseSoftwareCalifornia/CosmosCMS
// for more information concerning the license and the contributors participating to this project.
// </copyright>

namespace Cosmos.Cms.Publisher.Controllers
{
    using System.Diagnostics;
    using System.Net;
    using System.Text;
    using System.Web;
    using Cosmos.BlobService;
    using Cosmos.Cms.Common.Services.Configurations;
    using Cosmos.Cms.Publisher.Models;
    using Cosmos.Common;
    using Cosmos.Common.Data;
    using Cosmos.Common.Data.Logic;
    using Cosmos.Common.Models;
    using Cosmos.Common.Services.PowerBI;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Extensions.Options;

    /// <summary>
    /// Home page controller.
    /// </summary>
    public class HomeController : HomeControllerBase
    {
        private readonly ILogger<HomeController> logger;
        private readonly ArticleLogic articleLogic;
        private readonly IOptions<CosmosConfig> options;
        private readonly ApplicationDbContext dbContext;

        /// <summary>
        /// Initializes a new instance of the <see cref="HomeController"/> class.
        /// </summary>
        /// <param name="logger">Logger.</param>
        /// <param name="articleLogic">Article logic.</param>
        /// <param name="options">Cosmos options.</param>
        /// <param name="dbContext">Database Context.</param>
        /// <param name="storageContext">Storage context.</param>
        /// <param name="powerBiTokenService">Service used to get tokens from Power BI.</param>
        public HomeController(
            ILogger<HomeController> logger,
            ArticleLogic articleLogic,
            IOptions<CosmosConfig> options,
            ApplicationDbContext dbContext,
            StorageContext storageContext,
            PowerBiTokenService powerBiTokenService)
            : base(articleLogic, dbContext, storageContext, logger, powerBiTokenService)
        {
            this.logger = logger;
            this.articleLogic = articleLogic;
            this.options = options;
            this.dbContext = dbContext;
        }

        /// <summary>
        /// Index view.
        /// </summary>
        /// <returns>Returns an <see cref="IActionResult"/>.</returns>
        public async Task<IActionResult> Index()
        {
            try
            {
                var article = await articleLogic.GetPublishedPageByUrl(HttpContext.Request.Path, HttpContext.Request.Query["lang"], TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(20)); // ?? await _articleLogic.GetByUrl(id, langCookie);

                if (article == null)
                {
                    if (!await dbContext.Pages.CosmosAnyAsync())
                    {
                        // No pages published yet
                        return View("__UnderConstruction");
                    }

                    HttpContext.Response.StatusCode = 404;

                    if (article == null)
                    {
                        Response.StatusCode = (int)HttpStatusCode.NotFound;
                        return View("__NotFound");
                    }
                }

                if (options.Value.SiteSettings.CosmosRequiresAuthentication)
                {
                    if (!await CosmosUtilities.AuthUser(dbContext, User, article.ArticleNumber))
                    {
                        if (User.Identity.IsAuthenticated)
                        {
                            return View("__NeedPermission");
                        }

                        return Redirect("~/Identity/Account/Login?returnUrl=" + HttpUtility.UrlEncode(Request.Path));
                    }

                    Response.Headers.Expires = DateTimeOffset.UtcNow.AddMinutes(-30).ToString("ddd, dd MMM yyyy HH:mm:ss 'GMT'");
                    Response.Headers.ETag = Guid.NewGuid().ToString();
                    Response.Headers.LastModified = DateTimeOffset.UtcNow.AddMinutes(-30).ToString("ddd, dd MMM yyyy HH:mm:ss 'GMT'");
                    Response.Headers.CacheControl = "no-store,no-cache";
                }
                else
                {
                    Response.Headers.Expires = article.Expires.HasValue ?
                        article.Expires.Value.ToString("ddd, dd MMM yyyy HH:mm:ss 'GMT'") :
                        DateTimeOffset.UtcNow.AddMinutes(3).ToString("ddd, dd MMM yyyy HH:mm:ss 'GMT'");
                    Response.Headers.ETag = article.Id.ToString();
                    Response.Headers.LastModified = article.Updated.ToString("ddd, dd MMM yyyy HH:mm:ss 'GMT'");
                }

                if (HttpContext.Request.Query["mode"] == "json")
                {
                    article.Layout = null;
                    return Json(article);
                }

                return View(article);
            }
            catch (Microsoft.Azure.Cosmos.CosmosException e)
            {
                string message = e.Message;
                logger.LogError(e, message);

                if (HttpContext.Request.Query["mode"] == "json")
                {
                    return NotFound();
                }

                Response.StatusCode = (int)HttpStatusCode.NotFound;
                return View("__NotFound");
            }
            catch (Exception e)
            {
                string message = e.Message;
                logger.LogError(e, message);

                if (HttpContext.Request.Query["mode"] == "json")
                {
                    return NotFound();
                }

                Response.StatusCode = (int)HttpStatusCode.NotFound;
                return View("__NotFound");
            }
        }

        /// <summary>
        /// Returns and error page.
        /// </summary>
        /// <returns>Returns an <see cref="IActionResult"/> with an <see cref="ErrorViewModel"/>.</returns>
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        /// <summary>
        /// Gets the application validation for Microsoft.
        /// </summary>
        /// <returns>Returns the microsoft-identity-association.json file as an <see cref="IActionResult"/>.</returns>
        [AllowAnonymous]
        public IActionResult GetMicrosoftIdentityAssociation()
        {
            var model = new MicrosoftValidationObject();
            model.associatedApplications.Add(new AssociatedApplication() { applicationId = options.Value.MicrosoftAppId });

            var data = Newtonsoft.Json.JsonConvert.SerializeObject(model);

            return File(Encoding.UTF8.GetBytes(data), "application/json", fileDownloadName: "microsoft-identity-association.json");
        }
    }
}
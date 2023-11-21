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
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Cors;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.Options;
    using SendGrid.Helpers.Mail;

    /// <summary>
    /// Home page controller.
    /// </summary>
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> logger;
        private readonly ArticleLogic articleLogic;
        private readonly IOptions<CosmosConfig> options;
        private readonly ApplicationDbContext dbContext;
        private readonly StorageContext storageContext;

        /// <summary>
        /// Initializes a new instance of the <see cref="HomeController"/> class.
        /// </summary>
        /// <param name="logger">Logger.</param>
        /// <param name="articleLogic">Article logic.</param>
        /// <param name="options">Cosmos options.</param>
        /// <param name="dbContext">Database Context.</param>
        /// <param name="storageContext">Storage context.</param>
        public HomeController(ILogger<HomeController> logger, ArticleLogic articleLogic, IOptions<CosmosConfig> options, ApplicationDbContext dbContext, StorageContext storageContext)
        {
            this.logger = logger;
            this.articleLogic = articleLogic;
            this.options = options;
            this.dbContext = dbContext;
            this.storageContext = storageContext;
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
                }

                Response.Headers.Expires = article.Expires.HasValue ?
                    article.Expires.Value.ToString("ddd, dd MMM yyyy HH:mm:ss 'GMT'") :
                    DateTimeOffset.UtcNow.AddMinutes(3).ToString("ddd, dd MMM yyyy HH:mm:ss 'GMT'");
                Response.Headers.ETag = article.Id.ToString();
                Response.Headers.LastModified = article.Updated.ToString("ddd, dd MMM yyyy HH:mm:ss 'GMT'");

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

        /// <summary>
        /// Gets the children of a given page path.
        /// </summary>
        /// <param name="page">UrlPath.</param>
        /// <param name="orderByPub">Ordery by publishing date.</param>
        /// <param name="pageNo">Page number.</param>
        /// <param name="pageSize">Number of rows in each page.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        [EnableCors("AllCors")]
        public async Task<IActionResult> GetTOC(
            string page,
            bool? orderByPub,
            int? pageNo,
            int? pageSize)
        {
            var result = await articleLogic.GetTOC(page, pageNo ?? 0, pageSize ?? 10, orderByPub ?? false);
            return Json(result);
        }

        /// <summary>
        /// Gets contents in an article folder.
        /// </summary>
        /// <param name="path">Path to retrieve contents from storage.</param>
        /// <returns>An <see cref="IActionResult"/> containing a <see cref="FileManagerEntry"/> <see cref="List{T}"/>.</returns>
        public async Task<IActionResult> CCMS_GetArticleFolderContents(string path = "")
        {
            var r = Request.Headers["referer"];
            if (string.IsNullOrEmpty(r))
            {
                return Json("[]");
            }

            var url = new Uri(r);
            var page = await dbContext.Pages.Select(s => new { s.ArticleNumber, s.UrlPath }).FirstOrDefaultAsync(f => f.UrlPath == url.AbsolutePath.TrimStart('/'));

            if (page == null)
            {
                return Json("[]");
            }

            if (options.Value.SiteSettings.CosmosRequiresAuthentication)
            {
                // If the user is not logged in, have them login first.
                if (User.Identity == null || User.Identity?.IsAuthenticated == false)
                {
                    return Unauthorized();
                }

                if (!await CosmosUtilities.AuthUser(dbContext, User, page.ArticleNumber))
                {
                    return Unauthorized();
                }
            }

            var contents = await CosmosUtilities.GetArticleFolderContents(storageContext, page.ArticleNumber, path);

            return Json(contents);
        }

        /// <summary>
        /// Returns a health check.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        /// 
        [AllowAnonymous]
        public async Task<IActionResult> CWPS_UTILITIES_NET_PING_HEALTH_CHECK()
        {
            try
            {
                _ = await dbContext.Users.Select(s => s.Id).FirstOrDefaultAsync();
                return Ok();
            }
            catch
            {
            }

            return StatusCode(500);
        }
    }
}
// <copyright file="HomeController.cs" company="Moonrise Software, LLC">
// Copyright (c) Moonrise Software, LLC. All rights reserved.
// Licensed under the GNU Public License, Version 3.0 (https://www.gnu.org/licenses/gpl-3.0.html)
// See https://github.com/MoonriseSoftwareCalifornia/CosmosCMS
// for more information concerning the license and the contributors participating to this project.
// </copyright>

using System.Diagnostics;
using System.Net;
using System.Security.Claims;
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
using Cosmos.MicrosoftGraph;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace Cosmos.Cms.Publisher.Controllers
{
    /// <summary>
    /// Home page controller.
    /// </summary>
    public class HomeController : HomeControllerBase
    {
        private readonly IConfiguration configuration;
        private readonly ILogger<HomeController> logger;
        private readonly ArticleLogic articleLogic;
        private readonly IOptions<CosmosConfig> options;
        private readonly ApplicationDbContext dbContext;
        private readonly MsGraphService graphService;

        /// <summary>
        /// Initializes a new instance of the <see cref="HomeController"/> class.
        /// </summary>
        /// <param name="configuration">Configuration.</param>
        /// <param name="logger">Logger.</param>
        /// <param name="articleLogic">Article logic.</param>
        /// <param name="options">Cosmos options.</param>
        /// <param name="dbContext">Database Context.</param>
        /// <param name="storageContext">Storage context.</param>
        /// <param name="powerBiTokenService">Service used to get tokens from Power BI.</param>
        /// <param name="emailSender">Email services.</param>
        /// <param name="graphService">Microsoft Graph service.</param>
        public HomeController(
            IConfiguration configuration,
            ILogger<HomeController> logger,
            ArticleLogic articleLogic,
            IOptions<CosmosConfig> options,
            ApplicationDbContext dbContext,
            StorageContext storageContext,
            PowerBiTokenService powerBiTokenService,
            IEmailSender emailSender,
            MsGraphService graphService)
            : base(articleLogic, dbContext, storageContext, logger, powerBiTokenService, emailSender, options)
        {
            this.configuration = configuration;
            this.logger = logger;
            this.articleLogic = articleLogic;
            this.options = options;
            this.dbContext = dbContext;
            this.graphService = graphService;

            // Ensure the database is created if we are in setup mode.
            if (options.Value.SiteSettings.AllowSetup)
            {
                _ = dbContext.Database.EnsureCreatedAsync().Result;
            }
        }

        /// <summary>
        /// Handles the head request.
        /// </summary>
        /// <returns>IActionResult.</returns>
        [HttpHead]
        [ActionName("Index")]
        public async Task<IActionResult> CCMS___Head()
        {
            if (!options.Value.SiteSettings.CosmosRequiresAuthentication)
            {
                var article = await articleLogic.GetPublishedPageHeaderByUrl(HttpContext.Request.Path);

                if (article == null)
                {
                    return NotFound();
                }

                Response.Headers.Expires = article.Expires.HasValue ?
                    article.Expires.Value.ToString("ddd, dd MMM yyyy HH:mm:ss 'GMT'") :
                    DateTimeOffset.UtcNow.AddMinutes(1).ToString("ddd, dd MMM yyyy HH:mm:ss 'GMT'");
                Response.Headers.ETag = article.Id.ToString();
                Response.Headers.LastModified = article.Updated.ToString("ddd, dd MMM yyyy HH:mm:ss 'GMT'");
                ControllerContext.HttpContext.Response.Headers.CacheControl = "max-age=60, stale-while-revalidate=59";

                return Ok("Ok");
            }

            return Unauthorized();
        }

        /// <summary>
        /// Index view.
        /// </summary>
        /// <param name="lang">Language code.</param>
        /// <param name="mode">json or nothing.</param>
        /// <returns>Returns an <see cref="IActionResult"/>.</returns>
        [HttpGet]
        public async Task<IActionResult> Index(string lang = "", string mode = "")
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                var article = await articleLogic.GetPublishedPageByUrl(HttpContext.Request.Path, lang, TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(20));

                if (article == null)
                {
                    if (!await dbContext.Pages.CosmosAnyAsync())
                    {
                        // No pages published yet
                        return View("__UnderConstruction");
                    }

                    Response.StatusCode = (int)HttpStatusCode.NotFound;
                    return View("__NotFound");
                }

                if (options.Value.SiteSettings.CosmosRequiresAuthentication)
                {
                    if (!User.Identity.IsAuthenticated)
                    {
                        return Redirect("~/Identity/Account/Login?returnUrl=" + HttpUtility.UrlEncode(Request.Path));
                    }

                    // Look for group membership
                    var validGroups = configuration.GetValue<string>("EntraIdValidUserGroups");

                    if (!string.IsNullOrWhiteSpace(validGroups))
                    {
                        var groupArray = validGroups.Split(';');

                        if (graphService != null)
                        {
                            var principal = User as ClaimsPrincipal;
                            var emailAdress = principal.FindFirstValue(ClaimTypes.Email);

                            var graphUser = await graphService.GetGraphUserByEmailAddress(emailAdress);  
                            var groups = await graphService.GetGraphApiUserMemberGroups(graphUser.FirstOrDefault().Id);
                            if (groups.Any(a => groupArray.Contains(a.DisplayName)))
                            {
                                return View(article);
                            }
                            else
                            {
                                return View("__NeedPermission");
                            }
                        }

                        if (!await CosmosUtilities.AuthUser(dbContext, User, article.ArticleNumber))
                        {
                            if (User.Identity.IsAuthenticated)
                            {
                                return View("__NeedPermission");
                            }

                            return Redirect("~/Identity/Account/Login?returnUrl=" + HttpUtility.UrlEncode(Request.Path));
                        }
                    }

                    Response.Headers.Expires = DateTimeOffset.UtcNow.AddMinutes(-30).ToString("ddd, dd MMM yyyy HH:mm:ss 'GMT'");
                    Response.Headers.ETag = Guid.NewGuid().ToString();
                    Response.Headers.LastModified = DateTimeOffset.UtcNow.AddMinutes(-30).ToString("ddd, dd MMM yyyy HH:mm:ss 'GMT'");
                    Response.Headers.CacheControl = new Microsoft.Extensions.Primitives.StringValues("no -store,no-cache");
                }
                else
                {
                    Response.Headers.Expires = article.Expires.HasValue ?
                        article.Expires.Value.ToString("ddd, dd MMM yyyy HH:mm:ss 'GMT'") :
                        DateTimeOffset.UtcNow.AddMinutes(1).ToString("ddd, dd MMM yyyy HH:mm:ss 'GMT'");
                    Response.Headers.ETag = article.Id.ToString();
                    Response.Headers.LastModified = article.Updated.ToString("ddd, dd MMM yyyy HH:mm:ss 'GMT'");
                    ControllerContext.HttpContext.Response.Headers.CacheControl = "max-age=20, stale-while-revalidate=119";
                }

                if (mode == "json")
                {
                    article.Layout = null;
                    return Json(article);
                }

                if (article.StatusCode == StatusCodeEnum.Redirect)
                {
                    return View("~/Views/Home/Redirect.cshtml", new RedirectItemViewModel()
                    {
                        FromUrl = article.UrlPath,
                        ToUrl = article.Content,
                        Id = article.Id
                    });
                }

                return View(article);
            }
            catch (Microsoft.Azure.Cosmos.CosmosException e)
            {
                string message = e.Message;
                logger.LogError(e, message);

                if (mode == "json")
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

                if (mode == "json")
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
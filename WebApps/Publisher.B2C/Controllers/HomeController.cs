// <copyright file="HomeController.cs" company="Moonrise Software, LLC">
// Copyright (c) Moonrise Software, LLC. All rights reserved.
// Licensed under the GNU Public License, Version 3.0 (https://www.gnu.org/licenses/gpl-3.0.html)
// See https://github.com/MoonriseSoftwareCalifornia/CosmosCMS
// for more information concerning the license and the contributors participating to this project.
// </copyright>

namespace Cosmos.Publisher.B2C.Controllers;

using Cosmos.BlobService;
using Cosmos.Cms.Common.Services.Configurations;
using Cosmos.Common.Data;
using Cosmos.Common.Data.Logic;
using Cosmos.Common.Models;
using Cosmos.Common.Services.PowerBI;
using Cosmos.Publisher.B2C.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System.Diagnostics;
using System.Net;
using System.Text;

/// <summary>
/// Home page controller.
/// </summary>
[AllowAnonymous]
public class HomeController : B2CBaseController
{
    private readonly ILogger<HomeController> logger;
    private readonly ArticleLogic articleLogic;
    private readonly IOptions<CosmosConfig> options;
    private readonly ApplicationDbContext dbContext;
    private readonly string userGroupName;

    /// <summary>
    /// Initializes a new instance of the <see cref="HomeController"/> class.
    /// </summary>
    /// <param name="logger">Logger.</param>
    /// <param name="articleLogic">Article logic.</param>
    /// <param name="options">Cosmos options.</param>
    /// <param name="dbContext">Database Context.</param>
    /// <param name="storageContext">Storage context.</param>
    /// <param name="powerBiTokenService">Service used to get tokens from Power BI.</param>
    /// <param name="configuration">Application configuration.</param>
    public HomeController(
        ILogger<HomeController> logger,
        ArticleLogic articleLogic,
        IOptions<CosmosConfig> options,
        ApplicationDbContext dbContext,
        StorageContext storageContext,
        PowerBiTokenService powerBiTokenService,
        IConfiguration configuration)
        : base(articleLogic, dbContext, storageContext, logger, powerBiTokenService, configuration)
    {
        this.logger = logger;
        this.articleLogic = articleLogic;
        this.options = options;
        this.dbContext = dbContext;
        this.userGroupName = configuration.GetValue<string>("AzureAd:UserGroup") !;
    }

    /// <summary>
    /// Handles the head request.
    /// </summary>
    /// <returns>IActionResult.</returns>
    [HttpHead]
    [ActionName("Index")]
    public async Task<IActionResult> CCMS___Head()
    {
        try
        {
            var article = await articleLogic.GetPublishedPageByUrl(HttpContext.Request.Path, string.Empty, TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(20), true);

            if (article == null)
            {
                return NotFound();
            }

            if (options.Value.SiteSettings.CosmosRequiresAuthentication)
            {
                if (User == null || User.Identity == null || !User.Identity.IsAuthenticated || !IsMemberOfGroup(this.userGroupName))
                {
                    return Unauthorized();
                }

                ControllerContext.HttpContext.Response.Headers.Expires = DateTimeOffset.UtcNow.AddMinutes(-30).ToString("ddd, dd MMM yyyy HH:mm:ss 'GMT'");
                Response.Headers.ETag = Guid.NewGuid().ToString();
                Response.Headers.LastModified = DateTimeOffset.UtcNow.AddMinutes(-30).ToString("ddd, dd MMM yyyy HH:mm:ss 'GMT'");
                Response.Headers.CacheControl = "no-store,no-cache";
            }
            else
            {
                Response.Headers.Expires = article.Expires.HasValue ?
                    article.Expires.Value.ToString("ddd, dd MMM yyyy HH:mm:ss 'GMT'") :
                    DateTimeOffset.UtcNow.AddMinutes(1).ToString("ddd, dd MMM yyyy HH:mm:ss 'GMT'");
                Response.Headers.ETag = article.Id.ToString();
                Response.Headers.LastModified = article.Updated.ToString("ddd, dd MMM yyyy HH:mm:ss 'GMT'");
                ControllerContext.HttpContext.Response.Headers.CacheControl = "max-age=60, stale-while-revalidate=59";
            }

            return Ok("Ok");
        }
        catch (Microsoft.Azure.Cosmos.CosmosException e)
        {
            string message = e.Message;
            logger.LogError(e, message);

            Response.StatusCode = (int)HttpStatusCode.NotFound;
            return NotFound();
        }
        catch (Exception e)
        {
            string message = e.Message;
            logger.LogError(e, message);

            Response.StatusCode = (int)HttpStatusCode.NotFound;
            return NotFound();
        }
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
                if (User == null || User.Identity == null || !User.Identity.IsAuthenticated)
                {
                    // Entra ID B2C Login page.
                    return RedirectToAction("SignIn", "Account", new { area = "MicrosoftIdentity" });
                }

                // Check if user is a member of the group, if not alert that the person needs permission.
                var isMember = IsMemberOfGroup(userGroupName);
                var isMemberOfAdminGroup = IsMemberOfGroup("Admin");

                if (!isMember && !isMemberOfAdminGroup)
                {
                    return View("__NeedPermission");
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
    [AllowAnonymous]
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

// <copyright file="HomeController.cs" company="Moonrise Software, LLC">
// Copyright (c) Moonrise Software, LLC. All rights reserved.
// Licensed under the GNU Public License, Version 3.0 (https://www.gnu.org/licenses/gpl-3.0.html)
// See https://github.com/MoonriseSoftwareCalifornia/CosmosCMS
// for more information concerning the license and the contributors participating to this project.
// </copyright>


namespace Cosmos.Publisher.B2C.Controllers;

using System.Diagnostics;
using System.Net;
using System.Text;
using Cosmos.BlobService;
using Cosmos.Cms.Common.Services.Configurations;
using Cosmos.Common;
using Cosmos.Common.Data;
using Cosmos.Common.Data.Logic;
using Cosmos.Common.Models;
using Cosmos.Common.Services.PowerBI;
using Cosmos.Publisher.B2C.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

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
    private readonly string userGroupId;

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
        this.userGroupId = configuration.GetValue<string>("AzureAd:GroupId")!;
    }

    /// <summary>
    /// Index view.
    /// </summary>
    /// <returns>Returns an <see cref="IActionResult"/>.</returns>
    public async Task<IActionResult> Index()
    {
        if (Request.Method == "HEAD")
        {
            try
            {
                var article = await articleLogic.GetPublishedPageByUrl(HttpContext.Request.Path, HttpContext.Request.Query["lang"], TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(20), true); // ?? await _articleLogic.GetByUrl(id, langCookie);

                if (article == null)
                {
                    return NotFound();
                }


                if (options.Value.SiteSettings.CosmosRequiresAuthentication)
                {
                    if (User == null || User.Identity == null || User.Identity.IsAuthenticated == false || IsMemberOfGroup(this.userGroupId) == false)
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

                if (HttpContext.Request.Query["mode"] == "json")
                {
                    return NotFound();
                }

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

            var headers = Request.GetTypedHeaders();

            if (options.Value.SiteSettings.CosmosRequiresAuthentication)
            {
                if (User == null || User.Identity == null || User.Identity.IsAuthenticated == false)
                {
                    // Entra ID B2C Login page.
                    return RedirectToAction("SignIn", "Account", new { area = "MicrosoftIdentity" });
                }

                // Check if user is a member of the group, if not alert that the person needs permission.
                var isMember = IsMemberOfGroup(userGroupId);
                if (!User.Identity.IsAuthenticated && isMember)
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
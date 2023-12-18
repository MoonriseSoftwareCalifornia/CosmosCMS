// <copyright file="HomeController.cs" company="Moonrise Software, LLC">
// Copyright (c) Moonrise Software, LLC. All rights reserved.
// Licensed under the GNU Public License, Version 3.0 (https://www.gnu.org/licenses/gpl-3.0.html)
// See https://github.com/MoonriseSoftwareCalifornia/CosmosCMS
// for more information concerning the license and the contributors participating to this project.
// </copyright>

namespace Cosmos.Cms.Controllers
{
    using Cosmos.BlobService;
    using Cosmos.Cms.Common.Services.Configurations;
    using Cosmos.Cms.Data.Logic;
    using Cosmos.Cms.Models;
    using Cosmos.Common;
    using Cosmos.Common.Data;
    using Cosmos.Common.Models;
    using Microsoft.AspNetCore.Antiforgery;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Identity;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Options;
    using Microsoft.Net.Http.Headers;
    using System;
    using System.Diagnostics;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    /// <summary>
    /// Home page controller.
    /// </summary>
    [Authorize]
    [ResponseCache(NoStore = true)]
    public class HomeController : HomeControllerBase
    {
        private readonly ArticleEditLogic articleLogic;
        private readonly IOptions<CosmosConfig> options;
        private readonly ApplicationDbContext dbContext;
        private readonly ILogger<HomeController> logger;
        private readonly UserManager<IdentityUser> userManager;
        private readonly StorageContext storageContext;

        /// <summary>
        /// Initializes a new instance of the <see cref="HomeController"/> class.
        /// </summary>
        /// <param name="logger">ILogger to use.</param>
        /// <param name="cosmosConfig"><see cref="CosmosConfig">Cosmos configuration</see>.</param>
        /// <param name="dbContext"><see cref="ApplicationDbContext">Database context</see>.</param>
        /// <param name="articleLogic"><see cref="ArticleEditLogic">Article edit logic.</see>.</param>
        /// <param name="userManager">User manager.</param>
        /// <param name="storageContext"><see cref="StorageContext">File storage context</see>.</param>
        public HomeController(
            ILogger<HomeController> logger,
            IOptions<CosmosConfig> cosmosConfig,
            ApplicationDbContext dbContext,
            ArticleEditLogic articleLogic,
            UserManager<IdentityUser> userManager,
            StorageContext storageContext)
            : base(articleLogic, dbContext, storageContext, logger)
        {
            this.logger = logger;
            options = cosmosConfig;
            this.articleLogic = articleLogic;
            this.dbContext = dbContext;
            this.userManager = userManager;
            this.storageContext = storageContext;
        }

        /// <summary>
        /// Editor home index method.
        /// </summary>
        /// <param name="target">Path to page to edit.</param>
        /// <param name="articleNumber">article number to edit.</param>
        /// <param name="versionNumber">version to edit.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public async Task<IActionResult> CcmsContentIndex(string target, int? articleNumber = null, int? versionNumber = null)
        {
            if (!await EnsureLayoutExists())
            {
                return RedirectToAction("Index", "Layouts");
            }

            if (!await EnsureArticleExists())
            {
                return RedirectToAction("Index", "Edit");
            }

            ArticleViewModel article;

            if (articleNumber == null)
            {
                article = await articleLogic.GetByUrl(target);
            }
            else
            {
                article = await articleLogic.Get(articleNumber.Value, versionNumber.Value);
            }

            return View(article);
        }

        /// <summary>
        /// Get edit list.
        /// </summary>
        /// <param name="target">Path to page.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public async Task<IActionResult> EditList(string target)
        {
            var article = await articleLogic.GetByUrl(target);

            var data = await dbContext.Articles.OrderByDescending(o => o.VersionNumber)
                .Where(a => a.ArticleNumber == article.ArticleNumber).Select(s => new ArticleEditMenuItem
                {
                    Id = s.Id,
                    ArticleNumber = s.ArticleNumber,
                    Published = s.Published,
                    VersionNumber = s.VersionNumber,
                    UsesHtmlEditor = s.Content.ToLower().Contains(" editable=") || s.Content.ToLower().Contains(" data-ccms-ceid=")
                }).OrderByDescending(o => o.VersionNumber).Take(1).ToListAsync();

            return Json(data);
        }

        /// <summary>
        /// Index page.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        [AllowAnonymous]
        public async Task<IActionResult> Index()
        {
            try
            {
                if (User.Identity?.IsAuthenticated == false)
                {
                    // See if we need to register a new user.
                    if (await dbContext.Users.CosmosAnyAsync())
                    {
                        return Redirect("~/Identity/Account/Login");
                    }

                    return Redirect("~/Identity/Account/Register");
                }
                else
                {
                    // Make sure the user's claims identity has an account here.
                    var user = await userManager.GetUserAsync(User);

                    if (user == null)
                    {
                        Response.Cookies.Delete("CosmosAuthCookie");
                        return Redirect("~/Identity/Account/Logout");
                    }

                    if (options.Value.SiteSettings.AllowSetup && (await dbContext.Users.CountAsync()) == 1 && !User.IsInRole("Administrators"))
                    {
                        await userManager.AddToRoleAsync(user, "Administrators");
                    }

                    if (!User.IsInRole("Reviewers") && !User.IsInRole("Authors") && !User.IsInRole("Editors") &&
                        !User.IsInRole("Administrators"))
                    {
                        return RedirectToAction("AccessPending");
                    }
                }

                if (options.Value.SiteSettings.AllowSetup)
                {
                    // Enable static website for Azure BLOB storage
                    if (options.Value.SiteSettings.CosmosRequiresAuthentication == false)
                    {
                        await storageContext.EnableAzureStaticWebsite();
                    }
                    else
                    {
                        await storageContext.DisableAzureStaticWebsite();
                    }
                }

                // If we do not yet have a layout, go to a page where we can select one.
                if (!await EnsureLayoutExists())
                {
                    return RedirectToAction("Index", "Layouts");
                }

                // If there are not web pages yet, let's go create a new home page.
                if (!await EnsureArticleExists())
                {
                    return RedirectToAction("Index", "Editor");
                }

                // If yes, do NOT include headers that allow caching. 
                Response.Headers[HeaderNames.CacheControl] = "no-store";

                // Response.Headers[HeaderNames.Pragma] = "no-cache"; This conflicts with Azure Frontdoor premium with private links and affinity set.
                var article = await articleLogic.GetByUrl(HttpContext.Request.Path, HttpContext.Request.Query["lang"]); // ?? await _articleLogic.GetByUrl(id, langCookie);

                // Article not found?
                // try getting a version not published.
                if (article == null)
                {
                    // Create your own not found page for a graceful page for users.
                    article = await articleLogic.GetByUrl("/not_found", HttpContext.Request.Query["lang"]);

                    HttpContext.Response.StatusCode = 404;

                    if (article == null)
                    {
                        return NotFound();
                    }
                }

                article.EditModeOn = false;
                article.ReadWriteMode = true;

                return View(article);
            }
            catch (Exception e)
            {
                logger.LogError(e, e.Message);
                throw;
            }
        }

        /// <summary>
        ///     Gets an article by its ID (or row key).
        /// </summary>
        /// <param name="articleNumber">Article number.</param>
        /// <param name="versionNumber">Version number.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public async Task<IActionResult> Preview(int articleNumber, int? versionNumber = null)
        {
            try
            {
                ViewData["EditModeOn"] = false;
                var article = await articleLogic.Get(articleNumber, versionNumber);

                // Check base header
                // article.UrlPath = $"/home/preview/{id}";
                // _articleLogic.UpdateHeadBaseTag(article);

                // Home/Preview/154
                if (article != null)
                {
                    article.ReadWriteMode = false;
                    article.EditModeOn = false;

                    return View("Preview", article);
                }

                return NotFound();
            }
            catch (Exception e)
            {
                logger.LogError(e, e.Message);
                throw;
            }
        }

        /// <summary>
        /// Error page.
        /// </summary>
        /// <returns>Returns an <see cref="IActionResult"/>.</returns>
        public IActionResult Error()
        {
            // Response.Headers[HeaderNames.CacheControl] = "no-store";
            // Response.Headers[HeaderNames.Pragma] = "no-cache";
            ViewData["EditModeOn"] = false;
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        /// <summary>
        /// Gets the application validation for Microsoft.
        /// </summary>
        /// <returns>Returns an <see cref="FileContentResult"/> if successful.</returns>
        [AllowAnonymous]
        public IActionResult GetMicrosoftIdentityAssociation()
        {
            var model = new MicrosoftValidationObject();
            model.associatedApplications.Add(new AssociatedApplication() { applicationId = options.Value.MicrosoftAppId });

            var data = Newtonsoft.Json.JsonConvert.SerializeObject(model);

            return File(Encoding.UTF8.GetBytes(data), "application/json", fileDownloadName: "microsoft-identity-association.json");
        }

        /// <summary>
        /// Returns if a user has not been granted access yet.
        /// </summary>
        /// <returns>Returns an <see cref="IActionResult"/>.</returns>
        [Authorize]
        public IActionResult AccessPending()
        {
            var model = new ArticleViewModel
            {
                Id = Guid.NewGuid(),
                ArticleNumber = 0,
                UrlPath = null,
                VersionNumber = 0,
                Published = null,
                Title = "Access Pending",
                Content = null,
                Updated = default,
                HeadJavaScript = null,
                FooterJavaScript = null,
                Layout = null,
                ReadWriteMode = false,
                PreviewMode = false,
                EditModeOn = false
            };
            return View(model);
        }

        /// <summary>
        /// Ensures there is a Layout.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        private async Task<bool> EnsureLayoutExists()
        {
            return await dbContext.Layouts.CosmosAnyAsync();
        }

        /// <summary>
        /// Ensures there is at least one article.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        private async Task<bool> EnsureArticleExists()
        {
            return await dbContext.Articles.CosmosAnyAsync();
        }
    }
}
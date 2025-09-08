// <copyright file="HomeController.cs" company="Moonrise Software, LLC">
// Copyright (c) Moonrise Software, LLC. All rights reserved.
// Licensed under the GNU Public License, Version 3.0 (https://www.gnu.org/licenses/gpl-3.0.html)
// See https://github.com/MoonriseSoftwareCalifornia/CosmosCMS
// for more information concerning the license and the contributors participating to this project.
// </copyright>

namespace Cosmos.Cms.Controllers
{
    using System;
    using System.Diagnostics;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using System.Web;
    using Cosmos.BlobService;
    using Cosmos.Cms.Common.Services.Configurations;
    using Cosmos.Cms.Models;
    using Cosmos.Common.Data;
    using Cosmos.Common.Models;
    using Cosmos.Editor.Data.Logic;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Identity;
    using Microsoft.AspNetCore.Identity.UI.Services;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.Logging;
    using Microsoft.Net.Http.Headers;

    /// <summary>
    /// Home page controller.
    /// </summary>
    [Authorize]
    [ResponseCache(Location = ResponseCacheLocation.None, NoStore = true)]

    public class HomeController : Controller
    {
        private readonly ArticleEditLogic articleLogic;
        private readonly EditorSettings options;
        private readonly ApplicationDbContext dbContext;
        private readonly UserManager<IdentityUser> userManager;
        private readonly StorageContext storageContext;

        /// <summary>
        /// Initializes a new instance of the <see cref="HomeController"/> class.
        /// </summary>
        /// <param name="logger">ILogger to use.</param>
        /// <param name="options"><see cref="CosmosConfig">Cosmos configuration</see>.</param>
        /// <param name="dbContext"><see cref="ApplicationDbContext">Database context</see>.</param>
        /// <param name="articleLogic"><see cref="ArticleEditLogic">Article edit logic.</see>.</param>
        /// <param name="userManager">User manager.</param>
        /// <param name="signInManager">Sign in manager service.</param>
        /// <param name="storageContext"><see cref="StorageContext">File storage context</see>.</param>
        /// <param name="emailSender">Email service.</param>
        /// <param name="configuration">Website configuration.</param>
        /// <param name="services">Services provider.</param>
        public HomeController(
            ILogger<HomeController> logger,
            IEditorSettings options,
            ApplicationDbContext dbContext,
            ArticleEditLogic articleLogic,
            UserManager<IdentityUser> userManager,
            SignInManager<IdentityUser> signInManager,
            StorageContext storageContext,
            IEmailSender emailSender,
            IConfiguration configuration,
            IServiceProvider services)
        {
            // This handles injection manually to make sure everything is setup.
            this.options = (EditorSettings)options;
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
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            ArticleViewModel article;

            if (articleNumber == null)
            {
                article = await articleLogic.GetArticleByUrl(target);
            }
            else
            {
                article = await articleLogic.GetArticleByArticleNumber(articleNumber.Value, versionNumber.Value);
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
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var article = await articleLogic.GetArticleByUrl(target);

            var data = await dbContext.Articles.OrderByDescending(o => o.VersionNumber)
                .Where(a => a.ArticleNumber == article.ArticleNumber).Select(s => new ArticleEditMenuItem
                {
                    Id = s.Id,
                    ArticleNumber = s.ArticleNumber,
                    Published = s.Published,
                    VersionNumber = s.VersionNumber,
                    UsesHtmlEditor = s.Content != null && (s.Content.ToLower().Contains(" editable=") || s.Content.ToLower().Contains(" data-ccms-ceid="))
                }).OrderByDescending(o => o.VersionNumber).Take(1).ToListAsync();

            return Json(data);
        }

        /// <summary>
        /// Gets the index page.
        /// </summary>
        /// <param name="lang">Language code.</param>
        /// <param name="mode">json or nothing.</param>
        /// <param name="layoutId">Layout ID when previewing a layout.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        [AllowAnonymous]
        public async Task<IActionResult> Index(string lang = "", string mode = "", Guid? layoutId = null)
        {
            if (User.Identity?.IsAuthenticated == false)
            {
                // If we require authentication, redirect to the login page.
                Response.Cookies.Delete("CosmosAuthCookie");
                return Redirect("~/Identity/Account/Login");
            }

            // Make sure the user's claims identity has an account here.
            var user = await userManager.GetUserAsync(User);

            if (user == null)
            {
                Response.Cookies.Delete("CosmosAuthCookie");
                return Redirect("~/Identity/Account/Logout");
            }

            if (options.AllowSetup && (await dbContext.Users.CountAsync()) == 1 && !User.IsInRole("Administrators"))
            {
                await userManager.AddToRoleAsync(user, "Administrators");
            }

            if (options.AllowSetup)
            {
                // Enable static website for Azure BLOB storage
                if (!options.CosmosRequiresAuthentication)
                {
                    await storageContext.EnableAzureStaticWebsite();
                }
                else
                {
                    await storageContext.DisableAzureStaticWebsite();
                }
            }

            if (this.options.IsMultiTenantEditor && !EnsureSettingsExist())
            {
                // If we do not yet have a layout, go to a page where we can select one.
                return RedirectToAction("Index", "Cosmos___Settings");
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
            var article = await articleLogic.GetArticleByUrl(HttpContext.Request.Path, lang);

            // Article not found?
            // try getting a version not published.
            if (article == null)
            {
                // See if a page is un-published, but does exist, let us edit it.
                article = await articleLogic.GetArticleByUrl(HttpContext.Request.Path, lang, false);

                if (article != null)
                {
                    // Go into edit mode
                    if (!string.IsNullOrEmpty(article.Content) && article.Content.ToLower().Contains(" data-ccms-ceid="))
                    {
                        return RedirectToAction("Edit", "Editor", new { id = article.ArticleNumber });
                    }

                    return RedirectToAction("EditCode", "Editor", new { id = article.ArticleNumber });
                }

                // Create your own not found page for a graceful page for users.
                article = await articleLogic.GetArticleByUrl("/not_found", lang);

                HttpContext.Response.StatusCode = 404;

                if (article == null)
                {
                    return NotFound();
                }
            }

            article.EditModeOn = false;
            article.ReadWriteMode = true;

            // If preview mode is on for a layout, set the new layout here.
            if (layoutId.HasValue)
            {
                ViewData["LayoutId"] = layoutId.Value.ToString();
                article.Layout = new LayoutViewModel(await dbContext.Layouts.FirstOrDefaultAsync(f => f.Id == layoutId));
                return View("~/Views/Home/Preview.cshtml", article);
            }

            return View(article);
        }

        /// <summary>
        ///     Gets an article by its ID (or row key).
        /// </summary>
        /// <param name="articleNumber">Article number.</param>
        /// <param name="versionNumber">Version number.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public async Task<IActionResult> Preview(int articleNumber, int? versionNumber = null)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            ViewData["EditModeOn"] = false;
            var article = await articleLogic.GetArticleByArticleNumber(articleNumber, versionNumber);

            // Home/Preview/154
            if (article != null)
            {
                article.ReadWriteMode = false;
                article.EditModeOn = false;

                return View("Preview", article);
            }

            return NotFound();
        }

        /// <summary>
        /// Gets the error page.
        /// </summary>
        /// <returns>Returns an <see cref="IActionResult"/>.</returns>
        public IActionResult Error()
        {
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
            model.associatedApplications.Add(new AssociatedApplication() { applicationId = options.MicrosoftAppId });

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
        /// Ensures there are settings if this is in multi-tenant mode.
        /// </summary>
        /// <returns>Success or not.</returns>
        private bool EnsureSettingsExist()
        {
            return !string.IsNullOrWhiteSpace(options.PublisherUrl);
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
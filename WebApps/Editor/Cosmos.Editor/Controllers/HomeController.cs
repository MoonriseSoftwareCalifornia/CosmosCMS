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
    using Cosmos.BlobService;
    using Cosmos.Cms.Common.Services.Configurations;
    using Cosmos.Cms.Data.Logic;
    using Cosmos.Cms.Models;
    using Cosmos.Common;
    using Cosmos.Common.Data;
    using Cosmos.Common.Models;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Cors;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Identity;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Options;
    using Microsoft.Net.Http.Headers;

    /// <summary>
    /// Home page controller.
    /// </summary>
    [Authorize]
    [ResponseCache(NoStore = true)]
    public class HomeController : Controller
    {
        private readonly ArticleEditLogic _articleLogic;
        private readonly IOptions<CosmosConfig> _options;
        private readonly ApplicationDbContext _dbContext;
        private readonly ILogger<HomeController> _logger;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly StorageContext _storageContext;
        // private readonly SignInManager<IdentityUser> _signInManager;
        #region SETUP TESTS

        /// <summary>
        /// Insures there is an administrator.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        private async Task<bool> EnsureAdminSetup()
        {
            await _dbContext.Database.EnsureCreatedAsync();
            return await _dbContext.Users.CosmosAnyAsync() && (await _userManager.GetUsersInRoleAsync("Administrators")).Any();
        }

        /// <summary>
        /// Ensures there is a Layout.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        private async Task<bool> EnsureLayoutExists()
        {
            return await _dbContext.Layouts.CosmosAnyAsync();
        }

        /// <summary>
        /// Ensures there is at least one article.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        private async Task<bool> EnsureArticleExists()
        {
            return await _dbContext.Articles.CosmosAnyAsync();
        }

        #endregion

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="cosmosConfig"></param>
        /// <param name="dbContext"></param>
        /// <param name="articleLogic"></param>
        /// <param name="userManager"></param>
        /// <param name="storageContext"></param>
        public HomeController(ILogger<HomeController> logger,
            IOptions<CosmosConfig> cosmosConfig,
            ApplicationDbContext dbContext,
            ArticleEditLogic articleLogic,
            UserManager<IdentityUser> userManager,
            StorageContext storageContext
            )
        {
            _logger = logger;
            _options = cosmosConfig;
            _articleLogic = articleLogic;
            _dbContext = dbContext;
            _userManager = userManager;
            _storageContext = storageContext;
        }

        /// <summary>
        /// Editor home index method.
        /// </summary>
        /// <param name="target"></param>
        /// <param name="articleNumber"></param>
        /// <param name="versionNumber"></param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public async Task<IActionResult> CcmsContentIndex(string target, int? articleNumber = null, int? versionNumber = null)
        {
            ArticleViewModel article;

            if (articleNumber == null)
            {
                article = await _articleLogic.GetByUrl(target);
            }
            else
            {
                article = await _articleLogic.Get(articleNumber.Value, versionNumber.Value);
            }

            return View(article);
        }

        /// <summary>
        /// Gets contents in an article folder.
        /// </summary>
        /// <param name="path"></param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public async Task<IActionResult> CCMS_GetArticleFolderContents(string path = "")
        {
            string r = Request.Headers["referer"];
            var url = new Uri(r);

            int articleNumber;

            // This is just for the Editor
            if (url.Query.Contains("articleNumber"))
            {
                var query = url.Query.Split('=');
                articleNumber = int.Parse(query[1]);
            }
            else if (url.PathAndQuery.ToLower().Contains("editor/ccmscontent"))
            {
                var query = url.PathAndQuery.Split('/');
                articleNumber = int.Parse(query.LastOrDefault());
            }
            else
            {
                var page = await _dbContext.Pages.Select(s => new { s.ArticleNumber, s.UrlPath }).FirstOrDefaultAsync(f => f.UrlPath == url.AbsolutePath.TrimStart('/'));

                if (page == null)
                {
                    return Json("[]");
                }

                articleNumber = page.ArticleNumber;
            }

            var contents = await CosmosUtilities.GetArticleFolderContents(_storageContext, articleNumber, path);

            return Json(contents);
        }

        /// <summary>
        /// Get edit list.
        /// </summary>
        /// <param name="target"></param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public async Task<IActionResult> EditList(string target)
        {
            var article = await _articleLogic.GetByUrl(target);

            var data = await _dbContext.Articles.OrderByDescending(o => o.VersionNumber)
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
            // if (_options.Value.SiteSettings.AllowSetup ?? false)
            // {
            //    if (!await EnsureAdminSetup())
            //    {
            //        return RedirectToAction("Index", "Setup");
            //    }
            // }
            try
            {
                if (User.Identity?.IsAuthenticated == false)
                {
                    // See if we need to register a new user.
                    if (await _dbContext.Users.CosmosAnyAsync())
                    {
                        return Redirect("~/Identity/Account/Login");
                    }

                    return Redirect("~/Identity/Account/Register");
                }
                else
                {
                    // Make sure the user's claims identity has an account here.
                    var user = await _userManager.GetUserAsync(User);

                    if (user == null)
                    {
                        Response.Cookies.Delete("CosmosAuthCookie");
                        return Redirect("~/Identity/Account/Logout");
                    }

                    if (_options.Value.SiteSettings.AllowSetup && (await _dbContext.Users.CountAsync()) == 1 && !User.IsInRole("Administrators"))
                    {
                        await _userManager.AddToRoleAsync(user, "Administrators");
                    }

                    if (!User.IsInRole("Reviewers") && !User.IsInRole("Authors") && !User.IsInRole("Editors") &&
                        !User.IsInRole("Administrators"))
                    {
                        return RedirectToAction("AccessPending");
                    }
                }

                if (_options.Value.SiteSettings.AllowSetup)
                {
                    // Enable static website for Azure BLOB storage
                    await _storageContext.EnableAzureStaticWebsite();
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
                var article = await _articleLogic.GetByUrl(HttpContext.Request.Path, HttpContext.Request.Query["lang"]); // ?? await _articleLogic.GetByUrl(id, langCookie);

                // Article not found?
                // try getting a version not published.
                if (article == null)
                {
                    // Create your own not found page for a graceful page for users.
                    article = await _articleLogic.GetByUrl("/not_found", HttpContext.Request.Query["lang"]);

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
                _logger.LogError(e, e.Message);
                throw;
            }
        }

        /// <summary>
        ///     Gets an article by its ID (or row key).
        /// </summary>
        /// <param name="articleNumber"></param>
        /// <param name="versionNumber"></param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public async Task<IActionResult> Preview(int articleNumber, int? versionNumber = null)
        {
            try
            {
                ViewData["EditModeOn"] = false;
                var article = await _articleLogic.Get(articleNumber, versionNumber);

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
                _logger.LogError(e, e.Message);
                throw;
            }
        }

        /// <summary>
        /// Error page.
        /// </summary>
        /// <returns></returns>
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
        /// <returns></returns>
        [AllowAnonymous]
        public IActionResult GetMicrosoftIdentityAssociation()
        {
            var model = new MicrosoftValidationObject();
            model.associatedApplications.Add(new AssociatedApplication() { applicationId = _options.Value.MicrosoftAppId });

            var data = Newtonsoft.Json.JsonConvert.SerializeObject(model);

            return File(Encoding.UTF8.GetBytes(data), "application/json", fileDownloadName: "microsoft-identity-association.json");
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
                _ = await _dbContext.Users.Select(s => s.Id).FirstOrDefaultAsync();
                return Ok();
            }
            catch
            {
            }

            return StatusCode(500);
        }

        #region STATIC WEB PAGES

        /// <summary>
        /// Returns if a user has not been granted access yet.
        /// </summary>
        /// <returns></returns>
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

        #endregion

        #region API

        /// <summary>
        /// Gets the children of a given page path.
        /// </summary>
        /// <param name="page">URL path to paren.</param>
        /// <param name="pageNo"></param>
        /// <param name="pageSize"></param>
        /// <param name="orderByPub">Order by publishing date.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        [EnableCors("AllCors")]
        public async Task<IActionResult> GetTOC(
            string page,
            bool? orderByPub,
            int? pageNo,
            int? pageSize)
        {
            var result = await _articleLogic.GetTOC(page, pageNo ?? 0, pageSize ?? 10, orderByPub ?? false);
            return Json(result);
        }

        #endregion
    }
}
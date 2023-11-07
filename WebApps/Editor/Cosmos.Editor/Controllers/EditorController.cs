// <copyright file="EditorController.cs" company="Moonrise Software, LLC">
// Copyright (c) Moonrise Software, LLC. All rights reserved.
// Licensed under the GNU Public License, Version 3.0 (https://www.gnu.org/licenses/gpl-3.0.html)
// See https://github.com/MoonriseSoftwareCalifornia/CosmosCMS
// for more information concerning the license and the contributors participating to this project.
// </copyright>

namespace Cosmos.Cms.Controllers
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using System.Web;
    using Cosmos.Cms.Common.Services.Configurations;
    using Cosmos.Cms.Data.Logic;
    using Cosmos.Cms.Models;
    using Cosmos.Cms.Services;
    using Cosmos.Common.Data;
    using Cosmos.Common.Data.Logic;
    using Cosmos.Common.Models;
    using Cosmos.Editor.Models;
    using HtmlAgilityPack;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Identity;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.AspNetCore.Mvc.ModelBinding;
    using Microsoft.Azure.Cosmos.Serialization.HybridRow;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Options;

    /// <summary>
    /// Editor controller.
    /// </summary>
    // [ResponseCache(NoStore = true)]
    [Authorize(Roles = "Reviewers, Administrators, Editors, Authors")]
    public class EditorController : BaseController
    {
        private readonly ArticleEditLogic articleLogic;
        private readonly ApplicationDbContext dbContext;
        private readonly ILogger<EditorController> logger;
        private readonly IOptions<CosmosConfig> options;
        private readonly RoleManager<IdentityRole> roleManager;
        private readonly UserManager<IdentityUser> userManager;
        private readonly Uri blobPublicAbsoluteUrl;
        private readonly IViewRenderService viewRenderService;

        /// <summary>
        /// Initializes a new instance of the <see cref="EditorController"/> class.
        /// </summary>
        /// <param name="logger">ILogger to use.</param>
        /// <param name="dbContext">Database context.</param>
        /// <param name="userManager">User manager.</param>
        /// <param name="roleManager">Role manager.</param>
        /// <param name="articleLogic">Article logic.</param>
        /// <param name="options">Cosmos options.</param>
        /// <param name="viewRenderService">View rendering service.</param>
        public EditorController(
            ILogger<EditorController> logger,
            ApplicationDbContext dbContext,
            UserManager<IdentityUser> userManager,
            RoleManager<IdentityRole> roleManager,
            ArticleEditLogic articleLogic,
            IOptions<CosmosConfig> options,
            IViewRenderService viewRenderService)
            : base(dbContext, userManager, articleLogic, options)
        {
            this.logger = logger;
            this.dbContext = dbContext;
            this.options = options;
            this.roleManager = roleManager;
            this.userManager = userManager;
            this.articleLogic = articleLogic;
            var htmlUtilities = new HtmlUtilities();

            if (htmlUtilities.IsAbsoluteUri(options.Value.SiteSettings.BlobPublicUrl))
            {
                blobPublicAbsoluteUrl = new Uri(options.Value.SiteSettings.BlobPublicUrl);
            }
            else
            {
                blobPublicAbsoluteUrl = new Uri(options.Value.SiteSettings.PublisherUrl.TrimEnd('/') + "/" + options.Value.SiteSettings.BlobPublicUrl.TrimStart('/'));
            }

            this.viewRenderService = viewRenderService;
        }

        /// <summary>
        /// Catalog of web pages on this website.
        /// </summary>
        /// <param name="sortOrder">Sort order either asc or desc.</param>
        /// <param name="currentSort">Current sort field.</param>
        /// <param name="pageNo">Page number.</param>
        /// <param name="pageSize">Number of records per page.</param>
        /// <param name="filter">Search filter.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public async Task<IActionResult> Index(string sortOrder, string currentSort, int pageNo = 0, int pageSize = 10, string filter = "")
        {
            ViewData["HomePageArticleNumber"] = await dbContext.Pages.Where(f => f.UrlPath == "root").Select(s => s.ArticleNumber).FirstOrDefaultAsync();
            ViewData["PublisherUrl"] = options.Value.SiteSettings.PublisherUrl;
            ViewData["ShowFirstPageBtn"] = await dbContext.Articles.CosmosAnyAsync() == false;
            ViewData["ShowNotFoundBtn"] = await dbContext.ArticleCatalog.Where(w => w.UrlPath == "not_found").CosmosAnyAsync() == false;

            if (!string.IsNullOrEmpty(filter))
            {
                filter = filter.TrimStart('/');
            }

            ViewData["Filter"] = filter;

            ViewData["sortOrder"] = sortOrder;
            ViewData["currentSort"] = currentSort;
            ViewData["pageNo"] = pageNo;
            ViewData["pageSize"] = pageSize;

            var query = dbContext.ArticleCatalog.AsQueryable();

            ViewData["RowCount"] = await query.CountAsync();

            if (!string.IsNullOrEmpty(filter))
            {
                var f = filter.ToLower();
                query = query.Where(w => w.Title.ToLower().Contains(f));
            }

            if (sortOrder == "desc")
            {
                if (!string.IsNullOrEmpty(currentSort))
                {
                    switch (currentSort)
                    {
                        case "ArticleNumber":
                            query = query.OrderByDescending(o => o.ArticleNumber);
                            break;
                        case "Title":
                            query = query.OrderByDescending(o => o.Title);
                            break;
                        case "LastPublished":
                            query = query.OrderByDescending(o => o.Published);
                            break;
                        case "UrlPath":
                            query = query.OrderByDescending(o => o.UrlPath);
                            break;
                        case "Status":
                            query = query.OrderByDescending(o => o.Status);
                            break;
                        case "Updated":
                            query = query.OrderByDescending(o => o.Updated);
                            break;
                    }
                }
            }
            else
            {
                if (!string.IsNullOrEmpty(currentSort))
                {
                    switch (currentSort)
                    {
                        case "ArticleNumber":
                            query = query.OrderBy(o => o.ArticleNumber);
                            break;
                        case "Title":
                            query = query.OrderBy(o => o.Title);
                            break;
                        case "LastPublished":
                            query = query.OrderBy(o => o.Published);
                            break;
                        case "UrlPath":
                            query = query.OrderBy(o => o.UrlPath);
                            break;
                        case "Status":
                            query = query.OrderBy(o => o.Status);
                            break;
                        case "Updated":
                            query = query.OrderBy(o => o.Updated);
                            break;
                    }
                }
                else
                {
                    // Default sort order
                    query = query.OrderBy(o => o.Title);
                }
            }

            var model = query.Select(s => new ArticleListItem()
            {
                ArticleNumber = s.ArticleNumber,
                Title = s.Title,
                IsDefault = s.UrlPath == "root",
                LastPublished = s.Published,
                UrlPath = s.UrlPath,
                Status = s.Status,
                Updated = s.Updated
            }).Skip(pageNo * pageSize).Take(pageSize);

            var data = await model.ToListAsync();

            return View(data);
        }

        ///<summary>
        ///     Gets all the versions for an article.
        /// </summary>
        /// <param name="id">Article number.</param>
        /// <param name="sortOrder">Sort order is either asc or desc.</param>
        /// <param name="currentSort">Field to sort on.</param>
        /// <param name="pageNo">Page to return.</param>
        /// <param name="pageSize">Number of records per page.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public async Task<IActionResult> Versions(int? id, string sortOrder = "desc", string currentSort = "VersionNumber", int pageNo = 0, int pageSize = 10)
        {
            if (id == null)
            {
                return RedirectToAction("Index");
            }

            ViewData["sortOrder"] = sortOrder;
            ViewData["currentSort"] = currentSort;
            ViewData["pageNo"] = pageNo;
            ViewData["pageSize"] = pageSize;
            ViewData["articleNumber"] = id;

            var articleNumber = id.Value;

            var query = dbContext.Articles.Where(w => w.ArticleNumber == articleNumber).Select(s => new ArticleVersionViewModel
            {
                Id = s.Id,
                Published = s.Published,
                Title = s.Title,
                Updated = s.Updated,
                VersionNumber = s.VersionNumber,
                Expires = s.Expires,
                UserId = s.UserId,
                UsesHtmlEditor = s.Content != null && s.Content != string.Empty && (s.Content.ToLower().Contains(" contenteditable=") || s.Content.ToLower().Contains(" data-ccms-ceid="))
            }).AsQueryable();

            var test = await query.ToListAsync();

            ViewData["RowCount"] = await dbContext.Articles.Where(w => w.ArticleNumber == id).CountAsync();
            ViewData["LastVersion"] = await dbContext.Articles.Where(w => w.ArticleNumber == id).MaxAsync(m => m.VersionNumber);

            if (sortOrder == "desc")
            {
                if (!string.IsNullOrEmpty(currentSort))
                {
                    switch (currentSort)
                    {
                        case "Published":
                            query = query.OrderByDescending(o => o.Published);
                            break;
                        case "Updated":
                            query = query.OrderByDescending(o => o.Updated);
                            break;
                        case "VersionNumber":
                            query = query.OrderByDescending(o => o.VersionNumber);
                            break;
                        case "Expires":
                            query = query.OrderByDescending(o => o.Expires);
                            break;
                    }
                }
            }
            else
            {
                if (!string.IsNullOrEmpty(currentSort))
                {
                    switch (currentSort)
                    {
                        case "Published":
                            query = query.OrderBy(o => o.Published);
                            break;
                        case "Updated":
                            query = query.OrderBy(o => o.Updated);
                            break;
                        case "VersionNumber":
                            query = query.OrderBy(o => o.VersionNumber);
                            break;
                        case "Expires":
                            query = query.OrderBy(o => o.Expires);
                            break;
                    }
                }
            }

            var article = await dbContext.Articles.Where(a => a.ArticleNumber == id.Value)
                .Select(s => new { s.Title, s.VersionNumber }).FirstOrDefaultAsync();

            ViewData["ArticleTitle"] = article.Title;
            ViewData["ArticleId"] = id.Value;

            var skip = pageNo * pageSize;

            var data = await query.Skip(skip).Take(pageSize).ToListAsync();

            return View(data);
        }

        /// <summary>
        /// Saves live editor data.
        /// </summary>
        /// <param name="model"></param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        /// <exception cref="Exception"></exception>
        [HttpPost]
        public async Task<IActionResult> SaveLiveEditorData(LiveEditorSignal model)
        {
            var t = model;

            if (string.IsNullOrEmpty(model.Title))
            {
                throw new Exception("Title cannot be null or empty.");
            }

            if (model == null)
            {
                throw new Exception("SaveEditorContent method, model was null.");
            }

            // Next pull the original. This is a view model, not tracked by DbContext.
            var article = await articleLogic.Get(model.ArticleNumber, null);
            if (article == null)
            {
                throw new Exception($"SIGNALR: SaveEditorContent method, could not find artile with #: {model.ArticleNumber}.");
            }

            // Handle empty areas
            if (model.Data == null)
            {
                model.Data = string.Empty;
            }

            // Get the editable regions from the original document.
            var originalHtmlDoc = new HtmlDocument();
            originalHtmlDoc.LoadHtml(article.Content);
            var originalEditableDivs = originalHtmlDoc.DocumentNode.SelectNodes("//*[@data-ccms-ceid]");

            // Find the region we are updating
            var target = originalEditableDivs.FirstOrDefault(w => w.Attributes["data-ccms-ceid"].Value == model.EditorId);
            if (target != null)
            {
                // Update the region now
                target.InnerHtml = model.Data;
            }

            // Now carry over what's being updated to the original.
            article.Content = originalHtmlDoc.DocumentNode.OuterHtml;

            // Make sure we are setting to the orignal updated date/time
            // This is validated to make sure that someone else hasn't already edited this
            // entity
            article.Published = model.Published;
            article.Updated = DateTimeOffset.UtcNow;
            article.Title = model.Title;

            // Save changes back to the database
            var result = await articleLogic.Save(article, model.UserId);

            return Json(result.Model.VersionNumber);
        }

        /// <summary>
        /// Open trash.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        [Authorize(Roles = "Administrators, Editors, Authors")]
        public async Task<IActionResult> Trash(string sortOrder, string currentSort, int pageNo = 0, int pageSize = 10)
        {
            ViewData["sortOrder"] = sortOrder;
            ViewData["currentSort"] = currentSort;
            ViewData["pageNo"] = pageNo;
            ViewData["pageSize"] = pageSize;

            var data = await articleLogic.GetArticleTrashList();
            var query = data.AsQueryable();

            ViewData["RowCount"] = query.Count();

            if (sortOrder == "desc")
            {
                if (!string.IsNullOrEmpty(currentSort))
                {
                    switch (currentSort)
                    {
                        case "ArticleNumber":
                            query = query.OrderByDescending(o => o.ArticleNumber);
                            break;
                        case "Title":
                            query = query.OrderByDescending(o => o.Title);
                            break;
                        case "LastPublished":
                            query = query.OrderByDescending(o => o.LastPublished);
                            break;
                        case "UrlPath":
                            query = query.OrderByDescending(o => o.UrlPath);
                            break;
                        case "Status":
                            query = query.OrderByDescending(o => o.Status);
                            break;
                        case "Updated":
                            query = query.OrderByDescending(o => o.Updated);
                            break;
                    }
                }
            }
            else
            {
                if (!string.IsNullOrEmpty(currentSort))
                {
                    switch (currentSort)
                    {
                        case "ArticleNumber":
                            query = query.OrderBy(o => o.ArticleNumber);
                            break;
                        case "Title":
                            query = query.OrderBy(o => o.Title);
                            break;
                        case "LastPublished":
                            query = query.OrderBy(o => o.LastPublished);
                            break;
                        case "UrlPath":
                            query = query.OrderBy(o => o.UrlPath);
                            break;
                        case "Status":
                            query = query.OrderBy(o => o.Status);
                            break;
                        case "Updated":
                            query = query.OrderBy(o => o.Updated);
                            break;
                    }
                }
            }

            return View(query.Skip(pageNo * pageSize).Take(pageSize).ToList());
        }

        /// <summary>
        /// Compare two versions.
        /// </summary>
        /// <param name="leftId">Article ID of the left version.</param>
        /// <param name="rightId">Article ID of the right version.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public async Task<IActionResult> Compare(Guid leftId, Guid rightId)
        {
            var left = await articleLogic.Get(leftId, EnumControllerName.Edit, await GetUserId());
            var right = await articleLogic.Get(rightId, EnumControllerName.Edit, await GetUserId());
            @ViewData["PageTitle"] = left.Title;

            ViewData["LeftVersion"] = left.VersionNumber;
            ViewData["RightVersion"] = right.VersionNumber;

            var model = new CompareCodeViewModel()
            {
                EditorTitle = left.Title,
                EditorFields = new[]
                {
                    new EditorField
                    {
                        FieldId = "HeadJavaScript",
                        FieldName = "Head Block",
                        EditorMode = EditorMode.Html,
                        IconUrl = "/images/seti-ui/icons/html.svg",
                        ToolTip = "Content to appear at the bottom of the <head> tag."
                    },
                    new EditorField
                    {
                        FieldId = "Content",
                        FieldName = "Html Content",
                        EditorMode = EditorMode.Html,
                        IconUrl = "~/images/seti-ui/icons/html.svg",
                        ToolTip = "Content to appear in the <body>."
                    },
                    new EditorField
                    {
                        FieldId = "FooterJavaScript",
                        FieldName = "Footer Block",
                        EditorMode = EditorMode.Html,
                        IconUrl = "~/images/seti-ui/icons/html.svg",
                        ToolTip = "Content to appear at the bottom of the <body> tag."
                    }
                },
                Articles = new ArticleViewModel[] { left, right }
            };
            return View(model);
        }

        /// <summary>
        /// Gets template page information.
        /// </summary>
        /// <param name="Id"></param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        [Authorize(Roles = "Administrators, Editors, Authors, Team Members")]
        public async Task<IActionResult> GetTemplateInfo(Guid? Id)
        {
            if (Id == null)
            {
                return Json(string.Empty);
            }

            var model = await dbContext.Templates.FirstOrDefaultAsync(f => f.Id == Id.Value);

            return Json(model);
        }

        /// <summary>
        ///     Creates a <see cref="CreatePageViewModel" /> used to create a new article.
        /// </summary>
        /// <param name="title">Name of new page if known.</param>
        /// <param name="sortOrder"></param>
        /// <param name="currentSort"></param>
        /// <param name="pageNo"></param>
        /// <param name="pageSize"></param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        [Authorize(Roles = "Administrators, Editors, Authors, Team Members")]
        public async Task<IActionResult> Create(string title = "", string sortOrder = "asc", string currentSort = "Title", int pageNo = 0, int pageSize = 20)
        {
            var defautLayout = await dbContext.Layouts.FirstOrDefaultAsync(l => l.IsDefault);

            ViewData["Layouts"] = await BaseGetLayoutListItems();

            ViewData["sortOrder"] = sortOrder;
            ViewData["currentSort"] = currentSort;
            ViewData["pageNo"] = pageNo;
            ViewData["pageSize"] = pageSize;
            var reserved = await articleLogic.GetReservedPaths();
            var existingUrls = await dbContext.Articles.Where(w => w.StatusCode == (int)StatusCodeEnum.Active).Select(s => s.Title).Distinct().ToListAsync();
            existingUrls.AddRange(reserved.Select(s => s.Path));
            ViewData["reservedPaths"] = existingUrls;

            var query = dbContext.Templates.OrderBy(t => t.Title)
                .Where(w => w.LayoutId == defautLayout.Id)
                .Select(s => new TemplateIndexViewModel
                {
                    Id = s.Id,
                    LayoutName = defautLayout.LayoutName,
                    Description = s.Description,
                    Title = s.Title,
                    UsesHtmlEditor = s.Content.ToLower().Contains(" contenteditable=") || s.Content.ToLower().Contains(" data-ccms-ceid=")
                }).AsQueryable();

            ViewData["RowCount"] = await query.CountAsync();

            if (sortOrder == "desc")
            {
                if (!string.IsNullOrEmpty(currentSort))
                {
                    switch (currentSort)
                    {
                        case "LayoutName":
                            query = query.OrderByDescending(o => o.LayoutName);
                            break;
                        case "Description":
                            query = query.OrderByDescending(o => o.Description);
                            break;
                        case "Title":
                            query = query.OrderByDescending(o => o.Title);
                            break;
                    }
                }
            }
            else
            {
                if (!string.IsNullOrEmpty(currentSort))
                {
                    switch (currentSort)
                    {
                        case "LayoutName":
                            query = query.OrderBy(o => o.LayoutName);
                            break;
                        case "Title":
                            query = query.OrderBy(o => o.Title);
                            break;
                        case "Description":
                            query = query.OrderBy(o => o.Description);
                            break;
                    }
                }
            }

            ViewData["TemplateList"] = await query.Skip(pageNo * pageSize).Take(pageSize).ToListAsync();

            return View(new CreatePageViewModel()
            {
                Id = Guid.NewGuid(),
                Title = title
            });
        }

        /// <summary>
        ///     Uses <see cref="ArticleEditLogic.Create(string, string, Guid?)" /> to create an <see cref="ArticleViewModel" /> that is
        ///     saved to
        ///     the database with <see cref="ArticleEditLogic.Save(ArticleViewModel, string)" /> ready for editing.
        /// </summary>
        /// <param name="model"></param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        [Authorize(Roles = "Administrators, Editors, Authors, Team Members")]
        [HttpPost]
        public async Task<IActionResult> Create(CreatePageViewModel model)
        {
            if (ModelState.IsValid)
            {
                if (model == null)
                {
                    return NotFound();
                }

                model.Title = model.Title.TrimStart('/');

                var validTitle = await articleLogic.ValidateTitle(model.Title, null);

                if (!validTitle)
                {
                    ModelState.AddModelError("Title", $"Title: {model.Title} conflicts with another article title or reserved word.");
                    return View(model);
                }

                var article = await articleLogic.Create(model.Title, await GetUserEmail(), model.TemplateId);

                return RedirectToAction("Versions", "Editor", new { Id = article.ArticleNumber });
            }

            var defautLayout = await dbContext.Layouts.FirstOrDefaultAsync(l => l.IsDefault);

            ViewData["Layouts"] = await BaseGetLayoutListItems();

            ViewData["sortOrder"] = "asc";
            ViewData["currentSort"] = "title";
            ViewData["pageNo"] = 0;
            ViewData["pageSize"] = 20;

            var query = dbContext.Templates.OrderBy(t => t.Title)
               .Where(w => w.LayoutId == defautLayout.Id)
               .Select(s => new TemplateIndexViewModel
               {
                   Id = s.Id,
                   LayoutName = defautLayout.LayoutName,
                   Description = s.Description,
                   Title = s.Title,
                   UsesHtmlEditor = s.Content.ToLower().Contains(" contenteditable=") || s.Content.ToLower().Contains(" data-ccms-ceid=")
               }).AsQueryable();

            ViewData["RowCount"] = await query.CountAsync();
            ViewData["TemplateList"] = await query.Skip(0 * 20).Take(20).ToListAsync();

            return View(model);
        }

        /// <summary>
        ///     Creates a new version for an article and redirects to editor.
        /// </summary>
        /// <param name="id">Article ID.</param>
        /// <param name="entityId">Entity Id to use as new version.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        [Authorize(Roles = "Administrators, Editors, Authors")]
        public async Task<IActionResult> CreateVersion(int id, Guid? entityId = null)
        {
            // Grab the latest versions regardless
            var latest = await dbContext.Articles.OrderByDescending(o => o.VersionNumber).FirstOrDefaultAsync(f =>
                    f.ArticleNumber == id);

            // This is the article that we will edit
            Article article;

            // Are we basing this on an existing entity?
            if (entityId == null)
            {
                // Yes we are, target that version now.
                article = latest;
            }
            else
            {
                // We are here because the new version is being based on a
                // specific older version, not the latest version.
                //
                //
                // Create a new version based on a specific version
                article = await dbContext.Articles.FirstOrDefaultAsync(f =>
                    f.Id == entityId.Value);
            }

            var newArticle = new Article()
            {
                Id = Guid.NewGuid(),
                ArticleNumber = article.ArticleNumber,
                BannerImage = article.BannerImage,
                Content = article.Content,
                Expires = article.Expires,
                FooterJavaScript = article.FooterJavaScript,
                HeaderJavaScript = article.HeaderJavaScript,
                Published = article.Published,
                StatusCode = article.StatusCode,
                Title = article.Title,
                Updated = article.Updated,
                UrlPath = article.UrlPath,
                UserId = User.Identity.Name,
                VersionNumber = latest.VersionNumber + 1
            };

            dbContext.Articles.Add(newArticle);

            await dbContext.SaveChangesAsync();

            return RedirectToAction("EditCode", "Editor", new { id = newArticle.ArticleNumber });
        }

        /// <summary>
        /// Create a duplicate page from a specified page.
        /// </summary>
        /// <param name="id"></param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public async Task<IActionResult> Clone(int id)
        {
            var lastVersion = await dbContext.Articles.Where(a => a.ArticleNumber == id).MaxAsync(m => m.VersionNumber);

            var articleViewModel = await articleLogic.Get(id, lastVersion);

            ViewData["Original"] = articleViewModel;

            if (articleViewModel == null)
            {
                return NotFound();
            }

            var model = new DuplicateViewModel()
            {
                Id = articleViewModel.Id,
                Published = articleViewModel.Published,
                Title = articleViewModel.Title,
                ArticleId = articleViewModel.ArticleNumber,
                ArticleVersion = articleViewModel.VersionNumber
            };

            return View(model);
        }

        /// <summary>
        /// Creates a duplicate page from a specified page and version.
        /// </summary>
        /// <param name="model"></param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Administrators, Editors, Authors")]
        public async Task<IActionResult> Clone(DuplicateViewModel model)
        {
            string title = string.Empty;

            if (string.IsNullOrEmpty(model.ParentPageTitle))
            {
                title = model.Title;
            }
            else
            {
                title = $"{model.ParentPageTitle.Trim('/')}/{model.Title.Trim('/')} ";
            }

            if (await dbContext.Articles.Where(a => a.Title.ToLower() == title.ToLower()).CosmosAnyAsync())
            {
                if (string.IsNullOrEmpty(model.ParentPageTitle))
                {
                    ModelState.AddModelError("Title", "Page title already taken.");
                }
                else
                {
                    ModelState.AddModelError("Title", "Sub-page title already taken.");
                }
            }

            var userId = await GetUserId();

            var articleViewModel = await articleLogic.Get(model.Id, EnumControllerName.Edit, userId);

            if (ModelState.IsValid)
            {
                articleViewModel.ArticleNumber = 0;
                articleViewModel.Id = Guid.NewGuid();
                articleViewModel.Published = model.Published;
                articleViewModel.Title = title;

                try
                {
                    var clone = await articleLogic.Create(articleViewModel.Title, userId);
                    clone.StatusCode = articleViewModel.StatusCode;
                    clone.CacheDuration = articleViewModel.CacheDuration;
                    clone.Content = articleViewModel.Content;
                    clone.FooterJavaScript = articleViewModel.FooterJavaScript;
                    clone.HeadJavaScript = articleViewModel.HeadJavaScript;
                    clone.LanguageCode = articleViewModel.LanguageCode;

                    var result = await articleLogic.Save(clone, await GetUserEmail());

                    // Open the live editor if there are editable regions on the page.
                    if (result.Model.Content.Contains("editable", StringComparison.InvariantCultureIgnoreCase) ||
                        result.Model.Content.Contains("data-ccms-ceid", StringComparison.InvariantCultureIgnoreCase))
                    {
                        return RedirectToAction("Edit", new { result.Model.Id });
                    }

                    // Otherwise, open in the Monaco code editor
                    return RedirectToAction("EditCode", new { result.Model.Id });
                }
                catch (Exception e)
                {
                    ModelState.AddModelError(string.Empty, e.Message);
                }
            }

            ViewData["Original"] = articleViewModel;

            return View(model);
        }

        /// <summary>
        ///     Creates a <see cref="CreatePageViewModel" /> used to create a new article.
        /// </summary>
        /// <param name="id">Article ID.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        [Authorize(Roles = "Administrators, Editors")]
        public async Task<IActionResult> NewHome(int id)
        {
            var page = await dbContext.Articles.FirstOrDefaultAsync(f => f.ArticleNumber == id);
            return View(new NewHomeViewModel
            {
                Id = page.Id,
                ArticleNumber = page.ArticleNumber,
                Title = page.Title,
                IsNewHomePage = false,
                UrlPath = page.UrlPath
            });
        }

        /// <summary>
        /// Make a web page the new home page.
        /// </summary>
        /// <param name="model">Now home page post model.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        [HttpPost]
        [Authorize(Roles = "Administrators, Editors")]
        public async Task<IActionResult> NewHome(NewHomeViewModel model)
        {
            if (model == null)
            {
                return NotFound();
            }

            var user = await userManager.GetUserAsync(User);
            await articleLogic.NewHomePage(model, user.Email);

            return RedirectToAction("Index");
        }

        /// <summary>
        /// Recovers an article from trash.
        /// </summary>
        /// <param name="id">Article ID to recover from trash.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        [Authorize(Roles = "Administrators, Editors, Authors")]
        public async Task<IActionResult> Recover(int id)
        {
            await articleLogic.RetrieveFromTrash(id, await GetUserId());

            return RedirectToAction("Trash");
        }

        /// <summary>
        ///     Publish website dialog.
        /// </summary>
        /// <returns>Returns a view.</returns>
        [Authorize(Roles = "Administrators, Editors")]
        public IActionResult Publish()
        {
            return View();
        }

        /// <summary>
        /// Un-publishes an article.
        /// </summary>
        /// <param name="id">Article ID.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        [Authorize(Roles = "Administrators, Editors")]
        public async Task<IActionResult> UnpublishPage(int id)
        {
            await articleLogic.Unpublish(id);

            return Ok();
        }

        /// <summary>
        /// Page access permissions.
        /// </summary>
        /// <param name="id">Article ID.</param>
        /// <param name="forRoles">User roles.</param>
        /// <param name="sortOrder">Sort order asc or desc.</param>
        /// <param name="currentSort">Current sort field.</param>
        /// <param name="pageNo">Page number to return.</param>
        /// <param name="pageSize">Size of the page.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        [HttpGet]
        [Authorize(Roles = "Administrators, Editors")]
        public async Task<IActionResult> Permissions(int id, bool forRoles = true, string sortOrder = "asc", string currentSort = "Name", int pageNo = 0, int pageSize = 10)
        {
            ViewData["sortOrder"] = sortOrder;
            ViewData["currentSort"] = currentSort;
            ViewData["pageNo"] = pageNo;
            ViewData["pageSize"] = pageSize;
            ViewData["showingRoles"] = forRoles;

            var catalogEntry = await dbContext.ArticleCatalog.FirstOrDefaultAsync(f => f.ArticleNumber == id);

            // Ensure the anonymous role exists if the publisher requires authentication.
            if (options.Value.SiteSettings.CosmosRequiresAuthentication && (await roleManager.RoleExistsAsync("Anonymous")) == false)
            {
                await roleManager.CreateAsync(new IdentityRole("Anonymous"));
            }

            ViewData["ArticleNumber"] = catalogEntry.ArticleNumber;
            ViewData["ArticlePermissions"] = catalogEntry.ArticlePermissions;
            var objectIds = catalogEntry.ArticlePermissions.Select(s => s.IdentityObjectId).ToArray();

            ViewData["ViewModel"] = new ArticlePermissionsViewModel(catalogEntry, forRoles);
            ViewData["Title"] = catalogEntry.Title;
            ViewData["AllowedUsers"] = await userManager.Users.Where(w => objectIds.Contains(w.Id)).ToListAsync();


            ViewData["AllowedRoles"] = await roleManager.Roles.Where(w => objectIds.Contains(w.Id)).ToListAsync();

            IQueryable<ArticlePermisionItem> query;

            if (forRoles)
            {
                query = roleManager.Roles.Select(
                    s => new ArticlePermisionItem
                    {
                        IdentityObjectId = s.Id,
                        Name = s.Name,
                    }
                    ).AsQueryable();
            }
            else
            {
                query = userManager.Users.Select(
                    s => new ArticlePermisionItem
                    {
                        IdentityObjectId = s.Id,
                        Name = s.Email,
                    }
                    ).AsQueryable();
            }

            // Get count
            ViewData["RowCount"] = await query.CountAsync();

            if (sortOrder.Equals("desc", StringComparison.InvariantCultureIgnoreCase))
            {
                switch (currentSort)
                {
                    case "Name":
                        query = query.OrderByDescending(o => o.Name);
                        break;
                }
            }
            else
            {
                switch (currentSort)
                {
                    case "Name":
                        query = query.OrderBy(o => o.Name);
                        break;
                }
            }

            query = query.Skip(pageNo * pageSize).Take(pageSize);

            var data = await query.ToListAsync();

            return View(data);
        }

        /// <summary>
        /// Sets the permissions for an article.
        /// </summary>
        /// <param name="id">Article Number.</param>
        /// <param name="identityObjectIds">Identity object ID list.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        [HttpPost]
        [Authorize(Roles = "Administrators, Editors")]
        public async Task<IActionResult> Permissions(int id, string[] identityObjectIds)
        {
            try
            {
                var article = await dbContext.ArticleCatalog.FirstOrDefaultAsync(f => f.ArticleNumber == id);

                if (article.ArticlePermissions == null)
                {
                    article.ArticlePermissions = new List<ArticlePermission>();
                }
                else
                {
                    article.ArticlePermissions.Clear();
                }

                var roles = await dbContext.Roles.Where(w => identityObjectIds.Contains(w.Id)).ToListAsync();
                var users = await dbContext.Users.Where(w => identityObjectIds.Contains(w.Id)).ToListAsync();

                if (roles.Any())
                {
                    article.ArticlePermissions.AddRange(roles.Select(s => new ArticlePermission()
                    {
                        IdentityObjectId = s.Id,
                        IsRoleObject = true
                    }).ToArray());
                }

                if (users.Any())
                {
                    article.ArticlePermissions.AddRange(users.Select(s => new ArticlePermission()
                    {
                        IdentityObjectId = s.Id,
                        IsRoleObject = false
                    }).ToArray());
                }

                await dbContext.SaveChangesAsync();

                return Ok();
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        /// <summary>
        /// Open Cosmos logs.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        [Authorize(Roles = "Administrators, Editors")]
        public async Task<IActionResult> Logs()
        {
            var data = await dbContext.ArticleLogs
                .OrderByDescending(o => o.DateTimeStamp)
                .Select(s => new
                {
                    s.Id,
                    s.ActivityNotes,
                    s.DateTimeStamp,
                    s.IdentityUserId
                }).ToListAsync();

            var model = data.Select(s => new ArticleLogJsonModel
            {
                Id = s.Id,
                ActivityNotes = s.ActivityNotes,
                DateTimeStamp = s.DateTimeStamp.ToUniversalTime(),
                IdentityUserId = s.IdentityUserId
            }).AsQueryable();

            return View(model);
        }

        /// <summary>
        /// Gets a reserved path list.
        /// </summary>
        /// <param name="sortOrder">Sort order either asc or desc.</param>
        /// <param name="currentSort">Field to sort on.</param>
        /// <param name="pageNo">Page number to send back.</param>
        /// <param name="pageSize">Number of records per page.</param>
        /// <param name="filter">Search filter (optional).</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public async Task<IActionResult> ReservedPaths(string sortOrder, string currentSort, int pageNo = 0, int pageSize = 10, string filter = "")
        {
            var paths = await articleLogic.GetReservedPaths();

            ViewData["RowCount"] = paths.Count();

            var query = paths.AsQueryable();

            ViewData["Filter"] = filter;
            ViewData["sortOrder"] = sortOrder;
            ViewData["currentSort"] = currentSort;
            ViewData["pageNo"] = pageNo;
            ViewData["pageSize"] = pageSize;

            if (!string.IsNullOrEmpty(filter))
            {
                var f = filter.ToLower();
                query = query.Where(w => w.Path.ToLower().Contains(f));
            }

            if (sortOrder == "desc")
            {
                if (!string.IsNullOrEmpty(currentSort))
                {
                    switch (currentSort)
                    {
                        case "Path":
                            query = query.OrderByDescending(o => o.Path);
                            break;
                        case "CosmosRequired":
                            query = query.OrderByDescending(o => o.CosmosRequired);
                            break;
                        case "Notes":
                            query = query.OrderByDescending(o => o.Notes);
                            break;
                    }
                }
            }
            else
            {
                if (!string.IsNullOrEmpty(currentSort))
                {
                    switch (currentSort)
                    {
                        case "Path":
                            query = query.OrderBy(o => o.Path);
                            break;
                        case "CosmosRequired":
                            query = query.OrderBy(o => o.CosmosRequired);
                            break;
                        case "Notes":
                            query = query.OrderBy(o => o.Notes);
                            break;
                    }
                }
                else
                {
                    // Default sort order
                    query = query.OrderBy(o => o.Path);
                }
            }

            query = query.Skip(pageNo * pageSize).Take(pageSize);

            return View(query.ToList());
        }

        /// <summary>
        /// Creates a new reserved path.
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        public IActionResult CreateReservedPath()
        {
            ViewData["Title"] = "Create a Reserved Path";

            return View("~/Views/Editor/EditReservedPath.cshtml", new ReservedPath());
        }

        /// <summary>
        /// Creates a new reserved path.
        /// </summary>
        /// <param name="model"></param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        [HttpPost]
        public async Task<IActionResult> CreateReservedPath(ReservedPath model)
        {
            ViewData["Title"] = "Create a Reserved Path";

            if (ModelState.IsValid)
            {
                try
                {
                    await articleLogic.AddOrUpdateReservedPath(model);
                    return RedirectToAction("ReservedPaths");
                }
                catch (Exception e)
                {
                    ModelState.AddModelError("Path", e.Message);
                }
            }

            return View("~/Views/Editor/EditReservedPath.cshtml", model);
        }

        /// <summary>
        /// Edit a reserved path.
        /// </summary>
        /// <param name="Id"></param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public async Task<IActionResult> EditReservedPath(Guid Id)
        {
            ViewData["Title"] = "Edit Reserved Path";

            var paths = await articleLogic.GetReservedPaths();

            var path = paths.FirstOrDefault(f => f.Id == Id);

            if (path == null)
            {
                return NotFound();
            }

            return View(path);
        }

        /// <summary>
        /// Edit an existing reserved path.
        /// </summary>
        /// <param name="model"></param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        [HttpPost]
        public async Task<IActionResult> EditReservedPath(ReservedPath model)
        {
            ViewData["Title"] = "Edit Reserved Path";

            if (ModelState.IsValid)
            {
                try
                {
                    await articleLogic.AddOrUpdateReservedPath(model);
                    return RedirectToAction("ReservedPaths");
                }
                catch (Exception e)
                {
                    ModelState.AddModelError("Path", e.Message);
                }
            }

            return View(model);
        }

        /// <summary>
        /// Removes a reerved path.
        /// </summary>
        /// <param name="Id"></param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public async Task<IActionResult> RemoveReservedPath(Guid Id)
        {
            try
            {
                await articleLogic.RemoveReservedPath(Id);
                return RedirectToAction("ReservedPaths");
            }
            catch (Exception e)
            {
                ModelState.AddModelError("Path", e.Message);
            }

            return RedirectToAction("ReservedPaths");
        }

        /// <summary>
        /// Editor page.
        /// </summary>
        /// <param name="Id">Article number (int).</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public async Task<IActionResult> CcmsContent(int Id)
        {
            var article = await articleLogic.Get(Id, null);

            return View(article);
        }

        /// <summary>
        ///     Gets an article to edit by ID for the HTML (WYSIWYG) Editor.
        /// </summary>
        /// <param name="id">Article ID.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        [Authorize(Roles = "Administrators, Editors, Authors, Team Members")]
        public async Task<IActionResult> Edit(int id)
        {
            try
            {
                // Web browser may ask for favicon.ico, so if the ID is not a number, just skip the response.
                ViewData["BlobEndpointUrl"] = options.Value.SiteSettings.BlobPublicUrl;

                // Get an article, or a template based on the controller name.
                var model = await articleLogic.Get(id, null);
                ViewData["LastPubDateTime"] = null;

                ViewData["PageTitle"] = model.Title;
                ViewData["Published"] = null;

                // Override defaults
                model.EditModeOn = true;
                model.Published = null;

                // Authors cannot edit published articles
                if (model.Published.HasValue && User.IsInRole("Authors"))
                {
                    return Unauthorized();
                }

                return View(new HtmlEditorViewModel(model, await dbContext.ArticleCatalog.FirstOrDefaultAsync(f => f.ArticleNumber == id)));
            }
            catch (Exception e)
            {
                logger.LogError(e.Message, e);
                throw;
            }
        }

        /// <summary>
        /// Edit web page code with Monaco editor.
        /// </summary>
        /// <param name="id">Article Number (not ID).</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        [Authorize(Roles = "Administrators, Editors")]
        public async Task<IActionResult> EditCode(int id)
        {
            var article = await articleLogic.Get(id, null);
            if (article == null)
            {
                return NotFound();
            }

            ViewData["Version"] = article.VersionNumber;

            ViewData["PageTitle"] = article.Title;
            ViewData["Published"] = null;
            ViewData["LastPubDateTime"] = null;

            var catalogEntry = await dbContext.ArticleCatalog.FirstOrDefaultAsync(f => f.ArticleNumber == id);

            return View(new EditCodePostModel
            {
                Id = article.Id,
                ArticleNumber = article.ArticleNumber,
                VersionNumber = article.VersionNumber,
                Title = article.Title,
                Published = null,
                ArticlePermissions = catalogEntry.ArticlePermissions,
                EditorTitle = article.Title,
                UrlPath = article.UrlPath,
                BannerImage = article.BannerImage,
                Updated = article.Updated,
                EditorFields = new[]
                {
                    new EditorField
                    {
                        FieldId = "HeadJavaScript",
                        FieldName = "Head Block",
                        EditorMode = EditorMode.Html,
                        IconUrl = "/images/seti-ui/icons/html.svg",
                        ToolTip = "Content to appear at the bottom of the <head> tag."
                    },
                    new EditorField
                    {
                        FieldId = "Content",
                        FieldName = "Html Content",
                        EditorMode = EditorMode.Html,
                        IconUrl = "~/images/seti-ui/icons/html.svg",
                        ToolTip = "Content to appear in the <body>."
                    },
                    new EditorField
                    {
                        FieldId = "FooterJavaScript",
                        FieldName = "Footer Block",
                        EditorMode = EditorMode.Html,
                        IconUrl = "~/images/seti-ui/icons/html.svg",
                        ToolTip = "Content to appear at the bottom of the <body> tag."
                    }
                },
                HeadJavaScript = article.HeadJavaScript,
                FooterJavaScript = article.FooterJavaScript,
                Content = article.Content,
                EditingField = "HeadJavaScript",
                CustomButtons = new[] { "Preview", "Html", "Export", "Import" }
            });
        }

        /// <summary>
        ///     Saves the code and html of the page.
        /// </summary>
        /// <param name="model">Edit code post model.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        /// <remarks>
        ///     This method saves page code to the database.  <see cref="EditCodePostModel.Content" /> is validated using method
        ///     <see cref="BaseController.BaseValidateHtml" />.
        ///     HTML formatting errors that could not be automatically fixed are logged with
        ///     <see cref="ControllerBase.ModelState" /> and
        ///     the code is not saved in the database.
        /// </remarks>
        [HttpPost]
        [Authorize(Roles = "Administrators, Editors, Authors, Team Members")]
        public async Task<IActionResult> EditCode(EditCodePostModel model)
        {
            var saveError = new StringBuilder();

            if (string.IsNullOrEmpty(model.Title))
            {
                throw new Exception("Title cannot be null or empty.");
            }

            if (model == null)
            {
                return NotFound();
            }

            // Next pull the original. This is a view model, not tracked by DbContext.
            var article = await articleLogic.Get(model.ArticleNumber, null);
            var entry = await dbContext.ArticleCatalog.FirstOrDefaultAsync(f => f.ArticleNumber == model.ArticleNumber);

            if (article == null)
            {
                return NotFound();
            }

            var jsonModel = new SaveCodeResultJsonModel();

            if (ModelState.IsValid)
            {
                try
                {
                    var result = await articleLogic.Save(
                        new ArticleViewModel()
                        {
                            Id = model.Id,
                            ArticleNumber = article.ArticleNumber,
                            BannerImage = article.BannerImage,
                            Content = model.Content,
                            Title = model.Title,
                            Published = model.Published,
                            Expires = article.Expires,
                            FooterJavaScript = model.FooterJavaScript,
                            HeadJavaScript = model.HeadJavaScript,
                            StatusCode = (StatusCodeEnum)article.StatusCode,
                            UrlPath = article.UrlPath,
                            VersionNumber = article.VersionNumber,
                            Updated = model.Updated.Value,
                        }, await GetUserEmail());

                    jsonModel.Model = new EditCodePostModel()
                    {
                        Id = result.Model.Id,
                        ArticleNumber = result.Model.ArticleNumber,
                        VersionNumber = result.Model.VersionNumber,
                        BannerImage = result.Model.BannerImage,
                        Content = result.Model.Content,
                        EditingField = model.EditingField,
                        CustomButtons = model.CustomButtons,
                        EditorMode = model.EditorMode,
                        EditorFields = model.EditorFields,
                        EditorTitle = model.EditorTitle,
                        EditorType = model.EditorType,
                        FooterJavaScript = result.Model.FooterJavaScript,
                        HeadJavaScript = result.Model.HeadJavaScript,
                        UrlPath = result.Model.UrlPath,
                        Published = result.Model.Published,
                        Title = result.Model.Title,
                        Updated = result.Model.Updated,
                        ArticlePermissions = entry.ArticlePermissions
                    };

                    jsonModel.ArmOperation = result.ArmOperation;
                }
                catch (Exception e)
                {
                    ViewData["Version"] = article.VersionNumber;
                    var provider = new EmptyModelMetadataProvider();
                    ModelState.AddModelError("Save", e, provider.GetMetadataForType(typeof(string)));
                    logger.LogError(e.Message, e);
                }

                jsonModel.ErrorCount = ModelState.ErrorCount;
                jsonModel.IsValid = ModelState.IsValid;

                jsonModel.Errors.AddRange(ModelState.Values
                    .Where(w => w.ValidationState == ModelValidationState.Invalid)
                    .ToList());
                jsonModel.ValidationState = ModelState.ValidationState;

                ViewData["Version"] = jsonModel.Model.VersionNumber;

                return Json(jsonModel);
            }

            saveError.AppendLine("Error(s):");
            saveError.AppendLine("<ul>");

            var errors = ModelState.Values.Where(w => w.ValidationState == ModelValidationState.Invalid).ToList();

            foreach (var error in errors)
            {
                foreach (var e in error.Errors)
                {
                    saveError.AppendLine("<li>" + e.ErrorMessage + "</li>");
                }
            }

            saveError.AppendLine("</ul>");

            return StatusCode(StatusCodes.Status500InternalServerError, saveError.ToString());
        }

        /// <summary>
        /// Performs a query to see what pages will have changes.
        /// </summary>
        /// <param name="model">Post view model.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        [HttpPost]
        public async Task<IActionResult> SearchAndReplaceQuery(SearchAndReplaceViewModel model)
        {
            if (model.ArticleNumber.HasValue)
            {
                var articleCount = await dbContext.Articles.Where(c => c.ArticleNumber == model.ArticleNumber && c.Content.Contains(model.FindValue)).CountAsync();

                ViewData["SearchAndReplacePrequery"] = $"{articleCount} versions will be modified.";
            }
            else
            {
                var articleCount = await dbContext.Articles.Where(c => c.Published != null && c.Content.Contains(model.FindValue)).CountAsync();

                ViewData["SearchAndReplacePrequery"] = $"{articleCount} published articles will be modified.";
            }

            return View(model);
        }

        /// <summary>
        /// Exports a page as a file.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        [Authorize(Roles = "Administrators, Editors, Authors, Team Members")]
        public async Task<IActionResult> ExportPage(Guid? id)
        {
            ArticleViewModel article;
            var userId = await GetUserId();
            if (id.HasValue)
            {
                article = await articleLogic.Get(id.Value, EnumControllerName.Edit, userId);
            }
            else
            {
                // Get the user's ID for logging.
                article = await articleLogic.Create("Blank Page", userId);
            }

            var html = await articleLogic.ExportArticle(article, blobPublicAbsoluteUrl, viewRenderService);

            var exportName = $"pageid-{article.ArticleNumber}-version-{article.VersionNumber}.html";

            var bytes = Encoding.UTF8.GetBytes(html);

            return File(bytes, "application/octet-stream", exportName);
        }

        /// <summary>
        /// Pre-load the website (useful if CDN configured).
        /// </summary>
        /// <returns>IAction result.</returns>
        [HttpGet]
        [Authorize(Roles = "Administrators")]
        public IActionResult Preload()
        {
            return View(new PreloadViewModel());
        }

        #region Data Services

        /// <summary>
        /// Check to see if a page title is already taken.
        /// </summary>
        /// <param name="articleNumber"></param>
        /// <param name="title"></param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        [HttpGet]
        [HttpPost]
        public async Task<IActionResult> CheckTitle(int articleNumber, string title)
        {
            var result = await articleLogic.ValidateTitle(title, articleNumber);

            if (result)
            {
                return Json(true);
            }

            return Json($"Title '{title}' is already taken.");
        }

        /// <summary>
        /// Gets a list of articles (web pages).
        /// </summary>
        /// <param name="term">search text value (optional).</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        [HttpGet]
        public async Task<IActionResult> GetArticleList(string term = "")
        {
            var query = dbContext.ArticleCatalog.Select(s => new ArticleListItem()
            {
                ArticleNumber = s.ArticleNumber,
                Title = s.Title,
                IsDefault = s.UrlPath == "root",
                LastPublished = s.Published,
                UrlPath = HttpUtility.UrlEncode(s.UrlPath).Replace("%2f", "/"),
                Status = s.Status,
                Updated = s.Updated
            }).OrderBy(o => o.Title);

            var data = new List<ArticleListItem>();
            if (string.IsNullOrEmpty(term))
            {
                data.AddRange(await query.Take(10).ToListAsync());
            }
            else
            {
                term = term.TrimStart('/').Trim().ToLower();
                data.AddRange(await query.Where(w => w.Title.ToLower().Contains(term)).Take(10).ToListAsync());
            }

            return Json(data);
        }

        /// <summary>
        /// Gets a list of articles (pages) on this website.
        /// </summary>
        /// <param name="text"></param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        /// <remarks>Returns published and non-published links.</remarks>
        public async Task<IActionResult> List_Articles(string text)
        {
            IQueryable<Article> query = dbContext.Articles
            .OrderBy(o => o.Title)
            .Where(w => w.StatusCode == (int)StatusCodeEnum.Active || w.StatusCode == (int)StatusCodeEnum.Inactive);

            if (!string.IsNullOrEmpty(text))
            {
                query = query.Where(x => x.Title.ToLower().Contains(text.ToLower()));
            }

            var model = await query.Select(s => new
            {
                s.Title,
                s.UrlPath
            }).Distinct().Take(10).ToListAsync();

            return Json(model);
        }

        /// <summary>
        /// Sends an article (or page) to trash bin.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        [HttpGet]
        public async Task<IActionResult> TrashArticle(int Id)
        {
            await articleLogic.TrashArticle(Id);
            return RedirectToAction("Index", "Editor");
        }

        /// <summary>
        ///     Gets a role list, and allows for filtering.
        /// </summary>
        /// <param name="text"></param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        [HttpGet]
        public async Task<IActionResult> Get_RoleList(string text)
        {
            var query = dbContext.Roles.Select(s => new RoleItemViewModel
            {
                Id = s.Id,
                RoleName = s.Name,
                RoleNormalizedName = s.NormalizedName
            });

            if (!string.IsNullOrEmpty(text))
            {
                query = query.Where(w => w.RoleName.StartsWith(text));
            }

            return Json(await query.OrderBy(r => r.RoleName).ToListAsync());
        }

        #region REDIRECT MANAGEMENT

        /// <summary>
        /// Redirect manager page.
        /// </summary>
        /// <param name="sortOrder"></param>
        /// <param name="currentSort"></param>
        /// <param name="pageNo"></param>
        /// <param name="pageSize"></param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        [Authorize(Roles = "Administrators, Editors")]
        public async Task<IActionResult> Redirects(string sortOrder, string currentSort, int pageNo = 0, int pageSize = 10)
        {
            ViewData["sortOrder"] = sortOrder;
            ViewData["currentSort"] = currentSort;
            ViewData["pageNo"] = pageNo;
            ViewData["pageSize"] = pageSize;

            var query = articleLogic.GetArticleRedirects();

            ViewData["RowCount"] = await query.CountAsync();

            if (sortOrder == "desc")
            {
                if (!string.IsNullOrEmpty(currentSort))
                {
                    switch (currentSort)
                    {
                        case "FromUrl":
                            query = query.OrderByDescending(o => o.FromUrl);
                            break;
                        case "Title":
                            query = query.OrderByDescending(o => o.Id);
                            break;
                        case "ToUrl":
                            query = query.OrderByDescending(o => o.ToUrl);
                            break;
                    }
                }
            }
            else
            {
                if (!string.IsNullOrEmpty(currentSort))
                {
                    switch (currentSort)
                    {
                        case "FromUrl":
                            query = query.OrderBy(o => o.FromUrl);
                            break;
                        case "Id":
                            query = query.OrderBy(o => o.Id);
                            break;
                        case "ToUrl":
                            query = query.OrderBy(o => o.ToUrl);
                            break;
                    }
                }
            }

            var model = await query.Skip(pageNo * pageSize).Take(pageSize).ToListAsync();

            return View(model);
        }

        /// <summary>
        /// Sends an article (or page) to trash bin.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        [HttpGet]
        public async Task<IActionResult> RedirectDelete(Guid Id)
        {
            var article = await dbContext.Articles.FirstOrDefaultAsync(f => f.Id == Id);

            await articleLogic.TrashArticle(article.ArticleNumber);

            return RedirectToAction("Redirects");
        }

        /// <summary>
        /// Updates a redirect.
        /// </summary>
        /// <param name="Id"></param>
        /// <param name="FromUrl"></param>
        /// <param name="ToUrl"></param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        [Authorize(Roles = "Administrators, Editors")]
        public async Task<IActionResult> RedirectEdit([FromForm] Guid Id, string FromUrl, string ToUrl)
        {
            var redirect = await dbContext.Articles.FirstOrDefaultAsync(f => f.Id == Id && f.StatusCode == (int)StatusCodeEnum.Redirect);
            if (redirect == null)
            {
                return NotFound();
            }

            redirect.UrlPath = FromUrl;
            redirect.Content = ToUrl;

            await dbContext.SaveChangesAsync();

            return RedirectToAction("Redirects");
        }

        #endregion

        #endregion

        /// <summary>
        ///     Disposes of resources for this controller.
        /// </summary>
        /// <param name="disposing">Dispose or not.</param>
        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
        }
    }
}

﻿// <copyright file="EditorController.cs" company="Moonrise Software, LLC">
// Copyright (c) Moonrise Software, LLC. All rights reserved.
// Licensed under the GNU Public License, Version 3.0 (https://www.gnu.org/licenses/gpl-3.0.html)
// See https://github.com/MoonriseSoftwareCalifornia/CosmosCMS
// for more information concerning the license and the contributors participating to this project.
// </copyright>

namespace Sky.Cms.Controllers
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using System.Web;
    using Cosmos.BlobService;
    using Cosmos.Common.Data;
    using Cosmos.Common.Data.Logic;
    using Cosmos.Common.Models;
    using Cosmos.Common.Services;
    using Sky.Editor.Controllers;
    using Sky.Editor.Data;
    using Sky.Editor.Data.Logic;
    using Sky.Editor.Models;
    using Sky.Editor.Models.GrapesJs;
    using Cosmos.Editor.Services;
    using HtmlAgilityPack;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Identity;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.AspNetCore.Mvc.ModelBinding;
    using Microsoft.AspNetCore.SignalR;
    using Microsoft.Azure.Cosmos.Serialization.HybridRow;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.Logging;
    using SendGrid.Helpers.Errors.Model;
    using Sky.Cms.Hubs;
    using Sky.Cms.Models;
    using Sky.Cms.Services;

    /// <summary>
    /// Editor controller.
    /// </summary>
    // [ResponseCache(NoStore = true)]
    [Authorize(Roles = "Reviewers, Administrators, Editors, Authors")]
    [ResponseCache(Location = ResponseCacheLocation.None, NoStore = true)]
    public class EditorController : BaseController
    {
        private readonly ArticleEditLogic articleLogic;
        private readonly ApplicationDbContext dbContext;
        private readonly ILogger<EditorController> logger;
        private readonly IEditorSettings options;
        private readonly RoleManager<IdentityRole> roleManager;
        private readonly UserManager<IdentityUser> userManager;
        private readonly Uri blobPublicAbsoluteUrl;
        private readonly IViewRenderService viewRenderService;
        private readonly StorageContext storageContext;
        private readonly IWebHostEnvironment webHost;
        private readonly IHubContext<LiveEditorHub> hub;

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
        /// <param name="storageContext">Storage context.</param>
        /// <param name="hub">Editor SignalR hub.</param>
        /// <param name="webHost">Host environment.</param>
        public EditorController(
            ILogger<EditorController> logger,
            ApplicationDbContext dbContext,
            UserManager<IdentityUser> userManager,
            RoleManager<IdentityRole> roleManager,
            ArticleEditLogic articleLogic,
            IEditorSettings options,
            IViewRenderService viewRenderService,
            StorageContext storageContext,
            IHubContext<LiveEditorHub> hub,
            IWebHostEnvironment webHost)
            : base(dbContext, userManager)
        {
            this.logger = logger;
            this.dbContext = dbContext;
            this.options = options;
            this.roleManager = roleManager;
            this.userManager = userManager;
            this.articleLogic = articleLogic;
            this.storageContext = storageContext;
            this.hub = hub;
            this.webHost = webHost;
            var htmlUtilities = new HtmlUtilities();

            if (htmlUtilities.IsAbsoluteUri(options.BlobPublicUrl))
            {
                blobPublicAbsoluteUrl = new Uri(options.BlobPublicUrl);
            }
            else
            {
                blobPublicAbsoluteUrl = new Uri($"{options.PublisherUrl.TrimEnd('/')}/{options.BlobPublicUrl.TrimStart('/')}");
            }

            this.viewRenderService = viewRenderService;

            // Ensure the required roles exist.
            SetupNewAdministrator.Ensure_Roles_Exists(roleManager).Wait();
        }

        /// <summary>
        /// Catalog of web pages on this website.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public async Task<IActionResult> Index()
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            ViewData["ShowFirstPageBtn"] = false; // Default unless changed below.

            if ((await dbContext.Articles.CountAsync()) == 0)
            {
                var template = await dbContext.Templates.Where(w => w.Title.ToLower().Contains("home page")).FirstOrDefaultAsync();

                if (template == null)
                {
                    ViewData["ShowFirstPageBtn"] = true;
                }
                else
                {
                    return View(viewName: "__NewHomePage", model:
                        new CreatePageViewModel()
                        {
                            TemplateId = template.Id,
                            Title = string.Empty,
                            ArticleNumber = 1,
                            Id = Guid.NewGuid()
                        });
                }
            }

            ViewData["HomePageArticleNumber"] = await dbContext.Pages.Where(f => f.UrlPath == "root").Select(s => s.ArticleNumber).FirstOrDefaultAsync();

            ViewData["ShowNotFoundBtn"] = !await dbContext.ArticleCatalog.Where(w => w.UrlPath == "not_found").CosmosAnyAsync();

            return View();
        }

        /// <summary>
        /// Loads the designer GUI.
        /// </summary>
        /// <param name="id">Article number.</param>
        /// <returns>View.</returns>
        public async Task<IActionResult> Designer(int id)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // Loads GrapeJS.
            ViewData["IsDesigner"] = true;

            var article = await GetArticleForEdit(id);

            if (article == null)
            {
                return NotFound();
            }

            var config = new DesignerConfig(await dbContext.Layouts.FirstOrDefaultAsync(f => f.IsDefault), article.ArticleNumber.ToString(), article.Title);
            var assets = await FileManagerController.GetImageAssetArray(storageContext, $"/pub/articles/{id}", string.Empty);
            if (assets != null)
            {
                config.ImageAssets.AddRange(assets);
            }

            ViewData["DesignerConfig"] = config;
            ViewData["Version"] = article.VersionNumber;

            ViewData["PageTitle"] = article.Title;
            ViewData["Published"] = null;
            ViewData["LastPubDateTime"] = await articleLogic.GetLastPublishedDate(id);

            var catalogEntry = await articleLogic.GetCatalogEntry(article);

            var designerUtils = new DesignerUtilities();
            var data = designerUtils.ExtractDesignerData(article.Content);

            return View(new ArticleDesignerDataViewModel
            {
                Id = article.Id,
                ArticleNumber = article.ArticleNumber,
                VersionNumber = article.VersionNumber,
                Title = article.Title,
                Published = null,
                ArticlePermissions = catalogEntry.ArticlePermissions,
                UrlPath = article.UrlPath,
                BannerImage = article.BannerImage,
                Updated = article.Updated,
                HtmlContent = data.HtmlContent,
                CssContent = data.CssContent,
            });
        }

        /// <summary>
        /// Save designer data.
        /// </summary>
        /// <param name="model">Designer post model.</param>
        /// <returns>IActionResult.</returns>
        [HttpPost]
        public async Task<IActionResult> Designer(ArticleDesignerDataViewModel model)
        {
            if (model == null)
            {
                return Json(new { success = false, message = "No data sent." });
            }

            model.HtmlContent = CryptoJsDecryption.Decrypt(model.HtmlContent);
            model.CssContent = CryptoJsDecryption.Decrypt(model.CssContent);

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // Check for nested editable regions.
            if (!NestedEditableRegionValidation.Validate(model.HtmlContent))
            {
                return BadRequest("Cannot have nested editable regions.");
            }

            // Next pull the original. This is a view model, not tracked by DbContext.
            var article = await articleLogic.GetArticleByArticleNumber(model.ArticleNumber, null);

            if (article == null)
            {
                return NotFound();
            }

            var designerUtils = new DesignerUtilities();
            var html = designerUtils.AssembleDesignerOutput(new DesignerDataViewModel() { CssContent = model.CssContent, HtmlContent = model.HtmlContent, Title = model.Title, Id = model.Id });

            try
            {
                var result = await articleLogic.SaveArticle(
                                    new ArticleViewModel()
                                    {
                                        Id = article.Id,
                                        ArticleNumber = article.ArticleNumber,
                                        BannerImage = article.BannerImage,
                                        Content = html,
                                        Title = model.Title,
                                        Expires = article.Expires,
                                        FooterJavaScript = article.FooterJavaScript,
                                        HeadJavaScript = article.HeadJavaScript,
                                        StatusCode = article.StatusCode,
                                        UrlPath = article.UrlPath,
                                        VersionNumber = article.VersionNumber,
                                        Updated = DateTimeOffset.UtcNow
                                    }, Guid.Parse(await GetUserId()));
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }

            return Json(new { success = true });
        }

        /// <summary>
        /// Visual designer based on GrapeJS.
        /// </summary>
        /// <param name="id">Article number.</param>
        /// <returns>IActionResult.</returns>
        [HttpGet]
        public async Task<IActionResult> GetDesignerData(int id)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var article = await articleLogic.GetArticleByArticleNumber(id, null);
            if (article == null)
            {
                return NotFound();
            }

            var htmlContent = articleLogic.Ensure_ContentEditable_IsMarked(article.Content);

            return Json(new project(htmlContent));
        }

        /// <summary>
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
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

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
        /// Gets the article trash list.
        /// </summary>
        /// <returns>Trask list.</returns>
        [Authorize(Roles = "Administrators, Editors, Authors")]
        public async Task<IActionResult> GetTrashList()
        {
            if (dbContext.Database.IsCosmos())
            {
                var query = "SELECT c.ArticleNumber, c.Title, c.UrlPath, MAX(c.Published) as Published, MAX(c.Updated) as Updated FROM Articles c WHERE c.StatusCode = 2 GROUP BY c.ArticleNumber, c.Title, c.UrlPath";
                var client = dbContext.Database.GetCosmosClient();
                var queryService = new CosmosDbService(client, dbContext.Database.GetCosmosDatabaseId(), "Articles");

                return Json(await queryService.QueryWithGroupByAsync(query));
            }

            var data = await dbContext.Articles
                .Where(w => w.StatusCode == (int)StatusCodeEnum.Deleted)
                .GroupBy(g => new { g.ArticleNumber, g.Title, g.UrlPath })
                .Select(s => new
                {
                    ArticleNumber = s.Key.ArticleNumber,
                    Title = s.Key.Title,
                    UrlPath = s.Key.UrlPath,
                    Published = s.Max(m => m.Published),
                    Updated = s.Max(m => m.Updated)
                }).ToListAsync();

            return Json(data);
        }

        /// <summary>
        /// Open trash.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        [Authorize(Roles = "Administrators, Editors, Authors")]
        public IActionResult Trash()
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            return View();
        }

        /// <summary>
        /// Compare two versions.
        /// </summary>
        /// <param name="leftId">Article ID of the left version.</param>
        /// <param name="rightId">Article ID of the right version.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public async Task<IActionResult> Compare(Guid leftId, Guid rightId)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var left = await articleLogic.GetArticleById(leftId, EnumControllerName.Edit, Guid.Parse(await GetUserId()));
            var right = await articleLogic.GetArticleById(rightId, EnumControllerName.Edit, Guid.Parse(await GetUserId()));
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
        /// <param name="id">Template ID.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        [Authorize(Roles = "Administrators, Editors, Authors, Team Members")]
        public async Task<IActionResult> GetTemplateInfo(Guid? id)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            if (id == null)
            {
                return Json(string.Empty);
            }

            var model = await dbContext.Templates.FirstOrDefaultAsync(f => f.Id == id.Value);

            return Json(model);
        }

        /// <summary>
        ///     Creates a <see cref="CreatePageViewModel" /> used to create a new article.
        /// </summary>
        /// <param name="title">Name of new page if known.</param>
        /// <param name="sortOrder">Current sort order.</param>
        /// <param name="currentSort">Field being sorted on.</param>
        /// <param name="pageNo">Page number to retrieve.</param>
        /// <param name="pageSize">Number of records in each page.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        [Authorize(Roles = "Administrators, Editors, Authors, Team Members")]
        public async Task<IActionResult> Create(string title = "", string sortOrder = "asc", string currentSort = "Title", int pageNo = 0, int pageSize = 20)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            if ((await dbContext.Articles.CountAsync()) == 0)
            {
                var template = await dbContext.Templates.Where(w => w.Title.ToLower().Contains("home page")).FirstOrDefaultAsync();

                if (template == null)
                {
                    ViewData["ShowFirstPageBtn"] = true;
                }
                else
                {
                    return View(viewName: "__NewHomePage", model:
                        new CreatePageViewModel()
                        {
                            TemplateId = template.Id,
                            Title = string.Empty,
                            ArticleNumber = 1,
                            Id = Guid.NewGuid()
                        });
                }
            }

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
                Title = title.Contains("{new page name}", StringComparison.CurrentCultureIgnoreCase) ? string.Empty : title
            });
        }

        /// <summary>
        ///     Uses <see cref="ArticleEditLogic.CreateArticle" /> to create an <see cref="ArticleViewModel" /> that is
        ///     saved to
        ///     the database with <see cref="ArticleEditLogic.SaveArticle" /> ready for editing.
        /// </summary>
        /// <param name="model">Create page view model.</param>
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

                var article = await articleLogic.CreateArticle(model.Title, Guid.Parse(await GetUserId()), model.TemplateId);

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
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

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
        /// <param name="id">Page ID.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public async Task<IActionResult> Clone(int id)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var lastVersion = await dbContext.Articles.Where(a => a.ArticleNumber == id).MaxAsync(m => m.VersionNumber);

            var articleViewModel = await articleLogic.GetArticleByArticleNumber(id, lastVersion);

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
        /// <param name="model">Dublice page view model.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Administrators, Editors, Authors")]
        public async Task<IActionResult> Clone(DuplicateViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            string title = string.Empty;

            if (string.IsNullOrEmpty(model.ParentPageTitle))
            {
                title = model.Title;
            }
            else
            {
                title = $"{model.ParentPageTitle.Trim('/')}/{model.Title.Trim('/')} ";
            }

            if (await dbContext.Articles.Where(a => a.Title.ToLower() == title.ToLower() && a.StatusCode != (int)StatusCodeEnum.Deleted).CosmosAnyAsync())
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

            var userId = Guid.Parse(await GetUserId());

            var articleViewModel = await articleLogic.GetArticleById(model.Id, EnumControllerName.Edit, userId);

            if (ModelState.IsValid)
            {
                articleViewModel.ArticleNumber = 0;
                articleViewModel.Id = Guid.NewGuid();
                articleViewModel.Published = model.Published;
                articleViewModel.Title = title;

                try
                {
                    var clone = await articleLogic.CreateArticle(articleViewModel.Title, userId);
                    clone.StatusCode = articleViewModel.StatusCode;
                    clone.CacheDuration = articleViewModel.CacheDuration;
                    clone.Content = articleViewModel.Content;
                    clone.FooterJavaScript = articleViewModel.FooterJavaScript;
                    clone.HeadJavaScript = articleViewModel.HeadJavaScript;
                    clone.LanguageCode = articleViewModel.LanguageCode;

                    var result = await articleLogic.SaveArticle(clone, userId);

                    // Otherwise, open in the Monaco code editor
                    return RedirectToAction("Versions", "Editor", new { id = result.Model.ArticleNumber });
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
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

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

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = await userManager.GetUserAsync(User);
            await articleLogic.CreateHomePage(model);

            return RedirectToAction("Index");
        }

        /// <summary>
        /// Creates a new home page.
        /// </summary>
        /// <param name="model">Model used to create a the first home page.</param>
        /// <returns>Returns <see cref="IActionResult"/>.</returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateInitialHomePage(CreatePageViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            if (model == null)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                if (await dbContext.Articles.CosmosAnyAsync())
                {
                    ModelState.AddModelError("Title", "This can only be used to create a website's first home page.");
                }

                model.Title = model.Title.TrimStart('/');

                var validTitle = await articleLogic.ValidateTitle(model.Title, null);

                if (!validTitle)
                {
                    ModelState.AddModelError("Title", $"Title: {model.Title} conflicts with another article title or reserved word.");
                    return View(viewName: "__NewHomePage", model: model);
                }

                var template = await dbContext.Templates.FirstOrDefaultAsync(f => f.Title.ToLower() == "home page");
                var article = await articleLogic.CreateArticle(model.Title, Guid.Parse(await GetUserId()), template.Id);
                
                article.Published = DateTimeOffset.UtcNow;
                article.StatusCode = (int)StatusCodeEnum.Active;
                article.Content = template.Content;
                article.UrlPath = "root";

                await articleLogic.SaveArticle(article, Guid.Parse(await GetUserId()));

                return Redirect("/");
            }

            return View(viewName: "__NewHomePage", model: model);
        }

        /// <summary>
        /// Restore an article from trash.
        /// </summary>
        /// <param name="id">Article ID to recover from trash.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        [Authorize(Roles = "Administrators, Editors, Authors")]
        public async Task<IActionResult> Restore(int id)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            await articleLogic.RestoreArticle(id, await GetUserId());

            return Ok();
        }

        /// <summary>
        ///     Publish website dialog.
        /// </summary>
        /// <returns>Returns a view.</returns>
        [Authorize(Roles = "Administrators, Editors")]
        public IActionResult Publish()
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            return View();
        }

        /// <summary>
        /// Publishes an article.
        /// </summary>
        /// <param name="articleId">Article ID.</param>
        /// <param name="datetime">Date and time to publish.</param>
        /// <param name="editorUrl">Editor URL.</param>
        /// <returns>IActionResult.</returns>
        public async Task<IActionResult> PublishPage(Guid articleId, DateTimeOffset? datetime, string editorUrl)
        {
            await articleLogic.PublishArticle(articleId, datetime);

            return Redirect(editorUrl);
        }

        /// <summary>
        /// Un-publishes an article.
        /// </summary>
        /// <param name="id">Article ID.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        [Authorize(Roles = "Administrators, Editors")]
        public async Task<IActionResult> UnpublishPage(int id)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            await articleLogic.UnpublishArticle(id);

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
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            ViewData["sortOrder"] = sortOrder;
            ViewData["currentSort"] = currentSort;
            ViewData["pageNo"] = pageNo;
            ViewData["pageSize"] = pageSize;
            ViewData["showingRoles"] = forRoles;

            var article = await dbContext.Articles.FindAsync(id);

            var catalogEntry = await articleLogic.GetCatalogEntry(article);

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
                    }).AsQueryable();
            }
            else
            {
                query = userManager.Users.Select(
                    s => new ArticlePermisionItem
                    {
                        IdentityObjectId = s.Id,
                        Name = s.Email,
                    }).AsQueryable();
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
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                var article = await dbContext.Articles.Where(w => w.ArticleNumber == id).OrderByDescending(o => o.VersionNumber).LastOrDefaultAsync();
                var entry = await articleLogic.GetCatalogEntry(article);

                if (entry.ArticlePermissions == null)
                {
                    entry.ArticlePermissions = new List<ArticlePermission>();
                }
                else
                {
                    entry.ArticlePermissions.Clear();
                }

                var roles = await dbContext.Roles.Where(w => identityObjectIds.Contains(w.Id)).ToListAsync();
                var users = await dbContext.Users.Where(w => identityObjectIds.Contains(w.Id)).ToListAsync();

                if (roles.Any())
                {
                    entry.ArticlePermissions.AddRange(roles.Select(s => new ArticlePermission()
                    {
                        IdentityObjectId = s.Id,
                        IsRoleObject = true
                    }).ToArray());
                }

                if (users.Any())
                {
                    entry.ArticlePermissions.AddRange(users.Select(s => new ArticlePermission()
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
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

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
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var paths = await articleLogic.GetReservedPaths();

            ViewData["RowCount"] = paths.Count;

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

            return View(await query.ToListAsync());
        }

        /// <summary>
        /// Creates a new reserved path.
        /// </summary>
        /// <returns>ViewResult.</returns>
        [HttpGet]
        public IActionResult CreateReservedPath()
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            ViewData["Title"] = "Create a Reserved Path";

            return View("~/Views/Editor/EditReservedPath.cshtml", new ReservedPath());
        }

        /// <summary>
        /// Creates a new reserved path.
        /// </summary>
        /// <param name="model">Reserved path model.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        [HttpPost]
        public async Task<IActionResult> CreateReservedPath(ReservedPath model)
        {
            ViewData["Title"] = "Create a Reserved Path";

            if (ModelState.IsValid)
            {
                try
                {
                    await articleLogic.SaveReservedPath(model);
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
        /// <param name="id">Path ID.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public async Task<IActionResult> EditReservedPath(Guid id)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            ViewData["Title"] = "Edit Reserved Path";

            var paths = await articleLogic.GetReservedPaths();

            var path = paths.Find(f => f.Id == id);

            if (path == null)
            {
                return NotFound();
            }

            return View(path);
        }

        /// <summary>
        /// Edit an existing reserved path.
        /// </summary>
        /// <param name="model">Reserved path model.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        [HttpPost]
        public async Task<IActionResult> EditReservedPath(ReservedPath model)
        {
            ViewData["Title"] = "Edit Reserved Path";

            if (ModelState.IsValid)
            {
                try
                {
                    await articleLogic.SaveReservedPath(model);
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
        /// <param name="id">Path ID.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public async Task<IActionResult> RemoveReservedPath(Guid id)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                await articleLogic.DeleteReservedPath(id);
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
        /// <param name="id">Article number (int).</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public async Task<IActionResult> CcmsContent(int id)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var article = await articleLogic.GetArticleByArticleNumber(id, null);

            return View(article);
        }

        /// <summary>
        ///     Gets an article to edit by ID for the HTML (WYSIWYG) Editor.
        /// </summary>
        /// <param name="id">Article number.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        [Authorize(Roles = "Administrators, Editors, Authors, Team Members")]
        public async Task<IActionResult> Edit(int id)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // Web browser may ask for favicon.ico, so if the ID is not a number, just skip the response.
            ViewData["BlobEndpointUrl"] = options.BlobPublicUrl;

            // Get an article, or a template based on the controller name.
            var model = await articleLogic.GetArticleByArticleNumber(id, null);

            ViewData["PageTitle"] = model.Title;
            ViewData["Published"] = null;
            ViewData["LastPubDateTime"] = await articleLogic.GetLastPublishedDate(id);

            // Override defaults
            model.EditModeOn = true;
            model.Published = null;

            // Authors cannot edit published articles
            if (model.Published.HasValue && User.IsInRole("Authors"))
            {
                return Unauthorized();
            }

            var article = await GetArticleForEdit(id);

            var entry = await articleLogic.GetCatalogEntry(article);

            return View(new HtmlEditorViewModel(model, entry));
        }

        /// <summary>
        /// Saves article properties.
        /// </summary>
        /// <param name="model">Live editor post model.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        [HttpPost]
        public async Task<IActionResult> Edit(HtmlEditorPostViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            if (string.IsNullOrEmpty(model.Title))
            {
                throw new ArgumentException("Title cannot be null or empty.");
            }

            if (model == null)
            {
                throw new ArgumentException("SaveEditorContent method, model was null.");
            }

            // Next pull the original. This is a view model, not tracked by DbContext.
            var article = await articleLogic.GetArticleByArticleNumber(model.ArticleNumber, null);
            if (article == null)
            {
                throw new NotFoundException($"CScould not find artile with #: {model.ArticleNumber}.");
            }

            if (!string.IsNullOrWhiteSpace(model.EditorId))
            {
                // Now carry over what's being UPDATED to the original.
                article.Content = UpdateRegionInDocument(model.EditorId, article.Content, CryptoJsDecryption.Decrypt(model.Data));
            }

            // Banner image
            article.BannerImage = model.BannerImage;

            // Make sure we are setting to the orignal updated date/time
            // This is validated to make sure that someone else hasn't already edited this
            // entity
            article.Updated = DateTimeOffset.UtcNow;
            article.Title = model.Title;

            // Save changes back to the database
            var result = await articleLogic.SaveArticle(article, Guid.Parse(await GetUserId()));

            // Notify others of the changes.
            if (!string.IsNullOrWhiteSpace(model.EditorId))
            {
                await hub.Clients.All.SendCoreAsync("UpdateEditors", [model.Id, model.Data]);
            }

            return Json(result);
        }

        /// <summary>
        /// Updates a single region in an editable document.
        /// </summary>
        /// <param name="model">Editor view model.</param>
        /// <returns>Returns OK on success.</returns>
        public async Task<IActionResult> EditSaveRegion(EditorRegionViewModel model)
        {
            var article = await dbContext.Articles.Where(w => w.ArticleNumber == model.ArticleNumber).OrderBy(o => o.VersionNumber).LastOrDefaultAsync();

            var decryptedData = CryptoJsDecryption.Decrypt(model.Data);

            // Now carry over what's being UPDATED to the original.
            var content = UpdateRegionInDocument(model.EditorId, article.Content, decryptedData);

            if (article.Content != content)
            {
                article.Content = content;
                article.Updated = DateTimeOffset.UtcNow;
                await dbContext.SaveChangesAsync();
                await hub.Clients.All.SendCoreAsync("UpdateEditors", [model.EditorId, model.Data]);
            }

            return Ok();
        }

        /// <summary>
        /// Updates the entire body of a web page.
        /// </summary>
        /// <param name="model">Editor view model.</param>
        /// <returns>Returns OK on success.</returns>
        public async Task<IActionResult> EditSaveBody(EditorRegionViewModel model)
        {
            var article = await dbContext.Articles.Where(w => w.ArticleNumber == model.ArticleNumber).OrderBy(o => o.VersionNumber).LastOrDefaultAsync();

            var decryptedData = CryptoJsDecryption.Decrypt(model.Data);

            if (article.Content != decryptedData)
            {
                article.Content = decryptedData;
                article.Updated = DateTimeOffset.UtcNow;
                await dbContext.SaveChangesAsync();
            }

            return Ok();
        }

        /// <summary>
        /// Edit web page code with Monaco editor.
        /// </summary>
        /// <param name="id">Article Number (not ID).</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        [Authorize(Roles = "Administrators, Editors")]
        public async Task<IActionResult> EditCode(int id)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // Get an article, or a template based on the controller name.
            var article = await GetArticleForEdit(id);
            if (article == null)
            {
                return NotFound();
            }

            ViewData["Version"] = article.VersionNumber;

            ViewData["PageTitle"] = article.Title;
            ViewData["Published"] = null;
            ViewData["LastPubDateTime"] = await articleLogic.GetLastPublishedDate(id);

            var catalogEntry = await articleLogic.GetCatalogEntry(article);

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
                HeadJavaScript = article.HeaderJavaScript,
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
            model.Content = CryptoJsDecryption.Decrypt(model.Content);
            model.HeadJavaScript = CryptoJsDecryption.Decrypt(model.HeadJavaScript);
            model.FooterJavaScript = CryptoJsDecryption.Decrypt(model.FooterJavaScript);
            var saveError = new StringBuilder();

            // Validate the model as it comes in.
            if (ModelState.IsValid)
            {
                if (model == null)
                {
                    return NotFound();
                }

                // Check for nested editable regions.
                if (!NestedEditableRegionValidation.Validate(model.Content))
                {
                    ModelState.AddModelError("Content", "Cannot have nested editable regions.");
                }

                // Next pull the original. This is a view model, not tracked by DbContext.
                // var article = await articleLogic.GetArticleByArticleNumber(model.ArticleNumber, null);
                var article = await dbContext.Articles.Where(w => w.ArticleNumber == model.ArticleNumber).OrderBy(o => o.VersionNumber).LastOrDefaultAsync();
                var entry = await articleLogic.GetCatalogEntry(article);

                if (article == null)
                {
                    return NotFound();
                }

                var jsonModel = new SaveCodeResultJsonModel();

                // If still valid, continue processing.
                if (ModelState.IsValid)
                {
                    try
                    {
                        var result = await articleLogic.SaveArticle(
                            new ArticleViewModel()
                            {
                                Id = model.Id,
                                ArticleNumber = article.ArticleNumber,
                                BannerImage = article.BannerImage,
                                Content = model.Content,
                                Title = model.Title,
                                Expires = article.Expires,
                                FooterJavaScript = model.FooterJavaScript,
                                HeadJavaScript = model.HeadJavaScript,
                                StatusCode = (StatusCodeEnum)article.StatusCode,
                                UrlPath = article.UrlPath,
                                VersionNumber = article.VersionNumber,
                                Updated = model.Updated.Value
                            }, Guid.Parse(await GetUserId()));
                    }
                    catch (Exception e)
                    {
                        ViewData["Version"] = article.VersionNumber;
                        var provider = new EmptyModelMetadataProvider();
                        ModelState.AddModelError("Save", e, provider.GetMetadataForType(typeof(string)));
                        logger.LogError(e, e.Message);
                    }

                    jsonModel.ErrorCount = ModelState.ErrorCount;
                    jsonModel.IsValid = ModelState.IsValid;

                    jsonModel.Errors.AddRange(ModelState.Values
                        .Where(w => w.ValidationState == ModelValidationState.Invalid)
                        .ToList());
                    jsonModel.ValidationState = ModelState.ValidationState;

                    return Json(jsonModel);
                }
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
            if (!ModelState.IsValid)
            {
                return View(model);
            }

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
        /// <param name="id">Article version ID.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        [Authorize(Roles = "Administrators, Editors, Authors, Team Members")]
        public async Task<IActionResult> ExportPage(Guid? id)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            ArticleViewModel article;
            var userId = Guid.Parse(await GetUserId());
            if (id.HasValue)
            {
                article = await articleLogic.GetArticleById(id.Value, EnumControllerName.Edit, userId);
            }
            else
            {
                // Get the user's ID for logging.
                article = await articleLogic.CreateArticle("Blank Page", userId);
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
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            return View(new PreloadViewModel());
        }

        /// <summary>
        /// Check to see if a page title is already taken.
        /// </summary>
        /// <param name="articleNumber">Article number.</param>
        /// <param name="title">Article title.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        [HttpGet]
        [HttpPost]
        public async Task<IActionResult> CheckTitle(int articleNumber, string title)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

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
        /// <param name="publishedOnly">Only retrieve published articles.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        [HttpGet]
        public async Task<IActionResult> GetArticleList(string term = "", bool publishedOnly = true)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            if (dbContext.Database.IsCosmos())
            {
                var whereClause = publishedOnly ? $"WHERE c.Published != null AND " : "WHERE ";
                whereClause += $"c.StatusCode = {(int)StatusCodeEnum.Active}";

                if (!string.IsNullOrEmpty(term))
                {
                    whereClause += $" AND LOWER(c.Title) LIKE '%{term.ToLower()}%'";
                }

                var query = $"SELECT c.ArticleNumber, c.Title, c.UrlPath, MAX(c.Published) as Published, MAX(c.Updated) as Updated FROM Articles c {whereClause} GROUP BY c.ArticleNumber, c.Title, c.UrlPath";
                var client = dbContext.Database.GetCosmosClient();
                var queryService = new CosmosDbService(client, dbContext.Database.GetCosmosDatabaseId(), "Articles");

                var data = await queryService.QueryWithGroupByAsync<ArticleListViewItem>(query);

                var model = data.Select(s => new
                {
                    s.ArticleNumber,
                    s.Title,
                    IsDefault = s.UrlPath == "root",
                    LastPublished = s.Published.HasValue ? s.Published.Value.UtcDateTime.ToString("o") : null,
                    UrlPath = HttpUtility.UrlEncode(s.UrlPath).Replace("%2f", "/"),
                    Updated = s.Updated.UtcDateTime.ToString("o")
                }).OrderBy(o => o.Title).ToList();

                return Json(model);
            }
            else
            {
                // LINQ equivalent for the SQL GROUP BY and MAX aggregate
                var query = publishedOnly ? dbContext.Articles
                    .Where(a => a.Published != null && a.StatusCode == (int)StatusCodeEnum.Active) :
                    dbContext.Articles
                    .Where(a => a.StatusCode == (int)StatusCodeEnum.Active);

                if (!string.IsNullOrEmpty(term))
                {
                    query = query.Where(a => a.Title.ToLower().Contains(term.ToLower()));
                }

                var grouped = await query
                .GroupBy(a => new { a.ArticleNumber, a.Title, a.UrlPath })
                .Select(g => new
                {
                    ArticleNumber = g.Key.ArticleNumber,
                    Title = g.Key.Title,
                    UrlPath = g.Key.UrlPath,
                    Published = g.Max(x => x.Published),
                    Updated = g.Max(x => x.Updated)
                })
                .OrderBy(o => o.Title)
                .ToListAsync();

                var model = grouped.Select(s => new
                {
                    s.ArticleNumber,
                    s.Title,
                    IsDefault = s.UrlPath == "root",
                    LastPublished = s.Published.HasValue ? s.Published.Value.UtcDateTime.ToString("o") : null,
                    UrlPath = HttpUtility.UrlEncode(s.UrlPath).Replace("%2f", "/"),
                    Updated = s.Updated
                }).ToList();

                return Json(model);
            }
        }

        /// <summary>
        /// Gets an encryption key.
        /// </summary>
        /// <returns>Key.</returns>
        public async Task<IActionResult> GetEncryptionKey()
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var setting = await dbContext.Settings.Where(w => w.Description == "EncryptionKey").FirstOrDefaultAsync();
            if (setting == null)
            {
                var random = new Random();
                var chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
                var value = new string(Enumerable.Repeat(chars, 16)
                                            .Select(s => s[random.Next(s.Length)])
                                            .ToArray());

                setting = new Setting()
                {
                    Description = "EncryptionKey",
                    Value = value
                };

                dbContext.Settings.Add(setting);
                await dbContext.SaveChangesAsync();
            }

            return Json(setting.Value);
        }

        /// <summary>
        /// Gets a list of published pages.
        /// </summary>
        /// <returns>List of published pages.</returns>
        [HttpGet]
        public async Task<IActionResult> GetPublishedPageList()
        {
            var activeCode = (int)StatusCodeEnum.Active;
            var redirectCode = (int)StatusCodeEnum.Redirect;
            var pages = await dbContext.Pages.Where(w => w.Published.HasValue && (w.StatusCode == activeCode || w.StatusCode == redirectCode)).Select(s =>
            new
            {
                s.Id,
                s.ArticleNumber
            }).ToListAsync();

            return Json(pages);
        }

        /// <summary>
        /// Publish list of web pages to static website.
        /// </summary>
        /// <param name="guids">List of page IDs.</param>
        /// <returns>IActionResult.</returns>
        [HttpPost]
        [Authorize(Roles = "Editors,Administrators")]
        public async Task<IActionResult> PublishStaticPages([FromBody] List<Guid> guids)
        {
            var pages = await dbContext.Pages.Where(w => guids.Contains(w.Id) && w.Published.HasValue).ToListAsync();
            foreach (var page in pages)
            {
                await articleLogic.CreateStaticWebpage(page);
            }

            return Json(new { pages.Count });
        }

        /// <summary>
        /// Publishes a table of contents and new site map file.
        /// </summary>
        /// <param name="path">TOC root path.</param>
        /// <returns>IActionResult.</returns>
        [HttpGet]
        [Authorize(Roles = "Editors,Administrators")]
        public async Task<IActionResult> PublishTOC(string path = "/")
        {
            await articleLogic.CreateStaticTableOfContentsJsonFile(path);
            return Ok();
        }

        /// <summary>
        /// Gets a list of articles (pages) on this website.
        /// </summary>
        /// <param name="text">Search text.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        /// <remarks>Returns published and non-published links.</remarks>
        public async Task<IActionResult> List_Articles(string text)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

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
        /// <param name="id">Article number.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        [HttpGet]
        public async Task<IActionResult> TrashArticle(int id)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            await articleLogic.DeleteArticle(id);
            return Ok();
        }

        /// <summary>
        ///     Gets a role list, and allows for filtering.
        /// </summary>
        /// <param name="text">Filter string.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        [HttpGet]
        public async Task<IActionResult> Get_RoleList(string text)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

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

        /// <summary>
        /// Redirect manager page.
        /// </summary>
        /// <param name="sortOrder">Sort order.</param>
        /// <param name="currentSort">Current sort item.</param>
        /// <param name="pageNo">Page number.</param>
        /// <param name="pageSize">Page size.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        [Authorize(Roles = "Administrators, Editors")]
        public async Task<IActionResult> Redirects(string sortOrder, string currentSort, int pageNo = 0, int pageSize = 10)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

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
        /// <param name="id">Article ID.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        [HttpGet]
        public async Task<IActionResult> RedirectDelete(Guid id)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var article = await dbContext.Articles.FirstOrDefaultAsync(f => f.Id == id);

            await articleLogic.DeleteArticle(article.ArticleNumber);

            return RedirectToAction("Redirects");
        }

        /// <summary>
        /// Updates a redirect.
        /// </summary>
        /// <param name="id">Article ID.</param>
        /// <param name="fromUrl">Redirect from URL.</param>
        /// <param name="toUrl">Redirect to URL.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        [Authorize(Roles = "Administrators, Editors")]
        public async Task<IActionResult> RedirectEdit([FromForm] Guid id, string fromUrl, string toUrl)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var redirect = await dbContext.Articles.FirstOrDefaultAsync(f => f.Id == id && f.StatusCode == (int)StatusCodeEnum.Redirect);
            if (redirect == null)
            {
                return NotFound();
            }

            redirect.UrlPath = fromUrl;
            redirect.Content = toUrl;

            await dbContext.SaveChangesAsync();

            return RedirectToAction("Redirects");
        }

        /// <summary>
        /// Updates the time stamps for all published pages.
        /// </summary>
        /// <returns>IActionResult.</returns>
        [HttpGet]
        [Authorize(Roles = "Administrators, Editors")]
        public async Task<IActionResult> UpdateTimeStamps()
        {
            var pages = await dbContext.Pages.ToListAsync();
            var c = 0;
            foreach (var page in pages)
            {
                c++;
                page.Updated = DateTime.UtcNow;

                if (c >= 20)
                {
                    await dbContext.SaveChangesAsync();
                    c = 0;
                }
            }

            await dbContext.SaveChangesAsync();

            return Json("Ok");
        }

        /// <summary>
        /// Flush the CDN if configured.
        /// </summary>
        /// <returns>IActionResult.</returns>
        [HttpGet]
        [Authorize(Roles = "Administrators, Editors")]
        public async Task<IActionResult> RefreshCdn()
        {
            if (Request.Host.Host.Contains("localhost"))
            {
                return Ok();
            }

            var settings = await Cosmos___SettingsController.GetCdnConfiguration(dbContext);
            var cdnService = new CdnService(settings, logger, HttpContext);
            var result = await cdnService.PurgeCdn(new List<string>() { "/" });
            return Json(result);
        }

        /// <summary>
        ///     Disposes of resources for this controller.
        /// </summary>
        /// <param name="disposing">Dispose or not.</param>
        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
        }

        /// <summary>
        /// Updates the HTML within an editor region no a web page.
        /// </summary>
        /// <param name="editorId">Editor ID on page.</param>
        /// <param name="pageBody">Page body.</param>
        /// <param name="updatedContent">Updated content.</param>
        /// <returns>Revised page body.</returns>
        private string UpdateRegionInDocument(string editorId, string pageBody, string updatedContent)
        {
            // Get the editable regions from the original document.
            var originalHtmlDoc = new HtmlDocument();
            originalHtmlDoc.LoadHtml(pageBody);
            var originalEditableDivs = originalHtmlDoc.DocumentNode.SelectNodes("//*[@data-ccms-ceid]");

            // Find the region we are updating
            var target = originalEditableDivs.FirstOrDefault(w => w.Attributes["data-ccms-ceid"].Value == editorId);
            if (target != null)
            {
                // Update the region now
                target.InnerHtml = updatedContent;
            }

            // Now carry over what's being UPDATED to the original.
            return originalHtmlDoc.DocumentNode.OuterHtml;
        }



        private async Task<Article> GetArticleForEdit(int articleNumber)
        {
            var article = await dbContext.Articles.Where(w => w.ArticleNumber == articleNumber).OrderByDescending(o => o.VersionNumber).FirstOrDefaultAsync();
            if (article == null)
            {
               return null;
            }

            if (article.Published.HasValue)
            {
                return await NewVersion(article);
            }

            return article;
        }

        /// <summary>
        ///  Creates a new layout from an existing layout.
        /// </summary>
        /// <param name="article">Exiting article.</param>
        /// <returns>New layout with an incremented version number.</returns>
        private async Task<Article> NewVersion(Article article)
        {
            var nextVersion = new Article()
            {
                VersionNumber = (await dbContext.Articles.Where(a => a.ArticleNumber == article.ArticleNumber).CountAsync()) + 1,
                Published = null,
                Id = Guid.NewGuid(),
                ArticleNumber = article.ArticleNumber,
                BannerImage = article.BannerImage,
                Content = article.Content,
                FooterJavaScript = article.FooterJavaScript,
                HeaderJavaScript = article.HeaderJavaScript,
                StatusCode = article.StatusCode,
                Title = article.Title,
                UrlPath = article.UrlPath,
                Updated = DateTimeOffset.UtcNow,
                TemplateId = article.TemplateId,
                UserId = article.UserId,
                Expires = article.Expires,
            };

            dbContext.Articles.Add(nextVersion);
            await dbContext.SaveChangesAsync();
            return nextVersion;
        }

    }
}

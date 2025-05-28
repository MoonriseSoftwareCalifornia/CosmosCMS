// <copyright file="TemplatesController.cs" company="Moonrise Software, LLC">
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
    using System.Threading.Tasks;
    using Cosmos.BlobService;
    using Cosmos.Cms.Common.Services.Configurations;
    using Cosmos.Cms.Models;
    using Cosmos.Common.Data;
    using Cosmos.Common.Data.Logic;
    using Cosmos.Common.Models;
    using Cosmos.Editor.Data;
    using Cosmos.Editor.Data.Logic;
    using Cosmos.Editor.Models;
    using Cosmos.Editor.Models.GrapesJs;
    using Cosmos.Editor.Services;
    using HtmlAgilityPack;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Identity;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.Options;

    /// <summary>
    /// Templates controller.
    /// </summary>
    [Authorize(Roles = "Administrators, Editors")]
    [ResponseCache(Location = ResponseCacheLocation.None, NoStore = true)]
    public class TemplatesController : BaseController
    {
        private readonly ArticleEditLogic articleLogic;
        private readonly ApplicationDbContext dbContext;
        private readonly IEditorSettings options;
        private readonly StorageContext storageContext;

        /// <summary>
        /// Initializes a new instance of the <see cref="TemplatesController"/> class.
        /// Constructor.
        /// </summary>
        /// <param name="dbContext">Database context.</param>
        /// <param name="userManager">User manager.</param>
        /// <param name="storageContext">Storage context service.</param>
        /// <param name="articleLogic">Article edit logic.</param>
        /// <param name="options">Cosmos Options.</param>
        public TemplatesController(
            ApplicationDbContext dbContext,
            UserManager<IdentityUser> userManager,
            StorageContext storageContext,
            ArticleEditLogic articleLogic,
            IEditorSettings options)
            : base(dbContext, userManager)
        {
            this.dbContext = dbContext;
            this.articleLogic = articleLogic;
            this.options = options;
            this.storageContext = storageContext;
        }

        /// <summary>
        /// Index view model.
        /// </summary>
        /// <param name="sortOrder">Sort order.</param>
        /// <param name="currentSort">Current sort field.</param>
        /// <param name="pageNo">Page number.</param>
        /// <param name="pageSize">Page size.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public async Task<IActionResult> Index(string sortOrder = "asc", string currentSort = "Title", int pageNo = 0, int pageSize = 10)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var defautLayout = await dbContext.Layouts.FirstOrDefaultAsync(f => f.IsDefault);

            ViewData["Layouts"] = await BaseGetLayoutListItems();

            ViewData["sortOrder"] = sortOrder;
            ViewData["currentSort"] = currentSort;
            ViewData["pageNo"] = pageNo;
            ViewData["pageSize"] = pageSize;

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

            return View(await query.Skip(pageNo * pageSize).Take(pageSize).ToListAsync());
        }

        /// <summary>
        /// Gets a list of articles or pages that use a particular page template.
        /// </summary>
        /// <param name="id">Template ID.</param>
        /// <param name="sortOrder">Sort order or direction.</param>
        /// <param name="currentSort">Field being sorted on.</param>
        /// <param name="pageNo">Page number to retrieve.</param>
        /// <param name="pageSize">Number of items per page.</param>
        /// <param name="filter">Search filter.</param>
        /// <returns>Returns an <see cref="IActionResult"/>.</returns>
        public async Task<IActionResult> Pages(Guid id, string sortOrder = "asc", string currentSort = "Title", int pageNo = 0, int pageSize = 10, string filter = "")
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var template = await dbContext.Templates.FirstOrDefaultAsync(f => f.Id == id);

            if (template == null)
            {
                return NotFound();
            }

            ViewData["templateId"] = id;
            ViewData["sortOrder"] = sortOrder;
            ViewData["currentSort"] = currentSort;
            ViewData["pageNo"] = pageNo;
            ViewData["pageSize"] = pageSize;
            ViewData["template"] = template;
            ViewData["canApplyChanges"] = template.Content.ToLower().Contains(" data-ccms-ceid=");

            if (!string.IsNullOrEmpty(filter))
            {
                filter = filter.TrimStart('/');
            }

            ViewData["Filter"] = filter;

            ViewData["PublisherUrl"] = options.PublisherUrl;

            ViewData["ShowNotFoundBtn"] = !await dbContext.ArticleCatalog.Where(w => w.UrlPath == "not_found").CosmosAnyAsync();

            if (!string.IsNullOrEmpty(filter))
            {
                filter = filter.TrimStart('/');
            }

            ViewData["Filter"] = filter;

            ViewData["sortOrder"] = sortOrder;
            ViewData["currentSort"] = currentSort;
            ViewData["pageNo"] = pageNo;
            ViewData["pageSize"] = pageSize;

            var articles = await dbContext.ArticleCatalog.Where(w => w.TemplateId == template.Id).Select(s => new
            {
                s.ArticleNumber,
                s.Title,
                s.UrlPath,
                s.Published,
                s.Status,
                s.Updated,
                s.ArticlePermissions
            }).AsNoTracking().ToListAsync();

            var query = articles.AsQueryable();

            ViewData["RowCount"] = query.Count();

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

            var users = await dbContext.Users.Select(s => new { s.Id, s.Email }).ToListAsync();
            var roles = await dbContext.Roles.Select(s => new { s.Id, s.Name }).ToListAsync();

            var data = query.Skip(pageNo * pageSize).Take(pageSize).AsNoTracking().ToList();

            var model = new List<ArticleListItem>();

            foreach (var datum in data)
            {
                var item = new ArticleListItem()
                {
                    ArticleNumber = datum.ArticleNumber,
                    IsDefault = datum.UrlPath.Equals("root", StringComparison.CurrentCultureIgnoreCase),
                    UrlPath = datum.UrlPath,
                    LastPublished = datum.Published,
                    Status = datum.Status,
                    Updated = datum.Updated,
                    Title = datum.Title
                };

                if (datum.ArticlePermissions != null && datum.ArticlePermissions.Count > 0)
                {
                    var userIds = datum.ArticlePermissions.Where(w => !w.IsRoleObject).Select(s => s.IdentityObjectId).ToList();
                    if (userIds.Any())
                    {
                        item.Permissions.AddRange(users.Where(s => userIds.Contains(s.Id)).Select(s => s.Email).ToArray());
                    }

                    var roleds = datum.ArticlePermissions.Where(w => w.IsRoleObject).Select(s => s.IdentityObjectId).ToList();
                    if (roleds.Any())
                    {
                        item.Permissions.AddRange(roles.Where(s => roleds.Contains(s.Id)).Select(s => s.Name).ToArray());
                    }
                }

                model.Add(item);
            }

            return View(model);
        }

        /// <summary>
        /// Create a template method.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public async Task<IActionResult> Create()
        {
            var defautLayout = await dbContext.Layouts.FirstOrDefaultAsync(f => f.IsDefault);

            var entity = new Template
            {
                Title = "New Template " + await dbContext.Templates.CountAsync(),
                Description = "<p>New template, please add descriptive and helpful information here.</p>",
                Content = "<p>" + LoremIpsum.SubSection1 + "</p>",
                LayoutId = defautLayout?.Id,
                CommunityLayoutId = defautLayout?.CommunityLayoutId
            };

            entity.Content = articleLogic.Ensure_ContentEditable_IsMarked(entity.Content);

            dbContext.Templates.Add(entity);
            await dbContext.SaveChangesAsync();
            return RedirectToAction("EditCode", "Templates", new { entity.Id });
        }

        /// <summary>
        /// Edit template title and description.
        /// </summary>
        /// <param name="Id">Template ID.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public async Task<IActionResult> Edit(Guid Id)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var template = await dbContext.Templates.FirstOrDefaultAsync(f => f.Id == Id);
            ViewData["Title"] = template.Title;

            var model = new TemplateEditViewModel()
            {
                Title = template.Title,
                Description = template.Description,
                Id = Id
            };
            return View(model);
        }

        /// <summary>
        /// Save changes to template title and description.
        /// </summary>
        /// <param name="model">Template edit post model.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(TemplateEditViewModel model)
        {
            model.Description = CryptoJsDecryption.Decrypt(model.Description);

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var template = await dbContext.Templates.FirstOrDefaultAsync(f => f.Id == model.Id);
            template.Title = model.Title;
            template.Description = model.Description;
            await dbContext.SaveChangesAsync();

            return RedirectToAction("Index");
        }

        /// <summary>
        /// Edit template code.
        /// </summary>
        /// <param name="id">Template ID.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public async Task<IActionResult> EditCode(Guid id)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var entity = await dbContext.Templates.FirstOrDefaultAsync(f => f.Id == id);

            var model = new TemplateCodeEditorViewModel
            {
                Id = entity.Id,
                EditorTitle = "Template Editor",
                Title = entity.Title,
                EditorFields = new List<EditorField>
                {
                    new ()
                    {
                        EditorMode = EditorMode.Html,
                        FieldName = "Html Content",
                        FieldId = "Content",
                        IconUrl = "~/images/seti-ui/icons/html.svg"
                    }
                },
                EditingField = "Content",
                Content = articleLogic.Ensure_ContentEditable_IsMarked(entity.Content),
                Version = 0,
                CustomButtons = new List<string>
                {
                    "Preview"
                }
            };
            return View(model);
        }

        /// <summary>
        /// Save edited template code.
        /// </summary>
        /// <param name="model">Template post model.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        [HttpPost]
        public async Task<IActionResult> EditCode(TemplateCodeEditorViewModel model)
        {
            model.Content = CryptoJsDecryption.Decrypt(model.Content);

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // Check for nested editable regions.
            if (!NestedEditableRegionValidation.Validate(model.Content))
            {
                ModelState.AddModelError("Content", "Cannot have nested editable regions.");
            }

            if (ModelState.IsValid)
            {
                var entity = await dbContext.Templates.FirstOrDefaultAsync(f => f.Id == model.Id);

                entity.Title = model.Title;

                entity.Content = articleLogic.Ensure_ContentEditable_IsMarked(model.Content);

                await dbContext.SaveChangesAsync();

                model = new TemplateCodeEditorViewModel
                {
                    Id = entity.Id,
                    Title = entity.Title,
                    EditorTitle = "Template Editor",
                    EditorFields = new List<EditorField>
                {
                    new ()
                    {
                        EditorMode = EditorMode.Html,
                        FieldName = "Html Content",
                        FieldId = "Content",
                        IconUrl = "~/images/seti-ui/icons/html.svg"
                    }
                },
                    EditingField = "Content",
                    Content = entity.Content,
                    CustomButtons = new List<string>
                {
                    "Preview"
                },
                    IsValid = true
                };
            }

            return Json(model);
        }

        /// <summary>
        /// Loads the designer GUI.
        /// </summary>
        /// <param name="id">Template ID.</param>
        /// <returns>View.</returns>
        public async Task<IActionResult> Designer(Guid id)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // Loads GrapeJS.
            ViewData["IsDesigner"] = true;

            var template = await dbContext.Templates.FirstOrDefaultAsync(f => f.Id == id);
            if (template == null)
            {
                return NotFound();
            }

            var config = new DesignerConfig(await dbContext.Layouts.FirstOrDefaultAsync(f => f.IsDefault), id.ToString(), template.Title);
            var assets = await FileManagerController.GetImageAssetArray(storageContext, "/pub", "/pub/articles");
            if (assets != null)
            {
                config.ImageAssets.AddRange(assets);
            }

            return View(config);
        }

        /// <summary>
        /// Visual designer based on GrapeJS.
        /// </summary>
        /// <param name="id">Template ID.</param>
        /// <returns>IActionResult.</returns>
        [HttpGet]
        public async Task<IActionResult> DesignerData(Guid id)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var entity = await dbContext.Templates.FirstOrDefaultAsync(f => f.Id == id);

            var htmlContent = articleLogic.Ensure_ContentEditable_IsMarked(entity.Content);

            return Json(new project(htmlContent));
        }

        /// <summary>
        /// Save designer data.
        /// </summary>
        /// <param name="id">Template ID.</param>
        /// <param name="title">Template title.</param>
        /// <param name="htmlContent">HTML content.</param>
        /// <param name="cssContent">CSS content.</param>
        /// <returns>IActionResult.</returns>
        [HttpPost]
        public async Task<IActionResult> DesignerData(Guid id, string title, string htmlContent, string cssContent)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            DesignerDataViewModel model = new DesignerDataViewModel()
            {
                Id = id,
                HtmlContent = CryptoJsDecryption.Decrypt(htmlContent),
                CssContent = CryptoJsDecryption.Decrypt(cssContent),
                Title = title
            };

            // Check for nested editable regions.
            if (!NestedEditableRegionValidation.Validate(model.HtmlContent))
            {
                return BadRequest("Cannot have nested editable regions.");
            }

            var entity = await dbContext.Templates.FirstOrDefaultAsync(f => f.Id == model.Id);

            if (entity == null)
            {
                return NotFound();
            }

            model.HtmlContent = articleLogic.Ensure_ContentEditable_IsMarked(model.HtmlContent);

            if (string.IsNullOrEmpty(model.Title))
            {
                var c = await dbContext.Templates.CountAsync();
                entity.Title = string.IsNullOrEmpty(entity.Title) ? $"Template {c}" : entity.Title;
            }

            var designerUtils = new DesignerUtilities();
            entity.Content = designerUtils.AssembleDesignerOutput(model);

            await dbContext.SaveChangesAsync();

            return Json(new { success = true });
        }

        /// <summary>
        /// Preview a template.
        /// </summary>
        /// <param name="id">Template ID.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public async Task<IActionResult> Preview(Guid id)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var entity = await dbContext.Templates.FirstOrDefaultAsync(f => f.Id == id);

            var guid = Guid.NewGuid();

            // Template preview
            ArticleViewModel model = new()
            {
                ArticleNumber = 1,
                LanguageCode = string.Empty,
                LanguageName = string.Empty,
                CacheDuration = 10,
                Content = articleLogic.Ensure_ContentEditable_IsMarked(entity.Content),
                StatusCode = StatusCodeEnum.Active,
                Id = entity.Id,
                Published = DateTimeOffset.UtcNow,
                Title = entity.Title,
                UrlPath = guid.ToString(),
                Updated = DateTimeOffset.UtcNow,
                VersionNumber = 1,
                HeadJavaScript = string.Empty,
                FooterJavaScript = string.Empty,
                Layout = await articleLogic.GetDefaultLayout()
            };

            ViewData["UseGoogleTranslate"] = false;

            return View("~/Views/Home/Preview.cshtml", model);
        }

        /// <summary>
        /// Preview a template.
        /// </summary>
        /// <param name="id">Template ID.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public async Task<IActionResult> Trash(Guid id)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var entity = await dbContext.Templates.FirstOrDefaultAsync(f => f.Id == id);

            dbContext.Templates.Remove(entity);

            await dbContext.SaveChangesAsync();

            return RedirectToAction("Index");
        }

        /// <summary>
        /// Updates a page using the latest template version.
        /// </summary>
        /// <param name="id">Article number.</param>
        /// <param name="templateId">Template ID.</param>
        /// <returns>Returns a redirect to the live editor.</returns>
        public async Task<IActionResult> UpdatePage(int id, Guid templateId)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var template = await dbContext.Templates.FirstOrDefaultAsync(f => f.Id == templateId);
            if (template == null)
            {
                return NotFound($"Template with ID '{templateId} was not found.");
            }

            await ApplyTemplateChanges(id, template.Content);

            return RedirectToAction("Edit", "Editor", new { id = id });
        }

        /// <summary>
        /// Updates all the pages that use this template.
        /// </summary>
        /// <param name="id">Template ID.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public async Task<IActionResult> UpdateAll(Guid id)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            await UpdateAllPages(id);

            return RedirectToAction("Pages", routeValues: new { id });
        }

        private async Task UpdateAllPages(Guid id)
        {
            var pages = await dbContext.ArticleCatalog.Where(w => w.TemplateId == id).ToListAsync();
            var template = await dbContext.Templates.AsNoTracking().FirstOrDefaultAsync(f => f.Id == id);
            if (template != null)
            {
                foreach (var page in pages)
                {
                    await ApplyTemplateChanges(page.ArticleNumber, template.Content);
                }
            }
        }

        /// <summary>
        /// Applies the latest template to an article, creating a new version ready to edit.
        /// </summary>
        /// <param name="articleNumber">Article number.</param>
        /// <param name="templateContent">Latest template to be applied.</param>
        private async Task ApplyTemplateChanges(int articleNumber, string templateContent)
        {
            var article = await articleLogic.GetArticleByArticleNumber(articleNumber, null);

            // Pull out the editable DIVs.
            var articleHtmlDoc = new HtmlDocument();
            var templateHtmlDoc = new HtmlDocument();

            articleHtmlDoc.LoadHtml(article.Content);
            templateHtmlDoc.LoadHtml(templateContent);

            var originalEditableDivs = articleHtmlDoc.DocumentNode.SelectNodes("//*[@data-ccms-ceid]");
            var templateEditableDivs = templateHtmlDoc.DocumentNode.SelectNodes("//*[@data-ccms-ceid]");

            // Find the region we are updating
            foreach (var div in templateEditableDivs)
            {
                var original = originalEditableDivs.FirstOrDefault(w => w.Attributes["data-ccms-ceid"].Value == div.Attributes["data-ccms-ceid"].Value);
                if (original != null)
                {
                    // Update the region now
                    div.InnerHtml = original.InnerHtml;
                }
            }

            article.VersionNumber = 0;
            article.Content = templateHtmlDoc.DocumentNode.OuterHtml;

            await articleLogic.SaveArticle(article, Guid.Parse(await GetUserId()));
            Console.WriteLine($"Template applied to article {articleNumber}");
        }
    }
}

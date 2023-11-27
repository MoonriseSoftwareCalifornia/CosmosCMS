// <copyright file="TemplatesController.cs" company="Moonrise Software, LLC">
// Copyright (c) Moonrise Software, LLC. All rights reserved.
// Licensed under the GNU Public License, Version 3.0 (https://www.gnu.org/licenses/gpl-3.0.html)
// See https://github.com/MoonriseSoftwareCalifornia/CosmosCMS
// for more information concerning the license and the contributors participating to this project.
// </copyright>

namespace CDT.Cosmos.Cms.Controllers
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using global::Cosmos.Cms.Common.Services.Configurations;
    using global::Cosmos.Cms.Controllers;
    using global::Cosmos.Cms.Data.Logic;
    using global::Cosmos.Cms.Models;
    using global::Cosmos.Common.Data;
    using global::Cosmos.Common.Data.Logic;
    using global::Cosmos.Common.Models;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Identity;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Options;

    /// <summary>
    /// Templates controller.
    /// </summary>
    [Authorize(Roles = "Administrators, Editors")]
    public class TemplatesController : BaseController
    {
        private readonly ArticleEditLogic articleLogic;
        private readonly ApplicationDbContext dbContext;
        private readonly UserManager<IdentityUser> userManager;

        /// <summary>
        /// Initializes a new instance of the <see cref="TemplatesController"/> class.
        /// Constructor.
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="dbContext"></param>
        /// <param name="options"></param>
        /// <param name="userManager"></param>
        /// <param name="articleLogic"></param>
        /// <exception cref="Exception"></exception>
        public TemplatesController(ILogger<TemplatesController> logger, ApplicationDbContext dbContext,
            IOptions<CosmosConfig> options, UserManager<IdentityUser> userManager,
            ArticleEditLogic articleLogic) :
            base(dbContext, userManager, articleLogic, options)
        {
            this.userManager = userManager;
            this.dbContext = dbContext;
            this.articleLogic = articleLogic;
        }

        /// <summary>
        /// Index view model.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public async Task<IActionResult> Index(string sortOrder = "asc", string currentSort = "Title", int pageNo = 0, int pageSize = 10)
        {
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
            dbContext.Templates.Add(entity);
            await dbContext.SaveChangesAsync();
            return RedirectToAction("EditCode", "Templates", new { entity.Id });
        }

        /// <summary>
        /// Edit template title and description.
        /// </summary>
        /// <param name="Id"></param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public async Task<IActionResult> Edit(Guid Id)
        {
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
        /// <param name="model"></param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(TemplateEditViewModel model)
        {
            ViewData["Title"] = model.Title;

            if (ModelState.IsValid)
            {
                var template = await dbContext.Templates.FirstOrDefaultAsync(f => f.Id == model.Id);
                template.Title = model.Title;
                template.Description = model.Description;

                await dbContext.SaveChangesAsync();

                return RedirectToAction("Index");
            }

            return View(model);
        }

        /// <summary>
        /// Edit template code.
        /// </summary>
        /// <param name="Id"></param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public async Task<IActionResult> EditCode(Guid Id)
        {
            var entity = await dbContext.Templates.FirstOrDefaultAsync(f => f.Id == Id);

            var model = new TemplateCodeEditorViewModel
            {
                Id = entity.Id,
                EditorTitle = "Template Editor",
                Title = entity.Title,
                EditorFields = new List<EditorField>
                {
                    new()
                    {
                        EditorMode = EditorMode.Html,
                        FieldName = "Html Content",
                        FieldId = "Content",
                        IconUrl = "~/images/seti-ui/icons/html.svg"
                    }
                },
                EditingField = "Content",
                Content = entity.Content,
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
        /// <param name="model"></param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        [HttpPost]
        public async Task<IActionResult> EditCode(TemplateCodeEditorViewModel model)
        {
            if (ModelState.IsValid)
            {
                var entity = await dbContext.Templates.FirstOrDefaultAsync(f => f.Id == model.Id);

                entity.Title = model.Title;
                entity.Content = model.Content;

                await dbContext.SaveChangesAsync();

                model = new TemplateCodeEditorViewModel
                {
                    Id = entity.Id,
                    Title = entity.Title,
                    EditorTitle = "Template Editor",
                    EditorFields = new List<EditorField>
                {
                    new()
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
        /// Preview a template.
        /// </summary>
        /// <param name="Id"></param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public async Task<IActionResult> Trash(Guid Id)
        {
            var entity = await dbContext.Templates.FirstOrDefaultAsync(f => f.Id == Id);

            dbContext.Templates.Remove(entity);

            await dbContext.SaveChangesAsync();

            return RedirectToAction("Index");
        }

        /// <summary>
        /// Preview a template.
        /// </summary>
        /// <param name="Id"></param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public async Task<IActionResult> Preview(Guid Id)
        {
            var entity = await dbContext.Templates.FirstOrDefaultAsync(f => f.Id == Id);

            var guid = Guid.NewGuid();

            // Template preview
            ArticleViewModel model = new ArticleViewModel
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
    }
}

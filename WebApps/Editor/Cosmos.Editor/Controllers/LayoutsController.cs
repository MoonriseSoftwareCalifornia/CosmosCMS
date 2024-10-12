// <copyright file="LayoutsController.cs" company="Moonrise Software, LLC">
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
    using Cosmos.Common.Models;
    using Cosmos.Editor.Controllers;
    using HtmlAgilityPack;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Identity;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.AspNetCore.Mvc.ModelBinding;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Options;

    /// <summary>
    /// Layouts controller.
    /// </summary>
    // [ResponseCache(NoStore = true)]
    [Authorize(Roles = "Administrators, Editors")]
    public class LayoutsController : BaseController
    {
        private readonly ArticleEditLogic articleLogic;
        private readonly ApplicationDbContext dbContext;
        private readonly ILogger<LayoutsController> logger;
        private readonly Uri blobPublicAbsoluteUrl;
        private readonly IViewRenderService viewRenderService;

        /// <summary>
        /// Initializes a new instance of the <see cref="LayoutsController"/> class.
        /// </summary>
        /// <param name="dbContext">Database context.</param>
        /// <param name="userManager">User manager.</param>
        /// <param name="articleLogic"><see cref="ArticleEditLogic">Article edit logic</see>.</param>
        /// <param name="options"><see cref="CosmosConfig">Cosmos configuration</see> options.</param>
        /// <param name="logger">ILogger.</param>
        /// <param name="viewRenderService">View rendering service.</param>
        public LayoutsController(
            ApplicationDbContext dbContext,
            UserManager<IdentityUser> userManager,
            ArticleEditLogic articleLogic,
            IOptions<CosmosConfig> options,
            ILogger<LayoutsController> logger,
            IViewRenderService viewRenderService)
            : base(dbContext, userManager)
        {
            this.dbContext = dbContext;
            this.articleLogic = articleLogic;
            this.logger = logger;

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
        /// Gets a list of layouts.
        /// </summary>
        /// <param name="includeDefault">Default = false.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public async Task<IActionResult> GetLayoutList(bool includeDefault = false)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            if (includeDefault)
            {
                return Json(await dbContext.Layouts.OrderBy(o => o.LayoutName).Select(s => new { LayoutId = s.Id, s.LayoutName, s.Notes }).ToListAsync());
            }

            return Json(await dbContext.Layouts.Where(w => w.IsDefault == false).OrderBy(o => o.LayoutName).Select(s => new { LayoutId = s.Id, s.LayoutName, s.Notes }).ToListAsync());
        }

        /// <summary>
        /// Gets a list of layouts.
        /// </summary>
        /// <param name="sortOrder">Sort order either asc or desc (default is asc).</param>
        /// <param name="currentSort">Field to sort on.</param>
        /// <param name="pageNo">Page number to return.</param>
        /// <param name="pageSize">Number of records in each page.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public async Task<IActionResult> Index(string sortOrder = "asc", string currentSort = "LayoutName", int pageNo = 0, int pageSize = 10)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            ViewData["ShowCreateFirstLayout"] = !await dbContext.Layouts.CosmosAnyAsync();

            ViewData["ShowFirstPageBtn"] = !await dbContext.Articles.CosmosAnyAsync();

            ViewData["sortOrder"] = sortOrder;
            ViewData["currentSort"] = currentSort;
            ViewData["pageNo"] = pageNo;
            ViewData["pageSize"] = pageSize;

            var query = dbContext.Layouts.AsQueryable();

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
                    }
                }
            }

            var model = dbContext.Layouts.Select(s => new LayoutIndexViewModel
            {
                Id = s.Id,
                IsDefault = s.IsDefault,
                LayoutName = s.LayoutName,
                Notes = s.Notes
            });

            return View(await model.Skip(pageNo * pageSize).Take(pageSize).ToListAsync());
        }

        /// <summary>
        /// Page returns a list of community layouts.
        /// </summary>
        /// <param name="sortOrder">Sort order either asc or desc (default is asc).</param>
        /// <param name="currentSort">Field to sort on.</param>
        /// <param name="pageNo">Page number to return.</param>
        /// <param name="pageSize">Number of records in each page.</param>
        /// <returns>Returns an <see cref="IActionResult"/>.</returns>
        public IActionResult CommunityLayouts(string sortOrder = "asc", string currentSort = "Name", int pageNo = 0, int pageSize = 10)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            ViewData["sortOrder"] = sortOrder;
            ViewData["currentSort"] = currentSort;
            ViewData["pageNo"] = pageNo;
            ViewData["pageSize"] = pageSize;

            var utilities = new LayoutUtilities();

            var query = utilities.CommunityCatalog.LayoutCatalog.AsQueryable();

            ViewData["RowCount"] = query.Count();

            if (sortOrder == "desc")
            {
                if (!string.IsNullOrEmpty(currentSort))
                {
                    switch (currentSort)
                    {
                        case "ArticleNumber":
                            query = query.OrderByDescending(o => o.License);
                            break;
                        case "Name":
                            query = query.OrderByDescending(o => o.Name);
                            break;
                        case "Description":
                            query = query.OrderByDescending(o => o.Description);
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
                            query = query.OrderBy(o => o.License);
                            break;
                        case "Name":
                            query = query.OrderBy(o => o.Name);
                            break;
                        case "Description":
                            query = query.OrderBy(o => o.Description);
                            break;
                    }
                }
            }

            return View(query.Skip(pageNo * pageSize).Take(pageSize).ToList());
        }

        /// <summary>
        /// Create a new layout.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public async Task<IActionResult> Create()
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var layout = new Layout();
            layout.IsDefault = false;
            layout.LayoutName = "New Layout " + await dbContext.Layouts.CountAsync();
            layout.Notes = "New layout created. Please customize using code editor.";
            dbContext.Layouts.Add(layout);
            await dbContext.SaveChangesAsync();
            return RedirectToAction("EditCode", new { layout.Id });
        }

        /// <summary>
        /// Deletes a layout that is not the default layout.
        /// </summary>
        /// <param name="id">ID of the layout to delete.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public async Task<IActionResult> Delete(Guid id)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var entity = await dbContext.Layouts.FindAsync(id);

            if (!entity.IsDefault)
            {
                // also remove pages that go with this layout.
                var pages = await dbContext.Templates.Where(t => t.LayoutId == id).ToListAsync();
                dbContext.Templates.RemoveRange(pages);
                dbContext.Layouts.Remove(entity);
                await dbContext.SaveChangesAsync();
            }
            else
            {
                return BadRequest("Cannot delete the default layout.");
            }

            return RedirectToAction("Index");
        }

        /// <summary>
        /// Edit the page header and footer of a layout.
        /// </summary>
        /// <param name="id">ID of the layout..</param>
        /// <param name="header">Layout header content.</param>
        /// <param name="footer">Layout footer content.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        [HttpPost]
        public async Task<IActionResult> Edit([Bind("id,header,footer")] Guid id, string header, string footer)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var layout = await dbContext.Layouts.FirstOrDefaultAsync(i => i.Id == id);

            // Make editable
            // header = header.Replace(" contenteditable=\"", " crx=\"", StringComparison.CurrentCultureIgnoreCase);
            // footer = footer.Replace(" contenteditable=\"", " crx=\"", StringComparison.CurrentCultureIgnoreCase);
            layout.HtmlHeader = header;
            layout.FooterHtmlContent = footer;

            await dbContext.SaveChangesAsync();

            return await GetLayoutWithHomePage(id);
        }

        /// <summary>
        /// Gets a layout to edit it's notes.
        /// </summary>
        /// <param name="id">ID of the layout.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public async Task<IActionResult> EditNotes(Guid? id)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            if (id == null)
            {
                return RedirectToAction("Index");
            }

            var model = await dbContext.Layouts.Select(s => new LayoutIndexViewModel
            {
                Id = s.Id,
                IsDefault = s.IsDefault,
                LayoutName = s.LayoutName,
                Notes = s.Notes
            }).FirstOrDefaultAsync(f => f.Id == id.Value);

            if (model == null)
            {
                return NotFound();
            }

            return View(model);
        }

        /// <summary>
        /// Edit layout notes.
        /// </summary>
        /// <param name="model">Layout post <see cref="LayoutIndexViewModel">model</see>.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        [HttpPost]
        public async Task<IActionResult> EditNotes([Bind(include: "Id,IsDefault,LayoutName,Notes")] LayoutIndexViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            if (model != null)
            {
                var layout = await dbContext.Layouts.FindAsync(model.Id);
                layout.LayoutName = model.LayoutName;
                var contentHtmlDocument = new HtmlDocument();
                contentHtmlDocument.LoadHtml(HttpUtility.HtmlDecode(model.Notes));
                if (contentHtmlDocument.ParseErrors.Any())
                {
                    foreach (var error in contentHtmlDocument.ParseErrors)
                    {
                        ModelState.AddModelError("Notes", error.Reason);
                    }
                }

                var remove = "<div style=\"display:none;\"></div>";
                layout.Notes = contentHtmlDocument.ParsedText.Replace(remove, string.Empty).Trim();

                if (model.IsDefault)
                {
                    var layouts = await dbContext.Layouts.Where(w => w.Id != model.Id).ToListAsync();
                    foreach (var layout1 in layouts)
                    {
                        layout1.IsDefault = false;
                    }
                }

                await dbContext.SaveChangesAsync();
            }

            return RedirectToAction("Index");
        }

        /// <summary>
        /// Edit code for a layout.
        /// </summary>
        /// <param name="id">Layout ID.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public async Task<IActionResult> EditCode(Guid? id)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            if (id == null)
            {
                return NotFound();
            }

            var layout = await dbContext.Layouts.FirstOrDefaultAsync(f => f.Id == id.Value);
            if (layout == null)
            {
                return NotFound();
            }

            ViewData["PageTitle"] = layout.LayoutName;

            var model = new LayoutCodeViewModel
            {
                Id = layout.Id,
                EditorTitle = layout.LayoutName,
                EditorFields = new List<EditorField>
                {
                    new ()
                    {
                        FieldId = "Head",
                        FieldName = "Head",
                        EditorMode = EditorMode.Html,
                        ToolTip = "Layout content to appear in the HEAD of every page."
                    },
                    new ()
                    {
                        FieldId = "HtmlHeader",
                        FieldName = "Header Content",
                        EditorMode = EditorMode.Html,
                        ToolTip = "Layout body header content to appear on every page."
                    },
                    new ()
                    {
                        FieldId = "FooterHtmlContent",
                        FieldName = "Footer Content",
                        EditorMode = EditorMode.Html,
                        ToolTip = "Layout footer content to appear at the bottom of the body on every page."
                    }
                },
                CustomButtons = new List<string> { "Preview", "Layouts" },
                Head = layout.Head,
                HtmlHeader = layout.HtmlHeader,
                BodyHtmlAttributes = layout.BodyHtmlAttributes,
                FooterHtmlContent = layout.FooterHtmlContent,
                EditingField = "Head"
            };
            return View(model);
        }

        /// <summary>
        ///     Saves the code and html of the page.
        /// </summary>
        /// <param name="id">ID of the layout.</param>
        /// <param name="layout">Post <see cref="LayoutCodeViewModel">model</see>.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        /// <remarks>
        ///     <para>
        ///         This method saves page code to the database. The following properties are validated with method
        ///         <see cref="BaseController.BaseValidateHtml" />:
        ///     </para>
        ///     <list type="bullet">
        ///         <item>
        ///             <see cref="LayoutCodeViewModel.Head" />
        ///         </item>
        ///         <item>
        ///             <see cref="LayoutCodeViewModel.HtmlHeader" />
        ///         </item>
        ///         <item>
        ///             <see cref="LayoutCodeViewModel.FooterHtmlContent" />
        ///         </item>
        ///     </list>
        ///     <para>
        ///         HTML formatting errors that could not be automatically fixed by <see cref="BaseController.BaseValidateHtml" />
        ///         are logged with <see cref="ControllerBase.ModelState" />.
        ///     </para>
        /// </remarks>
        /// <exception cref="NotFoundResult">Not found.</exception>
        /// <exception cref="UnauthorizedResult">Unauthorized access attempted.</exception>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditCode(Guid id, LayoutCodeViewModel layout)
        {
            if (id != layout.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    // Strip out BOM
                    layout.Head = StripBOM(layout.Head);
                    layout.HtmlHeader = StripBOM(layout.HtmlHeader);
                    layout.FooterHtmlContent = StripBOM(layout.FooterHtmlContent);
                    layout.BodyHtmlAttributes = StripBOM(layout.BodyHtmlAttributes);

                    // This layout now is the default, make sure the others are set to "false."
                    var entity = await dbContext.Layouts.FindAsync(layout.Id);
                    entity.FooterHtmlContent =
                        BaseValidateHtml("FooterHtmlContent", layout.FooterHtmlContent);
                    entity.Head = BaseValidateHtml("Head", layout.Head);
                    entity.HtmlHeader = BaseValidateHtml("HtmlHeader", layout.HtmlHeader);
                    entity.BodyHtmlAttributes = layout.BodyHtmlAttributes;

                    // Check validation again after validation of HTML
                    await dbContext.SaveChangesAsync();

                    var jsonModel = new SaveCodeResultJsonModel
                    {
                        ErrorCount = ModelState.ErrorCount,
                        IsValid = ModelState.IsValid
                    };
                    jsonModel.Errors.AddRange(ModelState.Values
                        .Where(w => w.ValidationState == ModelValidationState.Invalid)
                        .ToList());
                    jsonModel.ValidationState = ModelState.ValidationState;

                    await PurgeCdn();

                    return Json(jsonModel);
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!LayoutExists(layout.Id))
                    {
                        return NotFound();
                    }

                    throw;
                }
                catch (Exception)
                {
                    throw;
                }
            }

            ViewData["PageTitle"] = layout.EditorTitle;
            return View(layout);
        }

        /// <summary>
        /// Preview. 
        /// </summary>
        /// <param name="id">ID of the layout to preview.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public async Task<IActionResult> Preview(Guid id)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest();
            }

            var layout = await dbContext.Layouts.FindAsync(id);
            if (layout == null)
            {
                return NotFound();
            }

            var model = await articleLogic.GetByUrl(string.Empty);
            model.Layout = new LayoutViewModel(layout);
            model.EditModeOn = false;
            model.ReadWriteMode = false;
            model.PreviewMode = true;

            return View("~/Views/Home/Preview.cshtml", model);
        }

        /// <summary>
        /// Preview how a layout will look in edit mode.
        /// </summary>
        /// <param name="id">ID of the layout.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public async Task<IActionResult> EditPreview(int id)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest();
            }

            var layout = await dbContext.Layouts.FindAsync(id);
            var model = await articleLogic.GetByUrl(string.Empty);
            model.Layout = new LayoutViewModel(layout);
            model.EditModeOn = true;
            model.ReadWriteMode = true;
            model.PreviewMode = true;
            return View("~/Views/Home/Index.cshtml", model);
        }

        /// <summary>
        /// Exports a layout with a blank page.
        /// </summary>
        /// <param name="id">ID of the layout.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        [Authorize(Roles = "Administrators, Editors, Authors, Team Members")]
        public async Task<IActionResult> ExportLayout(int? id)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest();
            }

            var article = await articleLogic.GetByUrl(string.Empty);

            var view = "~/Views/Layouts/ExportLayout.cshtml";
            var exportName = $"layout-{article.Layout.Id}.html";

            if (id.HasValue)
            {
                if (id.Value < 0)
                {
                    // Blank layout
                    view = "~/Views/Layouts/ExportBlank.cshtml";
                    exportName = "blank-layout.html";
                }
                else
                {
                    var layout = await dbContext.Layouts.FindAsync(id.Value);
                    article.Layout = new LayoutViewModel(layout);
                }
            }

            var htmlUtilities = new HtmlUtilities();

            article.Layout.Head = htmlUtilities.RelativeToAbsoluteUrls(article.Layout.Head, blobPublicAbsoluteUrl, false);

            // Layout body elements
            article.Layout.HtmlHeader = htmlUtilities.RelativeToAbsoluteUrls(article.Layout.HtmlHeader, blobPublicAbsoluteUrl, true);
            article.Layout.FooterHtmlContent = htmlUtilities.RelativeToAbsoluteUrls(article.Layout.FooterHtmlContent, blobPublicAbsoluteUrl, true);

            article.HeadJavaScript = htmlUtilities.RelativeToAbsoluteUrls(article.HeadJavaScript, blobPublicAbsoluteUrl, false);
            article.Content = htmlUtilities.RelativeToAbsoluteUrls(article.Content, blobPublicAbsoluteUrl, false);
            article.FooterJavaScript = htmlUtilities.RelativeToAbsoluteUrls(article.HeadJavaScript, blobPublicAbsoluteUrl, false);

            var html = await viewRenderService.RenderToStringAsync(view, article);

            var bytes = Encoding.UTF8.GetBytes(html);

            return File(bytes, "application/octet-stream", exportName);
        }

        /// <summary>
        /// Set a layout as the default layout.
        /// </summary>
        /// <param name="id">ID of the layout.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        [HttpGet]
        public async Task<IActionResult> SetLayoutAsDefault(Guid? id)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest();
            }

            if (id == null)
            {
                return RedirectToAction("Index");
            }

            var model = await dbContext.Layouts.Select(s => new LayoutIndexViewModel
            {
                Id = s.Id,
                IsDefault = s.IsDefault,
                LayoutName = s.LayoutName,
                Notes = s.Notes
            }).FirstOrDefaultAsync(f => f.Id == id.Value);

            if (model == null)
            {
                return NotFound();
            }

            return View(model);
        }

        /// <summary>
        ///     Sets a layout as the default layout.
        /// </summary>
        /// <param name="model">Post <see cref="LayoutIndexViewModel">view model</see>.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        [HttpPost]
        public async Task<IActionResult> SetLayoutAsDefault(LayoutIndexViewModel model)
        {

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var layout = await dbContext.Layouts.FirstOrDefaultAsync(f => f.Id == model.Id);
            layout.IsDefault = true;

            if (layout == null)
            {
                return RedirectToAction("Index", "Layouts");
            }

            await dbContext.SaveChangesAsync();
            var items = await dbContext.Layouts.Where(w => w.Id != model.Id).ToListAsync();
            foreach (var item in items)
            {
                item.IsDefault = false;
            }

            await dbContext.SaveChangesAsync();

            return RedirectToAction("Index", "Layouts");
        }

        /// <summary>
        /// Gets a community layout.
        /// </summary>
        /// <param name="id">Layout ID.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public async Task<IActionResult> ImportCommunityLayout(string id)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest();
            }

            try
            {
                if (await dbContext.Layouts.Where(c => c.CommunityLayoutId == id).CosmosAnyAsync())
                {
                    throw new Exception("Layout already loaded.");
                }

                var utilities = new LayoutUtilities();
                var layout = await utilities.GetCommunityLayout(id, false);
                var communityPages = await utilities.GetCommunityTemplatePages(id);
                layout.IsDefault = (await dbContext.Layouts.Where(a => a.IsDefault).CosmosAnyAsync()) == false;
                dbContext.Layouts.Add(layout);
                await dbContext.SaveChangesAsync();

                if (communityPages != null && communityPages.Any())
                {
                    var pages = communityPages.Select(p => new Template()
                    {
                        CommunityLayoutId = p.CommunityLayoutId,
                        Content = articleLogic.Ensure_ContentEditable_IsMarked(p.Content),
                        Description = p.Description,
                        LayoutId = layout.Id,
                        Title = p.Title,
                        PageType = p.PageType
                    });

                    // Mark the content editable regions.
                    dbContext.Templates.AddRange(pages);
                    await dbContext.SaveChangesAsync();

                    if (!await dbContext.Pages.CosmosAnyAsync() && !await dbContext.Articles.CosmosAnyAsync())
                    {
                        var homePageTemplate = await dbContext.Templates.FirstOrDefaultAsync(f => f.Title == "Home");
                        var page = new PublishedPage()
                        {
                            Title = "Home",
                            TemplateId = pages.First().Id
                        };
                        dbContext.Pages.Add(page);
                        await dbContext.SaveChangesAsync();
                    }
                }
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("Id", ex.Message);
            }

            return RedirectToAction("Index");
        }

        /// <summary>
        /// Gets the home page with the specified layout (may not be the default layout).
        /// </summary>
        /// <param name="id">Layout Id (default layout if null).</param>
        /// <returns>ViewResult with <see cref="ArticleViewModel"/>.</returns>
        private async Task<IActionResult> GetLayoutWithHomePage(Guid? id)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest();
            }

            // Get the home page
            var model = await articleLogic.GetByUrl(string.Empty);

            // Specify layout if given.
            if (id.HasValue)
            {
                var layout = await dbContext.Layouts.FirstOrDefaultAsync(i => i.Id == id.Value);
                model.Layout = new LayoutViewModel(layout);
            }

            // Make its editable
            model.Layout.HtmlHeader = model.Layout.HtmlHeader.Replace(" crx=\"", " contenteditable=\"", StringComparison.CurrentCultureIgnoreCase);
            model.Layout.FooterHtmlContent = model.Layout.FooterHtmlContent.Replace(" crx=\"", " contenteditable=\"", StringComparison.CurrentCultureIgnoreCase);

            return View(model);
        }

        private bool LayoutExists(Guid id)
        {
            return dbContext.Layouts.Where(e => e.Id == id).CosmosAnyAsync().Result;
        }

        private async Task PurgeCdn()
        {
            var settings = await Cosmos___CdnController.GetCdnConfiguration(dbContext);
            var cdnService = new Editor.Services.CdnService(settings);
            await cdnService.PurgeCdn(new List<string>() { "/" });
        }
    }
}

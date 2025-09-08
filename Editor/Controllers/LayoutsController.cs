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
    using Cosmos.BlobService;
    using Cosmos.Cms.Common.Services.Configurations;
    using Cosmos.Cms.Data.Logic;
    using Cosmos.Cms.Models;
    using Cosmos.Cms.Services;
    using Cosmos.Common.Data;
    using Cosmos.Common.Models;
    using Cosmos.Common.Services;
    using Cosmos.Editor.Data;
    using Cosmos.Editor.Data.Logic;
    using Cosmos.Editor.Models;
    using Cosmos.Editor.Models.GrapesJs;
    using HtmlAgilityPack;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Identity;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.AspNetCore.Mvc.ModelBinding;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.Logging;

    /// <summary>
    /// Layouts controller.
    /// </summary>
    // [ResponseCache(NoStore = true)]
    [Authorize(Roles = "Administrators, Editors")]
    public class LayoutsController : BaseController
    {
        private readonly ArticleEditLogic articleLogic;
        private readonly ApplicationDbContext dbContext;
        private readonly Uri blobPublicAbsoluteUrl;
        private readonly IViewRenderService viewRenderService;
        private readonly StorageContext storageContext;
        private readonly ILogger<LayoutsController> logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="LayoutsController"/> class.
        /// </summary>
        /// <param name="dbContext">Database context.</param>
        /// <param name="userManager">User manager.</param>
        /// <param name="articleLogic"><see cref="ArticleEditLogic">Article edit logic</see>.</param>
        /// <param name="options"><see cref="CosmosConfig">Cosmos configuration</see> options.</param>
        /// <param name="storageContext">Storage context.</param>
        /// <param name="viewRenderService">View rendering service.</param>
        /// <param name="logger">Log service.</param>
        public LayoutsController(
            ApplicationDbContext dbContext,
            UserManager<IdentityUser> userManager,
            ArticleEditLogic articleLogic,
            IEditorSettings options,
            StorageContext storageContext,
            IViewRenderService viewRenderService,
            ILogger<LayoutsController> logger)
            : base(dbContext, userManager)
        {
            this.dbContext = dbContext;
            this.articleLogic = articleLogic;
            this.storageContext = storageContext;
            this.logger = logger;

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
        }

        /// <summary>
        /// Gets a list of layouts.
        /// </summary>
        /// <returns>A list of layouts.</returns>
        public async Task<IActionResult> GetLayouts()
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var layouts = await dbContext.Layouts.ToListAsync();

            if (layouts.Any(l => l.Version.HasValue == false || l.Version == 0))
            {
                // Find the layout now published.
                var pub = layouts.FirstOrDefault(f => f.IsDefault);
                pub.Published = DateTimeOffset.UtcNow;
                pub.LastModified = DateTimeOffset.UtcNow;
                pub.Version = layouts.Count;

                // Increment the versions of the other layouts.
                var count = 1;
                foreach (var layout in layouts.Where(w => w.Id != pub.Id))
                {
                    layout.Published = null;
                    layout.LastModified = DateTimeOffset.UtcNow;
                    layout.Version = count;
                    count++;
                }

                await dbContext.SaveChangesAsync();
            }

            var htmlDoc = new HtmlAgilityPack.HtmlDocument();

            return Json(layouts.Select(s => new LayoutIndexViewModel ()
            {
                Id = s.Id,
                IsDefault = s.IsDefault,
                LayoutName = s.LayoutName,
                Notes = s.Notes,
                Version = s.Version.Value,
                LastModified = s.LastModified ?? DateTimeOffset.UtcNow,
                Published = s.Published
            }).OrderByDescending(o => o.Version).ToList());
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

            var model = query.Select(s => new LayoutIndexViewModel
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

            var layout = new Cosmos.Common.Data.Layout();
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
        /// Loads the designer GUI editing the latest version.
        /// </summary>
        /// <returns>View.</returns>
        public async Task<IActionResult> Designer()
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // Loads GrapeJS.
            ViewData["IsDesigner"] = true;

            var layout = await GetLayoutForEdit();

            var config = new DesignerConfig(layout, layout.Id.ToString(), layout.LayoutName);
            var assets = await FileManagerController.GetImageAssetArray(storageContext, "/pub", "/pub/articles");
            if (assets != null)
            {
                config.ImageAssets.AddRange(assets);
            }

            return View(config);
        }

        /// <summary>
        /// Gets data to edit.
        /// </summary>
        /// <returns>IActionResult.</returns>
        [HttpGet]
        public async Task<IActionResult> DesignerData()
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var layout = await GetLayoutForEdit();

            var builder = new StringBuilder();
            builder.AppendLine("<body>");
            builder.AppendLine("<!--CCMS--START--HEADER-->");
            if (string.IsNullOrEmpty(layout.HtmlHeader))
            {
                layout.Head = "<header style='height:50px;display:flex;justify-content:center;align-items: center;>HEADER GOES HERE</header>";
            }
            else
            {
                builder.AppendLine(layout.HtmlHeader);
            }

            builder.AppendLine("<!--CCMS--END--HEADER-->");
            builder.AppendLine("<div data-gjs-type='grapesjs-not-editable' draggable='false' style='height:50vh;display:flex;justify-content:center;align-items: center;'>");
            builder.AppendLine("<div style='text-align: center'>PAGE CONTENT GOES IN THIS BLOCK<br/>Cannot edit with layout designer.</div>");
            builder.AppendLine("</div>");
            builder.AppendLine("<!--CCMS--START--FOOTER-->");
            if (string.IsNullOrEmpty(layout.FooterHtmlContent))
            {
                layout.Head = "<footer style='height:50px;display:flex;justify-content:center;align-items: center;>FOOTER GOES HERE</footer>";
            }
            else
            {
                builder.AppendLine(layout.FooterHtmlContent);
            }

            builder.AppendLine("<!--CCMS--END--FOOTER-->");
            builder.AppendLine("</body>");
            return Json(new project(builder.ToString()));
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
            var layout = await dbContext.Layouts.FirstOrDefaultAsync(f => f.Id == id);

            if (layout == null)
            {
                return NotFound();
            }

            htmlContent = CryptoJsDecryption.Decrypt(htmlContent);
            cssContent = CryptoJsDecryption.Decrypt(cssContent);

            // Check for nested editable regions.
            if (!NestedEditableRegionValidation.Validate(htmlContent))
            {
                return BadRequest("Cannot have nested editable regions.");
            }

            // Get header and footer content.
            var header = GetTextBetween(htmlContent, "<!--CCMS--START--HEADER-->", "<!--CCMS--END--HEADER-->");
            var footer = GetTextBetween(htmlContent, "<!--CCMS--START--FOOTER-->", "<!--CCMS--END--FOOTER-->");

            // Assemble the designer output.
            var designerUtils = new DesignerUtilities();
            footer = designerUtils.AssembleDesignerOutput(new DesignerDataViewModel()
            {
                HtmlContent = footer,
                CssContent = cssContent,
                Title = title
            });

            // Update the layout.
            layout.HtmlHeader = header;
            layout.FooterHtmlContent = footer;
            layout.LastModified = DateTimeOffset.UtcNow;

            await dbContext.SaveChangesAsync();

            return Json(new { success = true });
        }

        /// <summary>
        /// Edit code for a layout.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public async Task<IActionResult> EditCode()
        {
            var layout = await GetLayoutForEdit();

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
        ///     Saves the code and html of the layout.
        /// </summary>
        /// <param name="model">Post <see cref="LayoutCodeViewModel">model</see>.</param>
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
        public async Task<IActionResult> EditCode(LayoutCodeViewModel model)
        {
            model.Head = CryptoJsDecryption.Decrypt(StripBOM(model.Head));
            model.HtmlHeader = CryptoJsDecryption.Decrypt(StripBOM(model.HtmlHeader));
            model.FooterHtmlContent = CryptoJsDecryption.Decrypt(StripBOM(model.FooterHtmlContent));

            if (!ModelState.IsValid)
            {
                ViewData["PageTitle"] = model.EditorTitle;
                return View(model);
            }

            // Strip out BOM
            model.BodyHtmlAttributes = StripBOM(model.BodyHtmlAttributes);

            // This layout now is the default, make sure the others are set to "false."
            var layout = await dbContext.Layouts.FindAsync(model.Id);
            layout.FooterHtmlContent =
                BaseValidateHtml("FooterHtmlContent", model.FooterHtmlContent);
            layout.Head = BaseValidateHtml("Head", model.Head);
            layout.HtmlHeader = BaseValidateHtml("HtmlHeader", model.HtmlHeader);
            layout.BodyHtmlAttributes = model.BodyHtmlAttributes;
            layout.LastModified = DateTimeOffset.UtcNow;

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

            return Json(jsonModel);
        }

        /// <summary>
        /// Gets a layout to edit it's notes.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public async Task<IActionResult> EditNotes()
        {
            var layout = await GetLayoutForEdit();

            return View(new LayoutIndexViewModel
            {
                Id = layout.Id,
                IsDefault = layout.IsDefault,
                LayoutName = layout.LayoutName,
                Notes = layout.Notes
            });
        }

        /// <summary>
        /// Edit layout notes.
        /// </summary>
        /// <param name="model">Layout post <see cref="LayoutIndexViewModel">model</see>.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        [HttpPost]
        public async Task<IActionResult> EditNotes(LayoutIndexViewModel model)
        {
            model.Notes = CryptoJsDecryption.Decrypt(model.Notes);

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
                layout.LastModified = DateTimeOffset.UtcNow;

                await dbContext.SaveChangesAsync();
            }

            return RedirectToAction("Index");
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

            var model = await articleLogic.GetArticleByUrl(string.Empty);
            model.Layout = new LayoutViewModel(layout);
            model.EditModeOn = false;
            model.ReadWriteMode = false;
            model.PreviewMode = true;

            ViewData["LayoutId"] = model.Layout.Id.ToString();

            return RedirectToAction("Index", "Home", new { layoutId = layout.Id });
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
            var model = await articleLogic.GetArticleByUrl(string.Empty);
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
        public async Task<IActionResult> ExportLayout(Guid? id)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest();
            }

            var article = await articleLogic.GetArticleByUrl(string.Empty);

            var layout = await dbContext.Layouts.FirstOrDefaultAsync(f => f.Id == id);
            article.Layout = new LayoutViewModel(layout);

            var htmlUtilities = new HtmlUtilities();

            article.Layout.Head = htmlUtilities.RelativeToAbsoluteUrls(article.Layout.Head, blobPublicAbsoluteUrl, false);

            // Layout body elements
            article.Layout.HtmlHeader = htmlUtilities.RelativeToAbsoluteUrls(article.Layout.HtmlHeader, blobPublicAbsoluteUrl, true);
            article.Layout.FooterHtmlContent = htmlUtilities.RelativeToAbsoluteUrls(article.Layout.FooterHtmlContent, blobPublicAbsoluteUrl, true);

            article.HeadJavaScript = htmlUtilities.RelativeToAbsoluteUrls(article.HeadJavaScript, blobPublicAbsoluteUrl, false);
            article.Content = htmlUtilities.RelativeToAbsoluteUrls(article.Content, blobPublicAbsoluteUrl, false);
            article.FooterJavaScript = htmlUtilities.RelativeToAbsoluteUrls(article.HeadJavaScript, blobPublicAbsoluteUrl, false);

            var html = await viewRenderService.RenderToStringAsync("~/Views/Layouts/ExportLayout.cshtml", article);

            var bytes = Encoding.UTF8.GetBytes(html);

            return File(bytes, "application/octet-stream", $"layout-{article.Layout.Id}.html");
        }

        /// <summary>
        ///     Publishes a layout as the default layout.
        /// </summary>
        /// <param name="id">Layout ID.</param>
        /// <returns>Success or failure.</returns>
        [HttpPost]
        public async Task<IActionResult> Publish(Guid id)
        {
            var layout = await dbContext.Layouts.FirstOrDefaultAsync(f => f.Id == id);

            if (layout == null)
            {
                return NotFound();
            }

            if (layout.IsDefault)
            {
                return Ok();
            }

            layout.IsDefault = true;

            var others = await dbContext.Layouts.Where(w => w.Id != id && w.IsDefault == true).ToListAsync();
            foreach (var item in others)
            {
                item.IsDefault = false;
            }

            await dbContext.SaveChangesAsync();

            return RedirectToAction("Publish", "Editor");
        }

        /// <summary>
        /// Gets a community layout.
        /// </summary>
        /// <param name="id">Layout ID.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public async Task<IActionResult> Import(string id)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest();
            }

            try
            {
                if (await dbContext.Layouts.Where(c => c.CommunityLayoutId == id).CosmosAnyAsync())
                {
                    throw new ArgumentException("Layout already loaded.");
                }

                var utilities = new LayoutUtilities();
                var layout = await utilities.GetCommunityLayout(id, false);
                var communityPages = await utilities.GetCommunityTemplatePages(id);

                if ((await dbContext.Layouts.FirstOrDefaultAsync(a => a.IsDefault)) == null)
                {
                    layout.Version = 1;
                    layout.IsDefault = true;
                }
                else
                {
                    layout.Version = (await dbContext.Layouts.CountAsync()) + 1;
                    layout.IsDefault = false;
                }

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
        ///  Promotes a layout to a new version.
        /// </summary>
        /// <param name="id">ID of the layout to promote.</param>
        /// <returns>New version number.</returns>
        public async Task<IActionResult> Promote(Guid id)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest();
            }

            var layout = await dbContext.Layouts.AsNoTracking().FirstOrDefaultAsync(f => f.Id == id);
            if (layout == null)
            {
                return NotFound();
            }

            var newLayout = await NewVersion(layout);

            return Json(newLayout.Version);
        }

        private async Task<Cosmos.Common.Data.Layout> GetLayoutForEdit()
        {
            var layout = await dbContext.Layouts.OrderByDescending(o => o.Version).FirstOrDefaultAsync();
            if (layout == null)
            {
                layout = new Layout()
                {
                    Id = Guid.NewGuid(),
                    IsDefault = true,
                    LayoutName = "Default Layout",
                    Notes = "Default layout created. Please customize using code editor.",
                    Version = 1,
                    LastModified = DateTimeOffset.UtcNow
                };
                dbContext.Layouts.Add(layout);
                await dbContext.SaveChangesAsync();
                return layout;
            }

            if (layout.IsDefault)
            {
                return await NewVersion(layout);
            }

            return layout;
        }

        /// <summary>
        ///  Creates a new layout from an existing layout.
        /// </summary>
        /// <param name="layout">Exiting layout.</param>
        /// <returns>New layout with an incremented version number.</returns>
        private async Task<Cosmos.Common.Data.Layout> NewVersion(Cosmos.Common.Data.Layout layout)
        {
            layout.Id = Guid.NewGuid();
            layout.IsDefault = false;
            layout.Published = null;
            layout.LastModified = DateTimeOffset.UtcNow;
            layout.Version = (await dbContext.Layouts.CountAsync()) + 1;
            dbContext.Layouts.Add(layout);
            await dbContext.SaveChangesAsync();
            return layout;
        }

        private string GetTextBetween(string input, string start, string end)
        {
            if (string.IsNullOrEmpty(input) || string.IsNullOrEmpty(start) || string.IsNullOrEmpty(end))
            {
                return string.Empty;
            }

            int startIndex = input.IndexOf(start);
            if (startIndex == -1)
            {
                return string.Empty;
            }

            startIndex += start.Length;
            int endIndex = input.IndexOf(end, startIndex);
            if (endIndex == -1)
            {
                return string.Empty;
            }

            return input.Substring(startIndex, endIndex - startIndex);
        }

    }
}

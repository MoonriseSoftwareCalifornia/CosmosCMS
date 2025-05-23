// <copyright file="ArticleEditLogic.cs" company="Moonrise Software, LLC">
// Copyright (c) Moonrise Software, LLC. All rights reserved.
// Licensed under the GNU Public License, Version 3.0 (https://www.gnu.org/licenses/gpl-3.0.html)
// See https://github.com/MoonriseSoftwareCalifornia/CosmosCMS
// for more information concerning the license and the contributors participating to this project.
// </copyright>

namespace Cosmos.Editor.Data.Logic
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using System.Web;
    using Azure.ResourceManager;
    using Cosmos.BlobService;
    using Cosmos.Cms.Common.Services.Configurations;
    using Cosmos.Cms.Controllers;
    using Cosmos.Cms.Models;
    using Cosmos.Cms.Services;
    using Cosmos.Common.Data;
    using Cosmos.Common.Data.Logic;
    using Cosmos.Common.Models;
    using Cosmos.Editor.Controllers;
    using Cosmos.Editor.Services;
    using Microsoft.AspNetCore.Http;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.Caching.Memory;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Options;
    using Newtonsoft.Json;
    using SendGrid.Helpers.Errors.Model;
    using X.Web.Sitemap.Extensions;

    /// <summary>
    ///     Article Editor Logic.
    /// </summary>
    /// <remarks>
    ///     Is derived from base class <see cref="ArticleLogic" />, adds on content editing functionality.
    /// </remarks>
    public class ArticleEditLogic : ArticleLogic
    {
        private readonly IViewRenderService viewRenderService;
        private readonly StorageContext storageContext;
        private readonly ILogger<ArticleEditLogic> logger;
        private readonly IHttpContextAccessor accessor;
        private readonly EditorSettings settings;

        /// <summary>
        /// Initializes a new instance of the <see cref="ArticleEditLogic"/> class.
        ///     Constructor.
        /// </summary>
        /// <param name="dbContext">Database context.</param>
        /// <param name="memoryCache">Memory cache.</param>
        /// <param name="config">Cosmos configuration.</param>
        /// <param name="viewRenderService">View rendering service used to save static web pages.</param>
        /// <param name="storageContext">Storage service used to manage static website blobs.</param>
        /// <param name="logger">Log service.</param>
        /// <param name="accessor">Http context access.</param>
        /// <param name="settings">Editor settings - used with multitenant editor.</param>
        public ArticleEditLogic(
            ApplicationDbContext dbContext,
            IMemoryCache memoryCache,
            IOptions<CosmosConfig> config,
            IViewRenderService viewRenderService,
            StorageContext storageContext,
            ILogger<ArticleEditLogic> logger,
            IHttpContextAccessor accessor,
            IEditorSettings settings)
            : base(
                dbContext,
                config,
                memoryCache,
                true)
        {
            this.viewRenderService = viewRenderService;
            this.storageContext = storageContext;
            this.logger = logger;
            this.accessor = accessor;
            this.settings = (EditorSettings)settings;
        }

        /// <summary>
        ///     Gets database Context with Synchronize Context.
        /// </summary>
        public new ApplicationDbContext DbContext => base.DbContext;

        /// <summary>
        ///     Validate that the title is not already taken by another article.
        /// </summary>
        /// <param name="title">Title.</param>
        /// <param name="articleNumber">Current article number.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        /// <remarks>
        /// If article number is given, this checks  all other article
        /// numbers to see if this title is already taken.
        /// If not given, this method returns true if article name already in use.
        /// </remarks>
        public async Task<bool> ValidateTitle(string title, int? articleNumber)
        {
            // Make sure it doesn't conflict with the publi blob path
            var reservedPaths = (await GetReservedPaths()).Select(s => s.Path.ToLower()).ToArray();

            foreach (var reservedPath in reservedPaths)
            {
                if (reservedPath.EndsWith('*'))
                {
                    var value = reservedPath.TrimEnd('*');
                    if (title.ToLower().StartsWith(value))
                    {
                        return false;
                    }
                }
                else if (title.ToLower() == reservedPath.ToLower())
                {
                    return false;
                }
            }

            Article article;
            if (articleNumber.HasValue)
            {
                article = await DbContext.Articles.FirstOrDefaultAsync(a =>
                    a.ArticleNumber != articleNumber && // look only at other article numbers
                    a.Title.ToLower() == title.Trim().ToLower() && // Is the title used already
                    a.StatusCode != (int)StatusCodeEnum.Deleted); // and the page is active (active or is inactive)
            }
            else
            {
                article = await DbContext.Articles.FirstOrDefaultAsync(a =>
                    a.Title.ToLower() == title.Trim().ToLower() && // Is the title used already
                    a.StatusCode != (int)StatusCodeEnum.Deleted); // and the page is active (active or is inactive)
            }

            if (article == null)
            {
                return true;
            }

            return false;
        }

        /// <summary>
        ///     Creates a new article, save it to the database before returning a copy for editing.
        /// </summary>
        /// <param name="title">Article title.</param>
        /// <param name="userId">ID of the user creating the page.</param>
        /// <param name="templateId">Page template ID.</param>
        /// <returns>Unsaved article ready to edit and save.</returns>
        /// <remarks>
        ///     <para>
        ///         Creates a new article, saves it to the database, and is ready to edit.  Uses <see cref="ArticleLogic.GetDefaultLayout" /> to get the
        ///         layout,
        ///         and builds the <see cref="ArticleViewModel" /> using method
        ///         <seealso cref="ArticleLogic.BuildArticleViewModel(Article, string, bool)" />. Creates a new article number.
        ///     </para>
        ///     <para>
        ///         If a template ID is given, the contents of this article is loaded with content from the <see cref="Template" />.
        ///     </para>
        ///     <para>
        ///         If this is the first article, it is saved as root and published immediately.
        ///     </para>
        /// </remarks>
        public async Task<ArticleViewModel> CreateArticle(string title, string userId, Guid? templateId = null)
        {
            // Is this the first article? If so, make it the root and publish it.
            var isFirstArticle = !await DbContext.Articles.CosmosAnyAsync();

            var isValidTitle = await ValidateTitle(title, null);

            if (!isValidTitle)
            {
                throw new ArgumentException($"Title '{title}' conflicts with another article or reserved word.");
            }

            var defaultTemplate = string.Empty;

            if (templateId.HasValue)
            {
                var template = await DbContext.Templates.FindAsync(templateId.Value);

                // For backward compatibility, make sure the templates are properly marked.
                // This ensures if the template are updated, the pages that use this page are properly updated.
                var content = Ensure_ContentEditable_IsMarked(template.Content);
                if (!content.Equals(template.Content))
                {
                    template.Content = content;
                    await DbContext.SaveChangesAsync();
                }

                defaultTemplate = template.Content;
            }

            if (string.IsNullOrEmpty(defaultTemplate))
            {
                defaultTemplate = "<div style='width: 100%;padding-left: 20px;padding-right: 20px;margin-left: auto;margin-right: auto;'>" +
                                  "<div contenteditable='true'><h1>Why Lorem Ipsum?</h1><p>" +
                                   LoremIpsum.WhyLoremIpsum + "</p></div>" +
                                  "</div>" +
                                  "</div>";
            }

            // Max returns the incorrect result.
            int max;
            if (!await DbContext.ArticleNumbers.CosmosAnyAsync())
            {
                max = 0;
            }
            else
            {
                max = await DbContext.ArticleNumbers.MaxAsync(m => m.LastNumber);
            }

            // Increment
            max++;

            // New article
            title = title.Trim('/');

            var article = new Article()
            {
                ArticleNumber = max,
                Content = Ensure_ContentEditable_IsMarked(defaultTemplate),
                StatusCode = (int)StatusCodeEnum.Active,
                Title = title,
                Updated = DateTimeOffset.Now,
                UrlPath = isFirstArticle ? "root" : NormailizeArticleUrl(title),
                VersionNumber = 1,
                Published = isFirstArticle ? DateTimeOffset.UtcNow : null,
                UserId = userId,
                TemplateId = templateId
            };

            DbContext.Articles.Add(article);
            DbContext.ArticleNumbers.Add(new ArticleNumber()
            {
                LastNumber = max
            });

            await DbContext.SaveChangesAsync();

            if (isFirstArticle)
            {
                await PublishArticle(article);
            }

            await CreateCatalogEntry(article);

            return await BuildArticleViewModel(article, "en-US");
        }

        /// <summary>
        ///   Gets or creates a catalog entry for an article.
        /// </summary>
        /// <param name="model">ArticleViewModel.</param>
        /// <returns>CatalogEntry.</returns>
        public async Task<CatalogEntry> GetCatalogEntry(ArticleViewModel model)
        {
            var article = await DbContext.Articles.FirstOrDefaultAsync(f => f.Id == model.Id);

            return await GetCatalogEntry(article);
        }

        /// <summary>
        /// Gets or creates a catalog entry for an article.
        /// </summary>
        /// <param name="article">Article for which to get the entry.</param>
        /// <returns>CatalogEntry.</returns>
        public async Task<CatalogEntry> GetCatalogEntry(Article article)
        {
            var entry = await DbContext.ArticleCatalog.FirstOrDefaultAsync(f => f.ArticleNumber == article.ArticleNumber);

            if (entry == null)
            {
                entry = await CreateCatalogEntry(article);
            }

            return entry;
        }

        /// <summary>
        /// Gest the publisher URL depending if this is a multi-tenant editor or not.
        /// </summary>
        /// <returns>Publisher URL.</returns>
        private string GetPublisherUrl()
        {
            var urlRoot = string.Empty;
            if (this.CosmosOptions.Value.SiteSettings.MultiTenantEditor)
            {
                urlRoot = settings.GetEditorConfig().PublisherUrl.TrimEnd('/') + "/";
            }
            else
            {
                urlRoot = this.CosmosOptions.Value.SiteSettings.PublisherUrl.TrimEnd('/') + "/";
            }

            return urlRoot;
        }

        /// <summary>
        ///     Makes an article the new home page.
        /// </summary>
        /// <param name="model">New home page post model.</param>
        /// <param name="userId">ID of user creating the page.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public async Task CreateHomePage(NewHomeViewModel model, string userId)
        {
            // Remove the old page from home
            var oldHomeArticle = await DbContext.Articles.Where(w => w.UrlPath.ToLower() == "root").ToListAsync();

            if (oldHomeArticle.Count == 0)
            {
                throw new ArgumentException("No existing home page found.");
            }

            // New page that will become page root
            var newHomeArticle = await DbContext.Articles.Where(w => w.ArticleNumber == model.ArticleNumber).ToListAsync();

            if (newHomeArticle.Count == 0)
            {
                throw new ArgumentException("New home page not found.");
            }

            // Change the path of the old home page (no longer 'root').
            var newUrl = NormailizeArticleUrl(oldHomeArticle.FirstOrDefault()?.Title);
            foreach (var article in oldHomeArticle)
            {
                article.UrlPath = newUrl;
            }

            await DbContext.SaveChangesAsync();

            // Publish the old home page as a regular page (also update catalog entry).
            await PublishArticle(oldHomeArticle.OrderBy(o => o.VersionNumber).LastOrDefault(f => f.Published.HasValue));

            foreach (var article in newHomeArticle)
            {
                article.UrlPath = "root";
            }

            await DbContext.SaveChangesAsync();

            var published = newHomeArticle.OrderBy(o => o.VersionNumber).LastOrDefault(f => f.Published.HasValue);

            // Publish the new home page as a regular page (also update catalog entry).
            await PublishArticle(published);
        }

        /// <summary>
        ///     This method puts an article into trash, and, all its versions.
        /// </summary>
        /// <param name="articleNumber">Article number.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        /// <remarks>
        ///     <para>This method puts an article into trash. Use <see cref="RestoreArticle" /> to restore an article. </para>
        ///     <para>It also removes it from the page catalog and any published pages..</para>
        ///     <para>WARNING: Make sure the menu MenuController.Index does not reference deleted files.</para>
        /// </remarks>
        public async Task DeleteArticle(int articleNumber)
        {
            var doomed = await DbContext.Articles.Where(w => w.ArticleNumber == articleNumber).ToListAsync();
            var url = doomed.FirstOrDefault()?.UrlPath;

            if (doomed == null)
            {
                throw new KeyNotFoundException($"Article number {articleNumber} not found.");
            }

            if (doomed.Exists(a => a.UrlPath.ToLower() == "root"))
            {
                throw new NotSupportedException(
                    "Cannot trash the home page.  Replace home page with another, then send to trash.");
            }

            foreach (var article in doomed)
            {
                article.StatusCode = (int)StatusCodeEnum.Deleted;
            }

            var doomedPages = await DbContext.Pages.Where(w => w.ArticleNumber == articleNumber).ToListAsync();
            DbContext.Pages.RemoveRange(doomedPages);

            await DbContext.SaveChangesAsync();
            await DeleteCatalogEntry(articleNumber);
            DeleteStaticWebpage(url);
            await CreateStaticTableOfContentsJsonFile();
        }

        /// <summary>
        ///     Retrieves and article and all its versions from trash.
        /// </summary>
        /// <param name="articleNumber">Article number.</param>
        /// <param name="userId">Current user ID.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        /// <remarks>
        ///     <para>
        ///         Please be aware of the following:
        ///     </para>
        ///     <list type="bullet">
        ///         <item><see cref="Article.StatusCode" /> is set to <see cref="StatusCodeEnum.Active" />.</item>
        ///         <item><see cref="Article.Title" /> will be altered if a live article exists with the same title.</item>
        ///         <item>
        ///             If the title changed, the <see cref="Article.UrlPath" /> will be updated using
        ///             <see cref="NormailizeArticleUrl" />.
        ///         </item>
        ///         <item>The article and all its versions are set to unpublished (<see cref="Article.Published" /> set to null).</item>
        ///         <item>Article is added back to the article catalog.</item>
        ///     </list>
        /// </remarks>
        public async Task RestoreArticle(int articleNumber, string userId)
        {
            var redeemed = await DbContext.Articles.Where(w => w.ArticleNumber == articleNumber).ToListAsync();

            if (redeemed == null || redeemed.Count == 0)
            {
                throw new KeyNotFoundException($"Article number {articleNumber} not found.");
            }

            var title = redeemed.FirstOrDefault()?.Title.ToLower();

            // Avoid restoring an article that has a title that collides with a live article.
            if (await DbContext.Articles.Where(a =>
                a.Title.ToLower() == title && a.ArticleNumber != articleNumber &&
                a.StatusCode == (int)StatusCodeEnum.Deleted).CosmosAnyAsync())
            {
                var newTitle = title + " (" + await DbContext.Articles.CountAsync() + ")";
                var url = NormailizeArticleUrl(newTitle);
                foreach (var article in redeemed)
                {
                    article.Title = newTitle;
                    article.UrlPath = url;
                    article.StatusCode = (int)StatusCodeEnum.Active;
                    article.Published = null;
                }
            }
            else
            {
                foreach (var article in redeemed)
                {
                    article.StatusCode = (int)StatusCodeEnum.Active;
                    article.Published = null;
                }
            }

            // Add back to the catalog
            var sample = redeemed.FirstOrDefault();
            DbContext.ArticleCatalog.Add(new CatalogEntry()
            {
                ArticleNumber = sample.ArticleNumber,
                Published = null,
                Status = "Active",
                Title = sample.Title,
                Updated = DateTimeOffset.Now,
                UrlPath = sample.UrlPath
            });

            await DbContext.SaveChangesAsync();
        }

        /// <summary>
        ///     Updates an existing article, or inserts a new one.
        /// </summary>
        /// <param name="model">Article view model.</param>
        /// <param name="userId">ID of the current user.</param>
        /// <remarks>
        ///     <para>
        ///         If the article number is '0', a new article is inserted.  If a version number is '0', then
        ///         a new version is created. Recreates <see cref="ArticleViewModel" /> using method
        ///         <see cref="ArticleLogic.BuildArticleViewModel(Article, string, bool)" />.
        ///     </para>
        ///     <list type="bullet">
        ///         <item>
        ///             Published articles will trigger the prior published article to have its Expired property set to this
        ///             article's published property.
        ///         </item>
        ///         <item>
        ///             Title changes (and redirects) are handled by adding a new article with redirect info.
        ///         </item>
        ///         <item>
        ///             The <see cref="ArticleViewModel" /> that is returned, is rebuilt using
        ///             <see cref="ArticleLogic.BuildArticleViewModel(Article, string, bool)" />.
        ///         </item>
        ///         <item>
        ///            <see cref="Article.Updated"/> property is automatically updated with current UTC date and time.
        ///         </item>
        ///     </list>
        /// </remarks>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public async Task<ArticleUpdateResult> SaveArticle(ArticleViewModel model, string userId)
        {
            // Retrieve the article that we will be using.
            // This will either be used to create a new version (detached then added as new),
            // or updated in place.
            var article = await DbContext.Articles.OrderByDescending(o => o.VersionNumber).FirstOrDefaultAsync(a => a.ArticleNumber == model.ArticleNumber);

            if (article == null)
            {
                throw new NotFoundException($"Article ID: {model.Id} not found.");
            }

            // Keep track of the old title--used below if title has changed.
            string oldTitle = article.Title;

            // A change in publish status triggers a new version.
            var isNewVersion = article.Published != model.Published;

            // =======================================================
            // BEGIN: MAKE CONTENT CHANGES HERE
            // =======================================================
            model.Content = Ensure_ContentEditable_IsMarked(model.Content);

            //Ensure_Oembed_Handled(model);

            // Make sure base tag is set properly.
            UpdateHeadBaseTag(model);

            // Minify the HTML content.
            article.Content = model.Content;
            article.Published = model.Published;
            article.Title = model.Title;
            article.Updated = DateTimeOffset.UtcNow;
            article.HeaderJavaScript = model.HeadJavaScript;
            article.FooterJavaScript = model.FooterJavaScript;
            article.BannerImage = model.BannerImage;

            // =======================================================
            // END: MAKE MODEL CHANGES HERE
            // =======================================================
            UpdateHeadBaseTag(article);

            if (isNewVersion)
            {
                DbContext.Entry(article).State = EntityState.Detached;

                // Ensure a user ID is set for creator
                article.UserId = userId;

                // New version, gets a new ID.
                article.Id = Guid.NewGuid();
                article.VersionNumber = article.VersionNumber + 1;
                model.VersionNumber = article.VersionNumber;
                DbContext.Articles.Add(article);
            }

            // Make sure this saves now
            await DbContext.SaveChangesAsync();

            // If we are publishing this to a static website, write it
            // to the blob storage now.
            if (CosmosOptions.Value.UseStaticPublisherWebsite)
            {
                // TODO: Write to blob storage
            }

            // IMPORTANT!
            // Handle title (and URL) changes for existing 
            await SaveTitleChange(article, oldTitle);

            // Update the catalog entry -- happens before publishing.
            await CreateCatalogEntry(article);

            // HANDLE PUBLISHING OF AN ARTICLE
            // This can be a new or existing article.
            var results = await PublishArticle(article);

            var result = new ArticleUpdateResult
            {
                ServerSideSuccess = true,
                Model = model,
                CdnResults = results
            };

            return result;
        }

        /// <summary>
        /// Make sure all content editble DIVs have a unique C/CMS ID (attribute 'data-ccms-ceid'), removes CK editor classes.
        /// </summary>
        /// <param name="content">HTML content to check.</param>
        /// <returns>string.</returns>
        /// <remarks>
        /// <para>
        /// The WYSIWYG editor is designed to only edit portions of an article content that are marked 
        /// with the attribute "contenteditable='true'".
        /// </para>
        /// <para>
        /// When an article is saved by the WYSIWYG editor only those portions within the DIV tags
        /// marked editable are saved.
        /// </para>
        /// <para>
        /// This allows editing of a web page with dynamic client-side functionality (JavaScript)
        /// like a map, chart, graph, etc. to be uneditable on a page while the text around it is.
        /// </para>
        /// </remarks>
        public string Ensure_ContentEditable_IsMarked(string content)
        {
            if (string.IsNullOrEmpty(content) || string.IsNullOrWhiteSpace(content))
            {
                return content;
            }

            var htmlDoc = new HtmlAgilityPack.HtmlDocument();
            htmlDoc.LoadHtml(content);

            var elements = htmlDoc.DocumentNode.SelectNodes("//*[@contenteditable='true']|//*[@contenteditable='']|//*[@data-ccms-new]|//*[@data-ccms-ceid]");

            if (elements == null)
            {
                return content;
            }

            var editables = new string[] { "div", "h1", "h2", "h3", "h4", "h5" };

            foreach (var element in elements)
            {
                if (editables.Contains(element.Name.ToLower()))
                {
                    if (element.Attributes.Contains("data-ccms-ceid"))
                    {
                        if (string.IsNullOrEmpty(element.Attributes["data-ccms-ceid"].Value))
                        {
                            element.Attributes["data-ccms-ceid"].Value = Guid.NewGuid().ToString();
                        }
                    }
                    else
                    {
                        // If the data-ccms-ceid attribute is missing, add it here.
                        element.Attributes.Add("data-ccms-ceid", Guid.NewGuid().ToString());
                    }

                    if (element.Attributes.Contains("contenteditable"))
                    {
                        element.Attributes.Remove("contenteditable");
                    }

                    if (element.Attributes.Contains("data-ccms-new"))
                    {
                        element.Attributes.Remove("data-ccms-new");
                    }
                }
                else
                {
                    if (element.Attributes.Contains("contenteditable"))
                    {
                        element.Attributes.Remove("contenteditable");
                    }
                }

                // Remove CK Editor classes
                if (element.HasClass("ck"))
                {
                    element.RemoveClass("ck");
                }

                // This class must be present on all CKEditor blocks
                if (element.Name.ToLower() == "div" && !element.HasClass("ck-content"))
                {
                    element.AddClass("ck-content");
                }

                if (element.HasClass("ck-editor__editable"))
                {
                    element.RemoveClass("ck-editor__editable");
                }

                if (element.HasClass("ck-rounded-corners"))
                {
                    element.RemoveClass("ck-rounded-corners");
                }

                if (element.HasClass("ck-editor__editable_inline"))
                {
                    element.RemoveClass("ck-editor__editable_inline");
                }

                if (element.HasClass("ck-focused"))
                {
                    element.RemoveClass("ck-focused");
                }

                // lang="en" dir="ltr" role="textbox" aria-label="Rich Text Editor. Editing area: main"
                if (element.HasAttributes)
                {
                    element.Attributes.Remove("role");
                    if (element.Attributes.Contains("lang"))
                    {
                        element.Attributes.Remove("lang");
                    }

                    if (element.Attributes.Contains("dir"))
                    {
                        element.Attributes.Remove("dir");
                    }

                    if (element.Attributes.Contains("role") && element.Attributes["role"].Value.ToLower() == "textbox")
                    {
                        element.Attributes.Remove("textbox");
                    }

                    if (element.Attributes.Contains("aria-label") && element.Attributes["aria-label"].Value == "Rich Text Editor. Editing area: main")
                    {
                        element.Attributes.Remove("aria-label");
                    }
                }
            }

            // Detect duplicate editable 
            if (elements.Count > 0)
            {
                var groups = elements.GroupBy(x => x.Attributes["data-ccms-ceid"].Value).Where(g => g.Count() > 1).ToList();

                if (!groups.Any())
                {
                    return htmlDoc.DocumentNode.OuterHtml;
                }

                var dups = elements.GroupBy(x => x.Attributes["data-ccms-ceid"].Value).Where(g => g.Count() > 1).Select(y => new { id = y.Key, Counter = y.Count() })
                  .ToList();

                if (dups.Any())
                {
                    var ids = dups.Select(s => s.id).ToList();

                    var duplicates = elements.Where(w => ids.Contains(w.Attributes["data-ccms-ceid"].Value));

                    foreach (var duplicate in duplicates)
                    {
                        duplicate.Attributes["data-ccms-ceid"].Value = Guid.NewGuid().ToString();
                    }
                }
            }

            // If we had to add at least one ID, then re-save the article.
            return htmlDoc.DocumentNode.OuterHtml;
        }

        /// <summary>
        /// Unpublishes an article.
        /// </summary>
        /// <param name="articleNumber">Article nymber.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public async Task UnpublishArticle(int articleNumber)
        {
            var versions = await DbContext.Articles.Where(w => w.ArticleNumber == articleNumber && w.Published != null).ToListAsync();

            if (versions.Exists(a => a.UrlPath.Equals("root", StringComparison.InvariantCultureIgnoreCase)))
            {
                // Cannot unpublish a home page
                return;
            }

            foreach (var version in versions)
            {
                version.Published = null;
            }

            await DbContext.SaveChangesAsync();

            var catalog = await DbContext.ArticleCatalog.Where(w => w.ArticleNumber == articleNumber).ToListAsync();
            foreach (var item in catalog)
            {
                item.Published = null;
            }

            await DbContext.SaveChangesAsync();

            var pages = await DbContext.Pages.Where(w => w.ArticleNumber == articleNumber).ToListAsync();
            DbContext.Pages.RemoveRange(pages);

            DeleteStaticWebpage(pages.FirstOrDefault().UrlPath);
            await CreateStaticTableOfContentsJsonFile();

            await DbContext.SaveChangesAsync();
        }

        /// <summary>
        /// Update head tag to match path. 
        /// </summary>
        /// <param name="model">Article view model.</param>
        /// <remarks>
        /// Angular uses the BASE tag within the HEAD to set relative path to article/app.
        /// If that tag is detected, it is updated automatically to match the current <see cref="Article.UrlPath"/>.
        /// </remarks>
        public void UpdateHeadBaseTag(ArticleViewModel model)
        {
            if (!string.IsNullOrEmpty(model.HeadJavaScript) && model.HeadJavaScript.Contains("<base "))
            {
                model.HeadJavaScript = UpdateHeadBaseTag(model.HeadJavaScript, model.UrlPath);
            }
        }

        /// <summary>
        /// Update head tag to match path. 
        /// </summary>
        /// <param name="model">Article view model.</param>
        /// <remarks>
        /// Angular uses the BASE tag within the HEAD to set relative path to article/app.
        /// If that tag is detected, it is updated automatically to match the current <see cref="Article.UrlPath"/>.
        /// </remarks>
        public void UpdateHeadBaseTag(Article model)
        {
            if (!string.IsNullOrEmpty(model.HeaderJavaScript) && (model.HeaderJavaScript.Contains("<base ") || model.HeaderJavaScript.ToLower().Contains("ccms:framework") && model.HeaderJavaScript.ToLower().Contains("angular")))
            {
                model.HeaderJavaScript = UpdateHeadBaseTag(model.HeaderJavaScript, model.UrlPath);
            }
        }

        /// <summary>
        ///     Gets a copy of the article ready for edit.
        /// </summary>
        /// <param name="articleNumber">Article Number.</param>
        /// <param name="versionNumber">Version to edit, or if null gets latest version.</param>
        /// <returns>
        ///     <see cref="ArticleViewModel" />.
        /// </returns>
        /// <remarks>
        ///     <para>
        ///         Returns <see cref="ArticleViewModel" />. For more details on what is returned, see
        ///         <see cref="ArticleLogic.BuildArticleViewModel(Article, string, bool)" />.
        ///     </para>
        ///     <para>NOTE: Cannot access articles that have been deleted.</para>
        /// </remarks>
        public async Task<ArticleViewModel> GetArticleByArticleNumber(int articleNumber, int? versionNumber)
        {
            Article article;

            if (versionNumber.HasValue)
            {
                // Get a specific version
                article = await DbContext.Articles
                .FirstOrDefaultAsync(
                    a => a.ArticleNumber == articleNumber &&
                         a.VersionNumber == versionNumber);
            }
            else
            {
                // Get the latest version
                article = await DbContext.Articles.OrderByDescending(v => v.VersionNumber)
                .FirstOrDefaultAsync(
                    a => a.ArticleNumber == articleNumber);
            }

            if (article == null)
            {
                throw new ArgumentException($"Article number:{articleNumber}, Version:{versionNumber}, not found.");
            }

            return await BuildArticleViewModel(article, "en-US");
        }

        /// <summary>
        ///     Gets an article by ID (row Key), or creates a new (unsaved) article if id is null.
        /// </summary>
        /// <param name="id">Row Id (or identity) number.  If null returns a new article.</param>
        /// <param name="controllerName">Controller name enum.</param>
        /// <param name="userId">User ID.</param>
        /// <remarks>
        ///     <para>
        ///         For new articles, uses <see cref="CreateArticle" /> and the method
        ///         ArticleLogic.BuildArticleViewModel to
        ///         generate the <see cref="ArticleViewModel" /> .
        ///     </para>
        ///     <para>
        ///         Retrieves <see cref="Article" /> and builds an <see cref="ArticleViewModel" /> using the method
        ///         ArticleLogic.BuildArticleViewModel,
        ///         or in the case of a template, uses method <see cref="CreateTemplateViewModel" />.
        ///     </para>
        ///     <para>
        ///         Makes sure editable areas are properly marked with <see cref="Ensure_ContentEditable_IsMarked(string)"/>.
        ///     </para>
        /// </remarks>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        /// <remarks>
        ///     <para>
        ///         Returns <see cref="ArticleViewModel" />. For more details on what is returned, see
        ///         ArticleLogic.BuildArticleViewModel or <see cref="CreateTemplateViewModel" />.
        ///     </para>
        ///     <para>NOTE: Cannot access articles that have been deleted.</para>
        /// </remarks>
        public async Task<ArticleViewModel> GetArticleById(Guid? id, EnumControllerName controllerName, string userId)
        {
            if (controllerName == EnumControllerName.Template)
            {
                if (id == null)
                {
                    throw new ArgumentException("Template ID:null not found.");
                }

                var idNo = id.Value;
                var template = await DbContext.Templates.FindAsync(idNo);

                if (template == null)
                {
                    throw new ArgumentException($"Template ID:{id} not found.");
                }

                return CreateTemplateViewModel(template);
            }

            // This is used to create a "blank" page just so we have something to get started with.
            if (id == null)
            {
                var count = await DbContext.Articles.CountAsync();
                return await CreateArticle("Page " + count, userId);
            }

            var article = await DbContext.Articles
                .FirstOrDefaultAsync(a => a.Id == id && a.StatusCode != 2);

            if (article == null)
            {
                throw new ArgumentException($"Article ID:{id} not found.");
            }

            article.Content = Ensure_ContentEditable_IsMarked(article.Content);

            return await BuildArticleViewModel(article, "en-US");
        }

        /// <summary>
        /// Gets a page, and allows unpublished or inactive pages to be returned.
        /// </summary>
        /// <param name="urlPath">URL path.</param>
        /// <param name="lang">Language code.</param>
        /// <param name="publishedOnly">Only published articles.</param>
        /// <param name="onlyActive">Only active articles.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public async Task<ArticleViewModel> GetArticleByUrl(string urlPath, string lang = "", bool publishedOnly = true, bool onlyActive = true)
        {
            if (publishedOnly && onlyActive)
            {
                return await GetPublishedPageByUrl(urlPath, lang);
            }

            var activeStatusCodes =
                onlyActive ? new[] { 0, 3 } : new[] { 0, 1, 3 }; // i.e. StatusCode.Active (DEFAULT) and StatusCode.Redirect

            urlPath = urlPath?.ToLower().Trim(' ', '/');

            if (string.IsNullOrEmpty(urlPath) || urlPath == "root")
            {
                urlPath = string.Empty;
            }

            if (publishedOnly)
            {
                var article = await DbContext.Pages.WithPartitionKey(urlPath)
                    .Where(a => a.Published <= DateTimeOffset.UtcNow &&
                                activeStatusCodes.Contains(a.StatusCode)) // Now filter on active status code.
                    .OrderByDescending(o => o.VersionNumber).FirstOrDefaultAsync();
                return await BuildArticleViewModel(article, lang, null);
            }
            else
            {
                // Will search unpublished and published articles
                var article = await DbContext.Articles
                    .Where(a => a.UrlPath == urlPath)
                    .OrderByDescending(o => o.VersionNumber)
                    .FirstOrDefaultAsync();
                return await BuildArticleViewModel(article, lang);
            }
        }

        /// <summary>
        /// Get a list of article redirects.
        /// </summary>
        /// <returns>RedirectItemViewModel.</returns>
        public IQueryable<RedirectItemViewModel> GetArticleRedirects()
        {
            var redirectCode = (int)StatusCodeEnum.Redirect;
            var query = DbContext.Articles.Where(w => w.StatusCode == redirectCode);

            return query.Select(s => new RedirectItemViewModel()
            {
                FromUrl = s.UrlPath,
                Id = s.Id,
                ToUrl = s.Content
            });
        }

        /// <summary>
        /// Gets the last published date.
        /// </summary>
        /// <param name="articleNumber">articleNumber.</param>
        /// <returns>Last published date and time.</returns>
        public async Task<DateTimeOffset?> GetLastPublishedDate(int articleNumber)
        {
            return await DbContext.Articles
                .Where(w => w.Published != null && w.ArticleNumber == articleNumber)
                .OrderByDescending(o => o.VersionNumber)
                .Select(s => s.Published)
                .LastOrDefaultAsync();
        }

        /// <summary>
        /// Exports and article as HTML with layout elements.
        /// </summary>
        /// <param name="article">Article view model.</param>
        /// <param name="blobPublicAbsoluteUrl">Blob absolute URL.</param>
        /// <param name="viewRenderService">View render service.</param>
        /// <returns>web page.</returns>
        public async Task<string> ExportArticle(ArticleViewModel article, Uri blobPublicAbsoluteUrl, IViewRenderService viewRenderService)
        {
            var htmlUtilities = new HtmlUtilities();

            article.Layout.Head = htmlUtilities.RelativeToAbsoluteUrls(article.Layout.Head, blobPublicAbsoluteUrl, false);

            // Layout body elements
            article.Layout.HtmlHeader = htmlUtilities.RelativeToAbsoluteUrls(article.Layout.HtmlHeader, blobPublicAbsoluteUrl, true);
            article.Layout.FooterHtmlContent = htmlUtilities.RelativeToAbsoluteUrls(article.Layout.FooterHtmlContent, blobPublicAbsoluteUrl, true);

            article.HeadJavaScript = htmlUtilities.RelativeToAbsoluteUrls(article.HeadJavaScript, blobPublicAbsoluteUrl, false);
            article.Content = htmlUtilities.RelativeToAbsoluteUrls(article.Content, blobPublicAbsoluteUrl, false);
            article.FooterJavaScript = htmlUtilities.RelativeToAbsoluteUrls(article.FooterJavaScript, blobPublicAbsoluteUrl, false);

            var html = await viewRenderService.RenderToStringAsync("~/Views/Editor/ExportPage.cshtml", article);

            return html;
        }

        /// <summary>
        /// Get a list of reserved paths.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public async Task<List<ReservedPath>> GetReservedPaths()
        {
            var setting = await DbContext.Settings.FirstOrDefaultAsync(f => f.Name == "ReservedPaths");

            List<ReservedPath> paths;
            if (setting == null)
            {
                // Create and save default list of reserved paths
                paths = new List<ReservedPath>();
                paths.Add(new ReservedPath()
                {
                    CosmosRequired = true,
                    Notes = "Path used by storage to hold folders and files",
                    Path = "pub*"
                });
                paths.Add(new ReservedPath()
                {
                    CosmosRequired = true,
                    Notes = "Path used by Google language translator",
                    Path = "GetSupportedLanguages"
                });
                paths.Add(new ReservedPath()
                {
                    CosmosRequired = true,
                    Notes = "Path used to get a page's table of contents",
                    Path = "GetTOC"
                });
                paths.Add(new ReservedPath()
                {
                    CosmosRequired = true,
                    Notes = "Path used when an admin needs to grant access to a new user",
                    Path = "AccessPending"
                });
                paths.Add(new ReservedPath()
                {
                    CosmosRequired = true,
                    Notes = "Path by MS Azure to validate ownership of a site for OAuth.",
                    Path = "GetMicrosoftIdentityAssociation"
                });
                paths.Add(new ReservedPath()
                {
                    CosmosRequired = true,
                    Notes = "Used to display an error.",
                    Path = "Error"
                });
                paths.Add(new ReservedPath()
                {
                    CosmosRequired = true,
                    Notes = "Editor user account path.",
                    Path = "Account*"
                });
                paths.Add(new ReservedPath()
                {
                    CosmosRequired = true,
                    Notes = "Code controller path.",
                    Path = "Code*"
                });
                paths.Add(new ReservedPath()
                {
                    CosmosRequired = true,
                    Notes = "Editor controller path.",
                    Path = "Editor*"
                });
                paths.Add(new ReservedPath()
                {
                    CosmosRequired = true,
                    Notes = "File manager controller path.",
                    Path = "FileManager*"
                });
                paths.Add(new ReservedPath()
                {
                    CosmosRequired = true,
                    Notes = "Layouts controller path.",
                    Path = "Layouts*"
                });
                paths.Add(new ReservedPath()
                {
                    CosmosRequired = true,
                    Notes = "Roles controller path.",
                    Path = "Roles*"
                });
                paths.Add(new ReservedPath()
                {
                    CosmosRequired = true,
                    Notes = "Home controller path.",
                    Path = "Home*"
                });
                paths.Add(new ReservedPath()
                {
                    CosmosRequired = true,
                    Notes = "Templates controller path.",
                    Path = "Templates*"
                });
                paths.Add(new ReservedPath()
                {
                    CosmosRequired = true,
                    Notes = "Users controller path.",
                    Path = "Users*"
                });
                paths.Add(new ReservedPath()
                {
                    CosmosRequired = true,
                    Notes = "Cosmos CDN controller path.",
                    Path = "Cosmos__Admin_Cdn*"
                });

                setting = new Setting()
                {
                    Value = JsonConvert.SerializeObject(paths),
                    Group = "Editing",
                    Description = "List of reserved paths used to deconflict page name conflicts",
                    IsRequired = true,
                    Name = "ReservedPaths"
                };

                DbContext.Settings.Add(setting);
                await DbContext.SaveChangesAsync();
            }
            else
            {
                paths = JsonConvert.DeserializeObject<List<ReservedPath>>(setting.Value);
            }

            return paths;
        }

        /// <summary>
        /// Create or update the reserved path list.
        /// </summary>
        /// <param name="model">Reserved path model.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        /// <exception cref="Exception">Cannot add path because it is already reserved.</exception>
        public async Task SaveReservedPath(ReservedPath model)
        {
            if (model.CosmosRequired)
            {
                throw new ArgumentException($"Reserved path {model.Path} cannot be modified.");
            }

            model.Path = model.Path.ToLower().Replace("\\", "/").Trim('/').TrimStart('*');

            var paths = await GetReservedPaths();

            // check for update
            var entity = paths.Find(f => f.Id == model.Id);

            if (entity == null)
            {
                if (paths.Exists(a => a.Path.ToLower() == model.Path.ToLower()))
                {
                    throw new ArgumentException($"Cannot add path '{model.Path}' because it is already reserved.");
                }

                paths.Add(model);
            }
            else
            {
                if (paths.Exists(a => a.Path.ToLower() == model.Path.ToLower() && a.Id != model.Id))
                {
                    throw new ArgumentException($"Cannot rename to path '{model.Path}' because it is already reserved.");
                }

                entity.Notes = model.Notes;
                entity.Path = model.Path;
            }

            var setting = await DbContext.Settings.FirstOrDefaultAsync(f => f.Name == "ReservedPaths");
            setting.Value = JsonConvert.SerializeObject(paths);
            await DbContext.SaveChangesAsync();
        }

        /// <summary>
        /// Creates a pre-rendered static web page and saves it in blob storage.
        /// </summary>
        /// <param name="page">PublishedPage.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        /// <remarks>Only works if the environment variable 'CosmosStaticWebPages' is set to 'true'.</remarks>
        public async Task CreateStaticWebpage(PublishedPage page)
        {
            if (CosmosOptions.Value.SiteSettings.StaticWebPages)
            {
                if (page.UrlPath.StartsWith("/pub", StringComparison.OrdinalIgnoreCase))
                {
                    throw new Exception("Cannot publish web page to path /pub.");
                }

                string html = string.Empty;
                if (page.StatusCode == (int)StatusCodeEnum.Redirect)
                {
                    var model = new RedirectItemViewModel()
                    {
                        FromUrl = page.UrlPath,
                        ToUrl = page.Content
                    };
                    html = await viewRenderService.RenderToStringAsync("~/Views/Home/Redirect.cshtml", model);
                }
                else
                {
                    var model = await BuildArticleViewModel(page, string.Empty);
                    html = await viewRenderService.RenderToStringAsync("~/Views/Home/Export.cshtml", model);
                }

                // Compress HTML
                //var compressed = Uglify.Html(html).Code;
                //if (!string.IsNullOrEmpty(compressed))
                //{
                //    html = compressed;
                //}

                using var stream = new MemoryStream(Encoding.UTF8.GetBytes(html));

                if (string.IsNullOrEmpty(page.UrlPath))
                {
                    page.UrlPath = "root";
                }

                var filePath = page.UrlPath.Equals("root", StringComparison.OrdinalIgnoreCase) ? "/index.html" : page.UrlPath;

                storageContext.AppendBlob(stream, new BlobService.Models.FileUploadMetaData()
                {
                    ChunkIndex = 0,
                    ContentType = "text/html",
                    FileName = Path.GetFileName(filePath).ToLower(),
                    ImageHeight = string.Empty,
                    ImageWidth = string.Empty,
                    RelativePath = filePath.ToLower(),
                    TotalChunks = 1,
                    TotalFileSize = stream.Length,
                    UploadUid = Guid.NewGuid().ToString(),
                }, "block");
            }
        }

        /// <summary>
        /// Removes a reserved path.
        /// </summary>
        /// <param name="id">Path ID.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        /// <exception cref="Exception">Path not found.</exception>
        public async Task DeleteReservedPath(Guid id)
        {
            var paths = await GetReservedPaths();

            var doomed = paths.Find(a => a.Id == id);

            if (doomed == null)
            {
                throw new ArgumentException($"Path Id '{id}' not found, could not delete.");
            }

            if (doomed.CosmosRequired)
            {
                throw new ArgumentException($"Reserved path {doomed.Path} cannot be deleted.");
            }

            paths.Remove(doomed);

            var setting = await DbContext.Settings.FirstOrDefaultAsync(f => f.Name == "ReservedPaths");
            setting.Value = JsonConvert.SerializeObject(paths);
            await DbContext.SaveChangesAsync();
        }

        /// <summary>
        /// Creates a site map file in the blob storage.
        /// </summary>
        /// <param name="filePath">Path and file name.</param>
        /// <returns>Task.</returns>
        public async Task CreateSiteMapFile(string filePath = "/pub/sitemap.xml")
        {
            var siteMap = await GetSiteMap();
            using (var stream = new MemoryStream(Encoding.UTF8.GetBytes(siteMap.ToXml())))
            {
                var storagePath = $"/{filePath}";

                storageContext.AppendBlob(stream, new BlobService.Models.FileUploadMetaData()
                {
                    ChunkIndex = 0,
                    ContentType = "application/xml",
                    FileName = Path.GetFileName(storagePath).ToLower(),
                    ImageHeight = string.Empty,
                    ImageWidth = string.Empty,
                    RelativePath = storagePath,
                    TotalChunks = 1,
                    TotalFileSize = stream.Length,
                    UploadUid = Guid.NewGuid().ToString(),
                    CacheControl = "max-age=300;must-revalidate"
                }, "block");
            }

            var robotsTxtFile = $"Sitemap: {GetPublisherUrl().TrimEnd('/')}/pub/sitemap.xml";
            using (var stream = new MemoryStream(Encoding.UTF8.GetBytes(robotsTxtFile)))
            {
                var storagePath = "/robots.txt";

                storageContext.AppendBlob(stream, new BlobService.Models.FileUploadMetaData()
                {
                    ChunkIndex = 0,
                    ContentType = "text/plain",
                    FileName = Path.GetFileName(storagePath).ToLower(),
                    ImageHeight = string.Empty,
                    ImageWidth = string.Empty,
                    RelativePath = storagePath,
                    TotalChunks = 1,
                    TotalFileSize = stream.Length,
                    UploadUid = Guid.NewGuid().ToString(),
                    CacheControl = "max-age=300;must-revalidate"
                }, "block");
            }
        }

        /// <summary>
        /// Publishes the table of contents starting at the root and site map file.
        /// </summary>
        /// <param name="path">Path to start from.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public async Task CreateStaticTableOfContentsJsonFile(string path = "")
        {
            var tableOfContents = await GetTableOfContents(path, 0, 50, true);
            var json = JsonConvert.SerializeObject(tableOfContents);
            using var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));
            var storagePath = "pub/js/toc.json";

            var urlRoot = GetPublisherUrl();

            foreach (var item in tableOfContents.Items)
            {
                if (!string.IsNullOrEmpty(item.BannerImage) && !item.BannerImage.StartsWith("http"))
                {
                    item.BannerImage = urlRoot + item.BannerImage.TrimStart('/');
                }
            }

            storageContext.AppendBlob(stream, new BlobService.Models.FileUploadMetaData()
            {
                ChunkIndex = 0,
                ContentType = "application/json",
                FileName = Path.GetFileName(storagePath).ToLower(),
                ImageHeight = string.Empty,
                ImageWidth = string.Empty,
                RelativePath = storagePath,
                TotalChunks = 1,
                TotalFileSize = stream.Length,
                UploadUid = Guid.NewGuid().ToString(),
                CacheControl = "max-age=300;must-revalidate"
            }, "block");

            await CreateSiteMapFile();
        }

        /// <summary>
        ///   Makes sure the article catalog isn't missing anything.
        /// </summary>
        /// <returns>Task.</returns>
        public async Task CheckCatalogEntries()
        {
            var articleNumbers = await DbContext.Pages.Select(s => s.ArticleNumber).Distinct().ToListAsync();
            var catalogArticleNumbers = await DbContext.ArticleCatalog.Select(s => s.ArticleNumber).Distinct().ToListAsync();

            var missing = catalogArticleNumbers.Except(catalogArticleNumbers).ToList();

            foreach (var articleNumber in missing)
            {
                var last = await DbContext.Articles.Where(w => w.ArticleNumber == articleNumber && w.Published != null).OrderBy(o => o.VersionNumber).LastOrDefaultAsync();
                await CreateCatalogEntry(last);
            }
        }

        /// <summary>
        ///     Provides a standard method for turning a title into a URL Encoded path.
        /// </summary>
        /// <param name="title">Title to be converted into a URL.</param>
        /// <remarks>
        ///     <para>This is accomplished using <see cref="HttpUtility.UrlEncode(string)" />.</para>
        ///     <para>Blanks are turned into underscores (i.e. "_").</para>
        ///     <para>All strings are normalized to lower case.</para>
        /// </remarks>
        private static string NormailizeArticleUrl(string title)
        {
            return title.Trim().Replace(" ", "_").ToLower();
        }

        /// <summary>
        /// Logic handing logic for publishing articles and saves changes to the database.
        /// </summary>
        /// <param name="article">Article entity.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        /// <remarks>
        /// If article is published, it adds the correct versions to the public pages collection. If not, 
        /// the article is removed from the public pages collection. Also updates the catalog entry.
        /// </remarks>
        private async Task<List<CdnResult>> PublishArticle(Article article)
        {
            if (article.Published.HasValue)
            {
                // If the article is already published, then remove the other published versions.
                var others = await DbContext.Articles.Where(
                    w => w.ArticleNumber == article.ArticleNumber
                    && w.Published != null
                    && w.Id != article.Id).ToListAsync();

                var now = DateTimeOffset.Now;

                // If published in the future, then keep the last published article
                if (article.Published.Value > now)
                {
                    // Keep the article pulished just before this one
                    var oneTokeep = others.Where(
                        w => w.Published <= now // other published date is before the article
                        && w.VersionNumber < article.VersionNumber).OrderByDescending(o => o.VersionNumber).FirstOrDefault();

                    if (oneTokeep != null)
                    {
                        others.Remove(oneTokeep);
                    }

                    // Also keep the other articles that are published between now and before the current article
                    var othersToKeep = others.Where(
                        w => w.Published.Value > now // Save items published after now, and...
                        && w.Published.Value < article.Published.Value // published before the current article
                        && w.VersionNumber < article.VersionNumber) // and are a version number before this one.
                        .ToList();

                    foreach (var o in othersToKeep)
                    {
                        others.Remove(o);
                    }
                }

                // Now remove the other ones published
                foreach (var item in others)
                {
                    item.Published = null;
                }

                await DbContext.SaveChangesAsync();

                // Resets the expiration dates, based on the last published article
                await UpdateVersionExpirations(article.ArticleNumber);

                // Update the published pages collection
                return await UpsertPublishedPage(article.ArticleNumber);
            }

            // Make sure the catalog is up to date.
            await CheckCatalogEntries();

            return null;
        }

        /// <summary>
        /// Removes a static webpage from the blob storage if enabled.
        /// </summary>
        /// <param name="filePath">Path to page to remove.</param>
        private void DeleteStaticWebpage(string filePath)
        {
            if (CosmosOptions.Value.SiteSettings.StaticWebPages)
            {
                if (filePath.StartsWith("/pub", StringComparison.OrdinalIgnoreCase))
                {
                    throw new Exception("Cannot remove web page from path /pub.");
                }

                filePath = filePath.Equals("root", StringComparison.OrdinalIgnoreCase) ? "/index.html" : filePath;
                storageContext.DeleteFile(filePath);
            }
        }

        private async Task CreateStaticRedirectPage(string fromUrl, string toUrl)
        {
            if (CosmosOptions.Value.SiteSettings.StaticWebPages)
            {
                if (fromUrl.Equals(toUrl, StringComparison.OrdinalIgnoreCase))
                {
                    return;
                }

                fromUrl = fromUrl.Equals("root", StringComparison.OrdinalIgnoreCase) ? "/index.html" : fromUrl;
                toUrl = toUrl.Equals("root", StringComparison.OrdinalIgnoreCase) ? "/index.html" : toUrl;

                var model = new RedirectItemViewModel()
                {
                    FromUrl = fromUrl,
                    ToUrl = toUrl,
                    Id = Guid.NewGuid()
                };

                var html = await viewRenderService.RenderToStringAsync("~/Views/Home/Redirect.cshtml", model);
                using var stream = new MemoryStream(Encoding.UTF8.GetBytes(html));

                storageContext.AppendBlob(stream, new BlobService.Models.FileUploadMetaData()
                {
                    ChunkIndex = 0,
                    ContentType = "text/html",
                    FileName = Path.GetFileName(fromUrl),
                    ImageHeight = string.Empty,
                    ImageWidth = string.Empty,
                    RelativePath = fromUrl,
                    TotalChunks = 1,
                    TotalFileSize = stream.Length,
                    UploadUid = Guid.NewGuid().ToString(),
                });
            }
        }

        /// <summary>
        ///     Gets a template represented as an <see cref="ArticleViewModel" />.
        /// </summary>
        /// <param name="template">Page template model.</param>
        /// <returns>ArticleViewModel.</returns>
        private ArticleViewModel CreateTemplateViewModel(Template template)
        {
            var articleNumber = DbContext.Articles.Max(m => m.ArticleNumber) + 1;

            return new()
            {
                Id = template.Id,
                ArticleNumber = articleNumber,
                UrlPath = HttpUtility.UrlEncode(template.Title.Trim().Replace(" ", "_")),
                VersionNumber = 1,
                Published = DateTime.Now.ToUniversalTime(),
                Title = template.Title,
                Content = template.Content,
                Updated = DateTime.Now.ToUniversalTime(),
                HeadJavaScript = string.Empty,
                FooterJavaScript = string.Empty,
                ReadWriteMode = true
            };
        }

        /// <summary>
        /// Resets the expiration dates, based on the last published article, saves changes to the database.
        /// </summary>
        /// <param name="articleNumber">Article number.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        private async Task UpdateVersionExpirations(int articleNumber)
        {
            var list = await DbContext.Articles.Where(a => a.ArticleNumber == articleNumber).ToListAsync();

            foreach (var item in list)
            {
                if (item.Expires.HasValue)
                {
                    item.Expires = null;
                }
            }

            var published = list.Where(a => a.ArticleNumber == articleNumber && a.Published.HasValue)
                .OrderBy(o => o.VersionNumber).TakeLast(2).ToList();

            if (published.Count == 2)
            {
                published[0].Expires = published[1].Published;
            }

            await DbContext.SaveChangesAsync();
        }

        /// <summary>
        /// Creates a published page for a set of article versions, both in the database and in the blob storage (if enabled).
        /// </summary>
        /// <param name="articleNumber">Artice number to publish.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        private async Task<List<CdnResult>> UpsertPublishedPage(int articleNumber)
        {
            // Clean things up a bit.
            var doomed = await DbContext.Pages.Where(w => w.Content == "" || w.Title == "").ToListAsync();

            if (doomed.Any())
            {
                DbContext.Pages.RemoveRange(doomed);
                await DbContext.SaveChangesAsync();
            }

            // One or more versions of the article are going to be published.
            var newVersions = await DbContext.Articles.Where(
                w => w.ArticleNumber == articleNumber
                && w.Published != null).ToListAsync();

            // These published versions are going to be replaced--except for redirects.
            var publishedVersions = await DbContext.Pages.Where(
                w => w.ArticleNumber == articleNumber
                && w.StatusCode != (int)StatusCodeEnum.Redirect).ToListAsync();

            // This holds the new or updated page URLS.
            var purgePaths = new List<string>();
            purgePaths.AddRange(newVersions.Select(s => s.UrlPath));
            purgePaths.AddRange(publishedVersions.Select(s => s.UrlPath));


            if (publishedVersions.Count > 0)
            {
                // Mark these for deletion - do this first to avoid any conflicts
                foreach (var item in publishedVersions)
                {
                    DbContext.Pages.Remove(item);
                    await DbContext.SaveChangesAsync();
                    DeleteStaticWebpage(item.UrlPath);
                }
            }

            // Now refresh the published pages
            foreach (var item in newVersions)
            {
                var authorInfo = await DbContext.AuthorInfos.FirstOrDefaultAsync(f => f.Id == item.UserId && f.AuthorName != string.Empty);

                var newPage = new PublishedPage()
                {
                    ArticleNumber = item.ArticleNumber,
                    BannerImage = item.BannerImage,
                    Content = item.Content,
                    Expires = item.Expires,
                    FooterJavaScript = item.FooterJavaScript,
                    HeaderJavaScript = item.HeaderJavaScript,
                    Id = Guid.NewGuid(), // Use a new GUID
                    Published = item.Published,
                    StatusCode = item.StatusCode,
                    Title = item.Title,
                    Updated = item.Updated,
                    UrlPath = item.UrlPath,
                    ParentUrlPath = item.UrlPath.Substring(0, Math.Max(item.UrlPath.LastIndexOf('/'), 0)),
                    VersionNumber = item.VersionNumber,
                    AuthorInfo = JsonConvert.SerializeObject(authorInfo).Replace("\"", "'")
                };

                DbContext.Pages.Add(newPage);
                await DbContext.SaveChangesAsync();

                if (item.UrlPath != "root")
                {
                    purgePaths.Add($"/pub/articles/{item.ArticleNumber}/");
                }

                // Publish the static webpage that are published before now (add 5 min)
                if (newPage.Published.Value <= DateTimeOffset.Now.AddMinutes(5))
                {
                    await CreateStaticWebpage(newPage);
                }
            }

            // Update TOC file.
            await CreateStaticTableOfContentsJsonFile("/");

            if (purgePaths.Count > 0)
            {
                var settings = await Cosmos___SettingsController.GetCdnConfiguration(DbContext);
                var cdnService = new CdnService(settings, logger, accessor.HttpContext);
                try
                {
                    return await cdnService.PurgeCdn(purgePaths.Select(s => "/" + s.Trim('/')).Distinct().ToList());
                }
                catch (Exception ex)
                {
                    var d = ex.Message; // debugging purposes
                }
            }

            return null;
        }

        /// <summary>
        /// If the title has changed, handle that here.
        /// </summary>
        /// <param name="article">Article.</param>
        /// <param name="oldTitle">Old title.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        /// <remarks>
        /// Upon title change:
        /// <list type="bullet">
        /// <item>Updates title for article and it's versions</item>
        /// <item>Updates the article catalog</item>
        /// <item>Updates title of all child articles</item>
        /// <item>Creates an automatic redirect</item>
        /// <item>Updates base tags for all articles changed</item>
        /// <item>Saves changes to the database</item>
        /// </list>
        /// </remarks>
        private async Task SaveTitleChange(Article article, string oldTitle)
        {
            if (string.Equals(article.Title, oldTitle, StringComparison.CurrentCultureIgnoreCase) && !string.IsNullOrEmpty(article.UrlPath))
            {
                // Nothing to do
                return;
            }

            // Capture the new title
            var newTitle = article.Title;

            var articleNumbersToUpdate = new List<int>
            {
                article.ArticleNumber
            };

            // Validate that title is not already taken.
            if (!await ValidateTitle(newTitle, article.ArticleNumber))
            {
                throw new ArgumentException($"Title '{newTitle}' already taken");
            }

            var oldUrl = NormailizeArticleUrl(oldTitle);
            var newUrl = NormailizeArticleUrl(newTitle);

            // If NOT the root, handle any child page updates and redirects
            // that need to be created.
            if (article.UrlPath != "root")
            {
                // Update sub articles.
                var subArticles = await GetAllSubArticles(oldTitle);

                foreach (var subArticle in subArticles)
                {
                    if (!subArticle.Title.Equals("redirect", StringComparison.CurrentCultureIgnoreCase))
                    {
                        subArticle.Title = UpdatePrefix(oldTitle, newTitle, subArticle.Title);
                    }

                    subArticle.UrlPath = UpdatePrefix(oldUrl, newUrl, subArticle.UrlPath);

                    // Make sure base tag is set properly.
                    UpdateHeadBaseTag(subArticle);

                    articleNumbersToUpdate.Add(article.ArticleNumber);
                }

                DbContext.Articles.UpdateRange(subArticles);

                // Remove any conflicting redirects
                var conflictingRedirects = await DbContext.Articles.Where(a => a.Content == newUrl && a.Title.ToLower().Equals("redirect")).ToListAsync();

                if (conflictingRedirects.Any())
                {
                    DbContext.Articles.RemoveRange(conflictingRedirects);
                    articleNumbersToUpdate.AddRange(conflictingRedirects.Select(s => s.ArticleNumber).ToList());
                }

                // Update base href
                UpdateHeadBaseTag(article);

                // Add redirects if published
                if (article.Published.HasValue)
                {
                    // Create a redirect
                    var entity = new PublishedPage
                    {
                        ArticleNumber = 0,
                        StatusCode = (int)StatusCodeEnum.Redirect,
                        UrlPath = oldUrl, // Old URL
                        VersionNumber = 0,
                        Published = DateTime.Now.ToUniversalTime().AddDays(-1), // Make sure this sticks!
                        Title = "Redirect",
                        Content = newUrl, // New URL
                        Updated = DateTime.Now.ToUniversalTime(),
                        HeaderJavaScript = null,
                        FooterJavaScript = null
                    };

                    // Create a static redirect page.
                    await CreateStaticRedirectPage(oldUrl, newUrl);

                    // Add redirect here
                    DbContext.Pages.Add(entity);
                }

                await DbContext.SaveChangesAsync();
            }

            // We have to change the title and paths for all versions now, since the last publish.
            var lastPublished = await DbContext.Articles.OrderBy(o => o.Published)
                    .LastOrDefaultAsync(a => a.ArticleNumber == article.ArticleNumber);

            var versions = await DbContext.Articles.Where(w => w.ArticleNumber == article.ArticleNumber &&
                                article.Published > lastPublished.Published)
                                    .ToListAsync();

            if (versions.Count == 0)
            {
                // If there are no versions since the last publish, then we need to get all versions.
                versions = await DbContext.Articles.Where(w => w.ArticleNumber == article.ArticleNumber)
                                    .ToListAsync();
            }

            if (string.IsNullOrEmpty(article.UrlPath))
            {
                article.UrlPath = NormailizeArticleUrl(article.Title);
            }

            foreach (var art in versions)
            {
                // Update base href (for Angular apps)
                UpdateHeadBaseTag(article);

                art.Title = newTitle;
                art.Updated = DateTime.Now.ToUniversalTime();
                art.UrlPath = article.UrlPath;
            }

            DbContext.Articles.UpdateRange(versions);

            await DbContext.SaveChangesAsync();

        }

        /// <summary>
        /// Gets the sub articles for a page.
        /// </summary>
        /// <param name="urlPrefix">URL Prefix.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        private async Task<List<Article>> GetAllSubArticles(string urlPrefix)
        {
            if (string.IsNullOrEmpty(urlPrefix) || string.IsNullOrWhiteSpace(urlPrefix) || urlPrefix.Equals("/"))
            {
                urlPrefix = string.Empty;
            }
            else
            {
                urlPrefix = HttpUtility.UrlDecode(urlPrefix.ToLower().Replace("%20", "_").Replace(" ", "_"));
            }

            var query = DbContext.Articles.Where(a => a.UrlPath.StartsWith(urlPrefix));

            var list = await query.ToListAsync();
            return list;
        }

        /// <summary>
        /// Updates the base tag in the head if Angular is being used.
        /// </summary>
        /// <param name="headerJavaScript">Javascript header.</param>
        /// <param name="urlPath">Url path.</param>
        /// <returns>string.</returns>
        private string UpdateHeadBaseTag(string headerJavaScript, string urlPath)
        {
            if (string.IsNullOrEmpty(headerJavaScript))
            {
                return string.Empty;
            }

            var htmlDoc = new HtmlAgilityPack.HtmlDocument();

            htmlDoc.LoadHtml(headerJavaScript);

            // <meta name="ccms:framework" value="angular">
            var meta = htmlDoc.DocumentNode.SelectSingleNode("//meta[@name='ccms:framework']");

            // This only needs to be run if the framework is "Angular"
            if (meta != null && meta.Attributes["value"].Value.ToLower() != "angular")
            {
                return headerJavaScript;
            }

            var element = htmlDoc.DocumentNode.SelectSingleNode("//base");

            urlPath = $"/{HttpUtility.UrlDecode(urlPath.ToLower().Trim('/'))}/";

            if (element == null)
            {
                var metaTag = htmlDoc.CreateElement("base");
                metaTag.SetAttributeValue("href", urlPath);
                htmlDoc.DocumentNode.AppendChild(metaTag);
            }
            else
            {
                var href = element.Attributes["href"];

                if (href == null)
                {
                    element.Attributes.Add("href", urlPath);
                }
                else
                {
                    href.Value = urlPath;
                }
            }

            headerJavaScript = htmlDoc.DocumentNode.OuterHtml;

            return headerJavaScript;
        }

        /// <summary>
        /// Deletes a catalog entry.
        /// </summary>
        /// <param name="articleNumber">Article number.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        private async Task DeleteCatalogEntry(int articleNumber)
        {
            var catalogEntry = await DbContext.ArticleCatalog.FirstOrDefaultAsync(f => f.ArticleNumber == articleNumber);
            DbContext.ArticleCatalog.Remove(catalogEntry);
            await DbContext.SaveChangesAsync();
        }

        private string UpdatePrefix(string oldprefix, string newPrefix, string targetString)
        {
            var updated = newPrefix + targetString.TrimStart(oldprefix.ToArray());
            return updated;
        }

        /// <summary>
        /// Creates or updates a catalog entry.
        /// </summary>
        /// <param name="article">Article from which to derive the catalog entry.</param>
        private async Task<CatalogEntry> CreateCatalogEntry(Article article)
        {
            var lastVersion = await DbContext.Articles.Where(a => a.ArticleNumber == article.ArticleNumber).OrderByDescending(o => o.VersionNumber).LastOrDefaultAsync();

            var userId = lastVersion.UserId;
            var authorInfo = await DbContext.AuthorInfos.FirstOrDefaultAsync(f => f.Id == userId && f.AuthorName != string.Empty);
            var intro = string.Empty;

            try
            {
                if (!string.IsNullOrWhiteSpace(article.Content))
                {
                    // If there is a parsing problem, log it and continue.
                    var htmlDoc = new HtmlAgilityPack.HtmlDocument();
                    htmlDoc.LoadHtml(lastVersion.Content);
                    var paragraphs = htmlDoc.DocumentNode.SelectNodes("//p");
                    if (paragraphs != null)
                    {
                        var contentAreas = paragraphs.Where(w => !string.IsNullOrEmpty(w.InnerHtml) && !string.IsNullOrEmpty(w.InnerText.Trim().ToLower().Replace("&nbsp;", string.Empty)));
                        if (contentAreas.Count() > 0)
                        {
                            foreach (var area in contentAreas)
                            {
                                if (!string.IsNullOrEmpty(area.InnerHtml))
                                {
                                    intro = area.InnerHtml;
                                    break;
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error parsing article content during catalog entry.");
            }

            var oldEntry = await DbContext.ArticleCatalog.FirstOrDefaultAsync(f => f.ArticleNumber == article.ArticleNumber);

            if (oldEntry != null)
            {
                DbContext.ArticleCatalog.Remove(oldEntry);
                await DbContext.SaveChangesAsync();
            }

            var entry = new CatalogEntry()
            {
                ArticleNumber = article.ArticleNumber,
                BannerImage = article.BannerImage,
                Published = article.Published,
                Status = article.StatusCode == (int)StatusCodeEnum.Active ? "Active" : "Inactive",
                Title = article.Title,
                Updated = article.Updated,
                UrlPath = article.UrlPath,
                TemplateId = article.TemplateId,
                AuthorInfo = JsonConvert.SerializeObject(authorInfo).Replace("\"", "'"),
                Introduction = intro,
            };

            DbContext.ArticleCatalog.Add(entry);
            await DbContext.SaveChangesAsync();

            return entry;
        }
    }
}
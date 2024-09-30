// <copyright file="ArticleEditLogic.cs" company="Moonrise Software, LLC">
// Copyright (c) Moonrise Software, LLC. All rights reserved.
// Licensed under the GNU Public License, Version 3.0 (https://www.gnu.org/licenses/gpl-3.0.html)
// See https://github.com/MoonriseSoftwareCalifornia/CosmosCMS
// for more information concerning the license and the contributors participating to this project.
// </copyright>

namespace Cosmos.Cms.Data.Logic
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using System.Web;
    using Azure;
    using Azure.Identity;
    using Azure.ResourceManager;
    using Azure.ResourceManager.Cdn;
    using Azure.ResourceManager.Cdn.Models;
    using Cosmos.Cms.Common.Services.Configurations;
    using Cosmos.Cms.Controllers;
    using Cosmos.Cms.Models;
    using Cosmos.Common.Data;
    using Cosmos.Common.Data.Logic;
    using Cosmos.Common.Models;
    using Cosmos.Editor.Services;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.Caching.Memory;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Options;
    using Newtonsoft.Json;
    using SendGrid.Helpers.Errors.Model;

    /// <summary>
    ///     Article Editor Logic.
    /// </summary>
    /// <remarks>
    ///     Is derived from base class <see cref="ArticleLogic" />, adds on content editing functionality.
    /// </remarks>
    public class ArticleEditLogic : ArticleLogic
    {
        private readonly ILogger<ArticleEditLogic> logger;
        private readonly AzureCdnConfig azureCdnConfig;

        /// <summary>
        /// Initializes a new instance of the <see cref="ArticleEditLogic"/> class.
        ///     Constructor.
        /// </summary>
        /// <param name="dbContext">Database context.</param>
        /// <param name="memoryCache">Memory cache.</param>
        /// <param name="config">Cosmos configuration.</param>
        /// <param name="logger">Log service.</param>
        /// <param name="azureCdnConfig">Azure front door of CDN configuration.</param>
        public ArticleEditLogic(
            ApplicationDbContext dbContext,
            IMemoryCache memoryCache,
            IOptions<CosmosConfig> config,
            ILogger<ArticleEditLogic> logger,
            IOptions<AzureCdnConfig> azureCdnConfig
            )
            : base(
                dbContext,
                config,
                memoryCache,
                true)
        {
            this.logger = logger;
            this.azureCdnConfig = azureCdnConfig.Value;
        }

        /// <summary>
        ///     Gets database Context with Synchronize Context.
        /// </summary>
        public new ApplicationDbContext DbContext => base.DbContext;

        /// <summary>
        ///     Determine if this service is configured.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public async Task<bool> IsConfigured()
        {
            return await DbContext.IsConfigured();
        }


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
                if (reservedPath.EndsWith("*"))
                {
                    var value = reservedPath.TrimEnd(new char[] { '*' });
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
        ///         <seealso cref="ArticleLogic.BuildArticleViewModel(Article, string)" />. Creates a new article number.
        ///     </para>
        ///     <para>
        ///         If a template ID is given, the contents of this article is loaded with content from the <see cref="Template" />.
        ///     </para>
        ///     <para>
        ///         If this is the first article, it is saved as root and published immediately.
        ///     </para>
        /// </remarks>
        public async Task<ArticleViewModel> Create(string title, string userId, Guid? templateId = null)
        {
            // Is this the first article? If so, make it the root and publish it.
            var isFirstArticle = (await DbContext.Articles.CosmosAnyAsync()) == false;

            var isValidTitle = await ValidateTitle(title, null);

            if (!isValidTitle)
            {
                throw new Exception($"Title '{title}' conflicts with another article or reserved word.");
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
                defaultTemplate = "<div class=\"container m-y-lg\">" +
                                  "<main class=\"main-primary\">" +
                                      "<div class=\"row\">" +
                                          "<div contenteditable='true' class=\"col-md-12\"><h1>Why Lorem Ipsum</h1><p>" +
                                          LoremIpsum.WhyLoremIpsum + "</p></div>" +
                                          "</div>" +
                                      "</div>" +
                                  "</main>" +
                                  "</div>";
            }

            DateTimeOffset? published = (await DbContext.Articles.CosmosAnyAsync()) ? null : DateTimeOffset.UtcNow.AddMinutes(-5);

            // Max returns the incorrect result.
            int max;
            if ((await DbContext.ArticleNumbers.CosmosAnyAsync()) == false)
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
                StatusCode = 0,
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
                await HandlePublishing(article, userId);
            }

            // Finally update the catalog entry
            await UpdateCatalogEntry(article.ArticleNumber, (StatusCodeEnum)article.StatusCode);

            return await BuildArticleViewModel(article, "en-US");
        }

        /// <summary>
        ///     Makes an article the new home page.
        /// </summary>
        /// <param name="model">New home page post model.</param>
        /// <param name="userId">ID of user creating the page.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public async Task NewHomePage(NewHomeViewModel model, string userId)
        {
            // Remove the old page from home
            var oldHomeArticle = await DbContext.Articles.Where(w => w.UrlPath.ToLower() == "root").ToListAsync();

            var oldCatalogEntry = await DbContext.ArticleCatalog.Where(w => w.UrlPath.ToLower() == "root").ToListAsync();

            var newUrl = NormailizeArticleUrl(oldHomeArticle.FirstOrDefault()?.Title);
            foreach (var article in oldHomeArticle)
            {
                article.UrlPath = newUrl;
            }

            await DbContext.SaveChangesAsync();

            foreach (var item in oldCatalogEntry)
            {
                item.UrlPath = newUrl;
                item.Updated = DateTimeOffset.UtcNow;
            }

            await DbContext.SaveChangesAsync();

            await HandlePublishing(oldHomeArticle.OrderBy(o => o.VersionNumber).LastOrDefault(f => f.Published.HasValue), userId);

            // Make new page root
            var newHomeArticle = await DbContext.Articles.Where(w => w.ArticleNumber == model.ArticleNumber).ToListAsync();
            foreach (var article in newHomeArticle)
            {
                article.UrlPath = "root";
            }

            await DbContext.SaveChangesAsync();

            var newArticleNumber = newHomeArticle.FirstOrDefault().ArticleNumber;
            var newCatalogEntry = await DbContext.ArticleCatalog.FirstOrDefaultAsync(f => f.ArticleNumber == newArticleNumber);
            newCatalogEntry.UrlPath = "root";
            newCatalogEntry.Updated = DateTimeOffset.UtcNow;

            await DbContext.SaveChangesAsync();

            var published = newHomeArticle.OrderBy(o => o.VersionNumber).LastOrDefault(f => f.Published.HasValue);

            await HandlePublishing(published, userId);

            // await HandleLogEntry(published, $"Article {published.ArticleNumber} is now the new home page.", userId);
        }

        /// <summary>
        ///     This method puts an article into trash, and, all its versions.
        /// </summary>
        /// <param name="articleNumber">Article number.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        /// <remarks>
        ///     <para>This method puts an article into trash. Use <see cref="RetrieveFromTrash" /> to restore an article. </para>
        ///     <para>It also removes it from the page catalog and any published pages..</para>
        ///     <para>WARNING: Make sure the menu MenuController.Index does not reference deleted files.</para>
        /// </remarks>
        public async Task TrashArticle(int articleNumber)
        {
            var doomed = await DbContext.Articles.Where(w => w.ArticleNumber == articleNumber).ToListAsync();

            if (doomed == null)
            {
                throw new KeyNotFoundException($"Article number {articleNumber} not found.");
            }

            if (doomed.Any(a => a.UrlPath.ToLower() == "root"))
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
        }

        /// <summary>
        /// Permanently deletes an <paramref name="articleNumber"/>, does not trash item.
        /// </summary>
        /// <param name="articleNumber">Article number.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public async Task Purge(int articleNumber)
        {
            var doomed = await DbContext.Articles.Where(w => w.ArticleNumber == articleNumber).ToListAsync();

            if (doomed == null)
            {
                throw new KeyNotFoundException($"Article number {articleNumber} not found.");
            }

            DbContext.Articles.RemoveRange(doomed);

            await DbContext.SaveChangesAsync();

            await DeleteCatalogEntry(articleNumber);
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
        public async Task RetrieveFromTrash(int articleNumber, string userId)
        {
            var redeemed = await DbContext.Articles.Where(w => w.ArticleNumber == articleNumber).ToListAsync();

            if (redeemed == null)
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

            // Update the log
            // await HandleLogEntry(redeemed.LastOrDefault(), $"Recovered '{sample.Title}' from trash.", userId);
        }


        /// <summary>
        /// Updates or inserts an article. 
        /// </summary>
        /// <param name="model">HTML Editor post model.</param>
        /// <param name="userId">ID of user making the change.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public async Task<ArticleUpdateResult> Save(HtmlEditorViewModel model, string userId)
        {
            ArticleViewModel entity;
            if (model.ArticleNumber == 0)
            {
                entity = new ArticleViewModel()
                {
                    ArticleNumber = model.ArticleNumber,
                    Content = model.Content,
                    Id = model.Id,
                    Published = model.Published,
                    Title = model.Title,
                    VersionNumber = model.VersionNumber,
                    UrlPath = model.UrlPath,
                    BannerImage = model.BannerImage
                };
            }
            else
            {
                entity = await Get(model.Id, EnumControllerName.Edit, userId);

                entity.Published = model.Published;
                entity.Title = model.Title;
                entity.VersionNumber = model.VersionNumber;
                entity.Content = model.Content;
            }

            return await Save(entity, userId);
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
        ///         <see cref="ArticleLogic.BuildArticleViewModel(Article, string)" />.
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
        ///             <see cref="ArticleLogic.BuildArticleViewModel(Article, string)" />.
        ///         </item>
        ///         <item>
        ///            <see cref="Article.Updated"/> property is automatically updated with current UTC date and time.
        ///         </item>
        ///     </list>
        /// </remarks>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public async Task<ArticleUpdateResult> Save(ArticleViewModel model, string userId)
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

            // If an article version is not published and the model is not published, then update the existing version
            var updateOnly = article.Published.HasValue == false && model.Published.HasValue == false;

            // =======================================================
            // Don't track this for now
            if (!updateOnly)
            {
                // Detach so we can create a new verion.
                DbContext.Entry(article).State = EntityState.Detached;
            }

            // =======================================================
            // BEGIN: MAKE CONTENT CHANGES HERE
            // =======================================================
            model.Content = Ensure_ContentEditable_IsMarked(model.Content);

            Ensure_Oembed_Handled(model);

            // Make sure base tag is set properly.
            UpdateHeadBaseTag(model);

            // Article article = new Article()
            if (!updateOnly)
            {
                // Ensure a user ID is set for creator
                article.UserId = userId;

                // New version, gets a new ID.
                article.Id = Guid.NewGuid();
                article.VersionNumber = article.VersionNumber + 1;
                model.VersionNumber = article.VersionNumber;
            }

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

            if (!updateOnly)
            {
                DbContext.Articles.Add(article);
            }

            // Make sure this saves now
            await DbContext.SaveChangesAsync();

            // If we are publishing this to a static website, write it
            // to the blob storage now.
            if (this.CosmosOptions.Value.UseStaticPublisherWebsite)
            {

            }

            // IMPORTANT!
            // Handle title (and URL) changes for existing 
            await HandleTitleChange(article, oldTitle);

            // HANDLE PUBLISHING OF AN ARTICLE
            // This can be a new or existing article.
            var armOperaton = await HandlePublishing(article, userId);

            // Finally update the catalog entry
            await UpdateCatalogEntry(article.ArticleNumber, (StatusCodeEnum)article.StatusCode);

            var result = new ArticleUpdateResult
            {
                ServerSideSuccess = true,
                Model = model,
                ArmOperation = armOperaton
            };

            return result;
        }

        /// <summary>
        /// Performs a global search and replace (except items in trash).
        /// </summary>
        /// <param name="model">Search and replace view model.</param>
        /// <param name="userId">ID of the current user for logs.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public async Task SearchAndReplace(SearchAndReplaceViewModel model, string userId)
        {
            if (model.ArticleNumber.HasValue)
            {
                await ArticleSearchAndReplace(model, userId);
            }
            else
            {
                // We are doing a global search and replace
                var articleQuery = DbContext.Articles.AsQueryable();

                if (model.LimitToPublished)
                {
                    articleQuery = articleQuery.Where(a => a.Published != null);
                }

                if (model.IncludeContent)
                {
                    articleQuery = articleQuery.Where(w => w.Content.Contains(model.FindValue));
                }

                if (model.IncludeTitle)
                {
                    articleQuery = articleQuery.Where(w => w.Title.Contains(model.FindValue));
                }

                var articleNumbers = await articleQuery.Select(s => s.ArticleNumber).ToListAsync();

                foreach (var articleNumber in articleNumbers)
                {
                    await ArticleSearchAndReplace(
                        new SearchAndReplaceViewModel()
                        {
                            ArticleNumber = articleNumber,
                            FindValue = model.FindValue,
                            IncludeContent = model.IncludeContent,
                            IncludeTitle = model.IncludeTitle,
                            LimitToPublished = model.LimitToPublished,
                            ReplaceValue = model.ReplaceValue
                        }, userId);
                }
            }
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

            var elements = htmlDoc.DocumentNode.SelectNodes("//*[@contenteditable]|//*[@crx]|//*[@data-ccms-ceid]");

            if (elements == null)
            {
                return content;
            }

            // var count = 0;
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

                    if (element.Attributes.Contains("crx"))
                    {
                        element.Attributes.Remove("crx");
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
                if (element.Name.ToLower() == "div" && element.HasClass("ck-content") == false)
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
            if (elements != null && elements.Count > 0 && htmlDoc.DocumentNode.SelectNodes("//*[@data-ccms-ceid]").Any())
            {
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
        public async Task Unpublish(int articleNumber)
        {
            var versions = await DbContext.Articles.Where(w => w.ArticleNumber == articleNumber && w.Published != null).ToListAsync();

            if (versions.Any(a => a.UrlPath.Equals("root", StringComparison.InvariantCultureIgnoreCase)))
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

            await DbContext.SaveChangesAsync();
        }


        /// <summary>
        /// Purges the CDN (or Front Door) if either is configured.
        /// </summary>
        /// <param name="purgeUrls">Purge URL Paths.</param>
        /// <returns>ArmOperation results.</returns>
        public async Task<ArmOperation> PurgeCdn(List<string> purgeUrls)
        {
            purgeUrls = purgeUrls.Distinct().Select(s => s.Trim('/')).Select(s => s.Equals("root") ? "/" : "/" + s).ToList();

            ArmClient client = new ArmClient(new DefaultAzureCredential());

            try
            {
                // Check for Azure Frontdoor, if available use that.
                if (azureCdnConfig.IsConfigured() && azureCdnConfig.IsFrontDoor)
                {
                    var frontendEndpointResourceId = FrontDoorEndpointResource.CreateResourceIdentifier(
                        azureCdnConfig.SubscriptionId.ToString(),
                        azureCdnConfig.ResourceGroup,
                        azureCdnConfig.ProfileName,
                        azureCdnConfig.EndPointName);

                    var frontDoor = client.GetFrontDoorEndpointResource(frontendEndpointResourceId);

                    var purgeContent = new FrontDoorPurgeContent(purgeUrls);

                    var result = await frontDoor.PurgeContentAsync(WaitUntil.Started, purgeContent);

                    logger.LogInformation(logger.IsEnabled(LogLevel.Information) ? $"Successfully began Front Door purge of {string.Join(",", purgeUrls)} from Azure Front Door on {DateTimeOffset.UtcNow.ToString()}." : string.Empty);

                    return result;
                }
                else if (azureCdnConfig.IsConfigured())
                {
                    var cdnResource = CdnEndpointResource.CreateResourceIdentifier(
                        azureCdnConfig.SubscriptionId.ToString(),
                        azureCdnConfig.ResourceGroup,
                        azureCdnConfig.ProfileName,
                        azureCdnConfig.EndPointName);

                    var cdnEndpoint = client.GetCdnEndpointResource(cdnResource);

                    ArmOperation operation = null;

                    if (purgeUrls.Count > 100 || purgeUrls.Any(p => p.Equals("/") || p.Equals("/*")))
                    {
                        operation = cdnEndpoint.PurgeContent(Azure.WaitUntil.Started, new PurgeContent(new string[] { "/*" }));
                    }
                    else
                    {
                        // 100 paths or less, no need to page or use wildcard
                        var purgeContent = new PurgeContent(purgeUrls);
                        operation = cdnEndpoint.PurgeContent(WaitUntil.Started, purgeContent);
                    }

                    logger.LogInformation(logger.IsEnabled(LogLevel.Information) ? $"Successfully began CDN purge of {string.Join(",", purgeUrls)} on {DateTimeOffset.UtcNow.ToString()}." : string.Empty);

                    return operation;
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, ex.Message);
            }

            return null;
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
        private string NormailizeArticleUrl(string title)
        {
            // return HttpUtility.UrlEncode(title.Trim().Replace(" ", "_").ToLower()).Replace("%2f", "/");
            return title.Trim().Replace(" ", "_").ToLower();
        }

        private async Task ArticleSearchAndReplace(SearchAndReplaceViewModel model, string userId)
        {
            // We are doing a search and replace only for versions for a single article
            var articleQuery = DbContext.Articles.Where(a => a.ArticleNumber == model.ArticleNumber);

            if (model.LimitToPublished)
            {
                articleQuery = articleQuery.Where(a => a.Published != null);
            }

            if (model.IncludeContent)
            {
                articleQuery = articleQuery.Where(w => w.Content.Contains(model.FindValue));
            }

            if (model.IncludeTitle)
            {
                articleQuery = articleQuery.Where(w => w.Title.Contains(model.FindValue));
            }

            var entities = await articleQuery.ToListAsync();

            var oldTitle = entities.FirstOrDefault().Title;

            var count = 0;
            foreach (var entity in entities)
            {
                if (model.IncludeContent)
                {
                    entity.Content = entity.Content.Replace(model.FindValue, model.ReplaceValue);
                }

                if (model.IncludeTitle)
                {
                    entity.Title = entity.Title.Replace(model.FindValue, model.ReplaceValue);
                }
            }

            count++;
            if (count == 50)
            {
                count = 0;
                await DbContext.SaveChangesAsync();
            }

            if (model.IncludeTitle)
            {
                await HandleTitleChange(entities.FirstOrDefault(), oldTitle);
            }

            if (entities.Any(a => a.Published.HasValue))
            {
                var last = entities.OrderByDescending(a => a.Published.Value).LastOrDefault();
                await HandlePublishing(last, userId);
            }

            // Finally update the catalog entry
            await UpdateCatalogEntry(model.ArticleNumber.Value, (StatusCodeEnum)entities.FirstOrDefault().StatusCode);
        }

        /// <summary>
        /// Logic handing logic for publishing articles and saves changes to the database.
        /// </summary>
        /// <param name="article">Article entity.</param>
        /// <param name="userId">Current user ID.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        /// <remarks>
        /// If article is published, it adds the correct versions to the public pages collection. If not, 
        /// the article is removed from the public pages collection.
        /// </remarks>
        private async Task<ArmOperation> HandlePublishing(Article article, string userId)
        {
            if (article.Published.HasValue)
            {
                // await HandleLogEntry(article, $"Published for: {article.Published.Value}.", userId);
                try
                {
                    var others = await DbContext.Articles.Where(w => w.ArticleNumber == article.ArticleNumber && w.Published != null && w.Id != article.Id).ToListAsync();

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
                    await ResetVersionExpirations(article.ArticleNumber);

                    // Update the published pages collection
                    return await UpdatePublishedPages(article.ArticleNumber);
                }
                catch (Exception e)
                {
                    var t = e;
                }
            }

            return null;
        }

        /// <summary>
        ///     Gets a template represented as an <see cref="ArticleViewModel" />.
        /// </summary>
        /// <param name="template">Page template model.</param>
        /// <returns>ArticleViewModel.</returns>
        private ArticleViewModel BuildTemplateViewModel(Template template)
        {
            var articleNumber = DbContext.Articles.Max(m => m.ArticleNumber) + 1;

            return new ()
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
        /// If an OEMBED element is present, ensures the necessary JavaScript is injected.
        /// </summary>
        /// <param name="model">Article view model.</param>
        private void Ensure_Oembed_Handled(ArticleViewModel model)
        {
            var htmlDoc = new HtmlAgilityPack.HtmlDocument();
            var footerDoc = new HtmlAgilityPack.HtmlDocument();

            htmlDoc.LoadHtml(string.IsNullOrEmpty(model.Content) ? string.Empty : model.Content);
            footerDoc.LoadHtml(string.IsNullOrEmpty(model.FooterJavaScript) ? string.Empty : model.FooterJavaScript);

            var oembed = htmlDoc.DocumentNode.SelectNodes("//oembed[@url]");
            var hasOembed = oembed != null && oembed.Any();

            var embedlyElements = footerDoc.DocumentNode.SelectNodes("//script[@id='cwps_embedly']");
            var scriptElements = footerDoc.DocumentNode.SelectNodes("//script[@id='cwps_embedly_launch']");

            // Now add or remove supporting JavaScript as  needed
            if (hasOembed)
            {
                // There are OEmbeds, so add supporting JavaScript injects below
                if (embedlyElements == null || !embedlyElements.Any())
                {
                    var embedly = footerDoc.CreateElement("script");
                    embedly.Id = "cwps_embedly";
                    embedly.Attributes.Append("async");
                    embedly.Attributes.Append("charset", "utf-8");
                    embedly.Attributes.Append("src", "//cdn.embedly.com/widgets/platform.js");

                    footerDoc.DocumentNode.AppendChild(embedly);
                }

                if (scriptElements == null || !scriptElements.Any())
                {
                    var addon = footerDoc.CreateElement("script");
                    addon.Id = "cwps_embedly_launch";
                    addon.InnerHtml = "document.querySelectorAll( 'oembed[url]' ).forEach( element => { const anchor = document.createElement( 'a' ); anchor.setAttribute( 'href', element.getAttribute( 'url' ) ); anchor.className = 'embedly-card'; element.appendChild( anchor ); });";

                    footerDoc.DocumentNode.AppendChild(addon);
                }

                model.FooterJavaScript = footerDoc.DocumentNode.OuterHtml;
            }
            else
            {
                // There are NO OEmbeds, so REMOVE supporting JavaScript injects below
                if (embedlyElements != null && embedlyElements.Any())
                {
                    foreach (var el in embedlyElements)
                    {
                        el.Remove();
                    }
                }

                if (scriptElements != null && scriptElements.Any())
                {
                    foreach (var el in scriptElements)
                    {
                        el.Remove();
                    }
                }

                model.FooterJavaScript = footerDoc.DocumentNode.OuterHtml;
            }
        }

        /// <summary>
        /// Resets the expiration dates, based on the last published article, saves changes to the database.
        /// </summary>
        /// <param name="articleNumber">Article number.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        private async Task ResetVersionExpirations(int articleNumber)
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
        /// Updates the published pages collection by article number.
        /// </summary>
        /// <param name="articleNumber">Article number to publish.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        private async Task<ArmOperation> UpdatePublishedPages(int articleNumber)
        {
            // Now we are going to update the Pages table
            var itemsToPublish = await DbContext.Articles.Where(w => w.ArticleNumber == articleNumber && w.Published != null)
                .OrderByDescending(o => o.Published).AsNoTracking().ToListAsync();

            var paths = itemsToPublish.Select(s => s.UrlPath).Distinct().ToList();

            // Get everything that is going to be removed or replaced
            var itemsToRemove = await DbContext.Pages.Where(w => w.ArticleNumber == articleNumber || paths.Contains(w.UrlPath)).ToListAsync();

            if (itemsToRemove.Any())
            {
                // Mark these for deletion - do this first to avoid any conflicts
                DbContext.Pages.RemoveRange(itemsToRemove);
                await DbContext.SaveChangesAsync();
                paths.AddRange(itemsToRemove.Select(s => s.UrlPath).ToArray());
            }

            if (itemsToPublish.Any())
            {
                // Now refresh the published pages
                foreach (var item in itemsToPublish)
                {
                    var authorInfo = await DbContext.AuthorInfos.FirstOrDefaultAsync(f => f.UserId == item.UserId && f.AuthorName != string.Empty);

                    // Carry over the permissions for this page.
                    var catalogEntry = await DbContext.ArticleCatalog.FirstOrDefaultAsync(f => f.ArticleNumber == item.ArticleNumber);

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

                    // Check for duplicate
                    var duplicate = await DbContext.Pages.FirstOrDefaultAsync(f => f.Id == newPage.Id);
                    if (duplicate == null)
                    {
                        DbContext.Pages.Add(newPage);
                    }
                    else
                    {
                        throw new Exception($"Duplicate Page Id. Existing: {duplicate.Id} New: {newPage.Id} ArticleId: {articleNumber}.");
                    }
                }

                // Update the pages collection
                await DbContext.SaveChangesAsync();
            }

            if (paths.Any())
            {
                var purgeUrls = paths.Select(s => "/" + s.Trim('/')).ToList();

                try
                {
                    return await PurgeCdn(purgeUrls);
                }
                catch (Exception e)
                {
                    logger.LogError("CDN Error:", e);
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
        private async Task HandleTitleChange(Article article, string oldTitle)
        {
            if (string.Equals(article.Title, oldTitle, StringComparison.CurrentCultureIgnoreCase) && !string.IsNullOrEmpty(article.UrlPath))
            {
                // Nothing to do
                return;
            }

            // Capture the new title
            var newTitle = article.Title;

            var articleNumbersToUpdate = new List<int>();

            articleNumbersToUpdate.Add(article.ArticleNumber);

            // Validate that title is not already taken.
            if (!await ValidateTitle(newTitle, article.ArticleNumber))
            {
                throw new Exception($"Title '{newTitle}' already taken");
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

                // Create a redirect
                var entity = new Article
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

                // Add redirect here
                DbContext.Articles.Add(entity);
                await DbContext.SaveChangesAsync();

                // await HandleLogEntry(entity, $"Redirect {oldUrl} to {newUrl}", userId);
            }

            // We have to change the title and paths for all versions now.
            var versions = await DbContext.Articles.Where(w => w.ArticleNumber == article.ArticleNumber)
                .ToListAsync();

            var catalog = await DbContext.ArticleCatalog.Where(a => a.ArticleNumber == article.ArticleNumber).ToListAsync();

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

            foreach (var item in catalog)
            {
                item.Title = newTitle;
                item.Updated = DateTime.Now.ToUniversalTime();
                item.UrlPath = article.UrlPath;
            }

            DbContext.Articles.UpdateRange(versions);
            DbContext.ArticleCatalog.UpdateRange(catalog);

            await DbContext.SaveChangesAsync();

            // Now update the published pages
            foreach (var num in articleNumbersToUpdate)
            {
                await UpdatePublishedPages(num);
            }
        }

        /// <summary>
        /// Update head tag to match path. 
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
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

            return;
        }

        /// <summary>
        /// Update head tag to match path. 
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        /// <remarks>
        /// Angular uses the BASE tag within the HEAD to set relative path to article/app.
        /// If that tag is detected, it is updated automatically to match the current <see cref="Article.UrlPath"/>.
        /// </remarks>
        public void UpdateHeadBaseTag(Article model)
        {
            if (!string.IsNullOrEmpty(model.HeaderJavaScript) && (model.HeaderJavaScript.Contains("<base ") || (model.HeaderJavaScript.ToLower().Contains("ccms:framework") && model.HeaderJavaScript.ToLower().Contains("angular"))))
            {
                model.HeaderJavaScript = UpdateHeadBaseTag(model.HeaderJavaScript, model.UrlPath);
            }

            return;
        }

        /// <summary>
        /// Updates the base tag in the head if Angular is being used.
        /// </summary>
        /// <param name="headerJavaScript"></param>
        /// <param name="urlPath"></param>
        /// <returns></returns>
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
        ///     Changes the status of an article by marking all versions with that status.
        /// </summary>
        /// <param name="articleNumber">Article to set status for.</param>
        /// <param cref="StatusCodeEnum" name="code"></param>
        /// <param name="userId"></param>
        /// <exception cref="Exception">User ID or article number not found.</exception>
        /// <returns>Returns the number of versions for the given article where status was set.</returns>
        public async Task<int> SetStatus(int articleNumber, StatusCodeEnum code, string userId)
        {
            if (!await DbContext.Users.Where(a => a.Id == userId).CosmosAnyAsync())
            {
                throw new Exception($"User ID: {userId} not found!");
            }

            var versions =
                await DbContext.Articles.Where(a => a.ArticleNumber == articleNumber).ToListAsync();

            if (!versions.Any())
            {
                throw new Exception($"Article number: {articleNumber} not found!");
            }

            foreach (var version in versions)
            {
                version.StatusCode = (int)code;

                var statusText = code switch
                {
                    StatusCodeEnum.Deleted => "deleted",
                    StatusCodeEnum.Active => "active",
                    _ => "inactive"
                };

                DbContext.ArticleLogs.Add(new ArticleLog
                {
                    ActivityNotes = $"Status changed to '{statusText}'.",
                    IdentityUserId = userId,
                    DateTimeStamp = DateTime.Now.ToUniversalTime(),
                    ArticleId = version.Id
                });
            }

            var count = await DbContext.SaveChangesAsync();
            return count;
        }

        #region GET METHODS ONLY FOR EDITOR

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
        ///         <see cref="ArticleLogic.BuildArticleViewModel(Article, string)" />.
        ///     </para>
        ///     <para>NOTE: Cannot access articles that have been deleted.</para>
        /// </remarks>
        public async Task<ArticleViewModel> Get(int articleNumber, int? versionNumber)
        {
            Article article;

            if (versionNumber.HasValue)
            {
                // Get a specific version
                article = await DbContext.Articles
                .FirstOrDefaultAsync(
                    a => a.ArticleNumber == articleNumber &&
                         a.VersionNumber == versionNumber &&
                         a.StatusCode != 2);
            }
            else
            {
                // Get the latest version
                article = await DbContext.Articles.OrderByDescending(v => v.VersionNumber)
                .FirstOrDefaultAsync(
                    a => a.ArticleNumber == articleNumber &&
                         a.StatusCode != 2);
            }

            if (article == null)
            {
                throw new Exception($"Article number:{articleNumber}, Version:{versionNumber}, not found.");
            }

            return await BuildArticleViewModel(article, "en-US");
        }

        /// <summary>
        ///     Gets an article by ID (row Key), or creates a new (unsaved) article if id is null.
        /// </summary>
        /// <param name="id">Row Id (or identity) number.  If null returns a new article.</param>
        /// <param name="controllerName"></param>
        /// <param name="userId"></param>
        /// <remarks>
        ///     <para>
        ///         For new articles, uses <see cref="Create" /> and the method
        ///         <see cref="ArticleLogic.BuildArticleViewModel(Article, string)" /> to
        ///         generate the <see cref="ArticleViewModel" /> .
        ///     </para>
        ///     <para>
        ///         Retrieves <see cref="Article" /> and builds an <see cref="ArticleViewModel" /> using the method
        ///         <see cref="ArticleLogic.BuildArticleViewModel(Article, string)" />,
        ///         or in the case of a template, uses method <see cref="BuildTemplateViewModel" />.
        ///     </para>
        ///     <para>
        ///         Makes sure editable areas are properly marked with <see cref="Ensure_ContentEditable_IsMarked(string)"/>.
        ///     </para>
        /// </remarks>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        /// <remarks>
        ///     <para>
        ///         Returns <see cref="ArticleViewModel" />. For more details on what is returned, see
        ///         <see cref="ArticleLogic.BuildArticleViewModel(Article, string)" /> or <see cref="BuildTemplateViewModel" />.
        ///     </para>
        ///     <para>NOTE: Cannot access articles that have been deleted.</para>
        /// </remarks>
        public async Task<ArticleViewModel> Get(Guid? id, EnumControllerName controllerName, string userId)
        {
            if (controllerName == EnumControllerName.Template)
            {
                if (id == null)
                {
                    throw new Exception("Template ID:null not found.");
                }

                var idNo = id.Value;
                var template = await DbContext.Templates.FindAsync(idNo);

                if (template == null)
                {
                    throw new Exception($"Template ID:{id} not found.");
                }

                return BuildTemplateViewModel(template);
            }

            // This is used to create a "blank" page just so we have something to get started with.
            if (id == null)
            {
                var count = await DbContext.Articles.CountAsync();
                return await Create("Page " + count, userId);
            }

            var article = await DbContext.Articles
                .FirstOrDefaultAsync(a => a.Id == id && a.StatusCode != 2);

            if (article == null)
            {
                throw new Exception($"Article ID:{id} not found.");
            }

            article.Content = Ensure_ContentEditable_IsMarked(article.Content);

            return await BuildArticleViewModel(article, "en-US");
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
                urlPrefix = System.Web.HttpUtility.UrlDecode(urlPrefix.ToLower().Replace("%20", "_").Replace(" ", "_"));
            }

            var query = DbContext.Articles.Where(a => a.UrlPath.StartsWith(urlPrefix));
            // .Where(a => a.Published <= DateTime.UtcNow && a.UrlPath.Like());
            try
            {
                var list = await query.ToListAsync();
                return list;
            }
            catch (Exception e)
            {
                var t = e; // For debuging purposes
                throw;
            }
        }

        /// <summary>
        /// Gets a page, and allows unpublished or inactive pages to be returned.
        /// </summary>
        /// <param name="urlPath">URL path.</param>
        /// <param name="lang">Language code.</param>
        /// <param name="publishedOnly">Only published articles.</param>
        /// <param name="onlyActive">Only active articles.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public async Task<ArticleViewModel> GetByUrl(string urlPath, string lang = "", bool publishedOnly = true, bool onlyActive = true)
        {
            if (publishedOnly && onlyActive)
            {
                return await base.GetPublishedPageByUrl(urlPath, lang);
            }

            var activeStatusCodes =
                onlyActive ? new[] { 0, 3 } : new[] { 0, 1, 3 }; // i.e. StatusCode.Active (DEFAULT) and StatusCode.Redirect


            urlPath = urlPath?.ToLower().Trim(new char[] { ' ', '/' });
            if (string.IsNullOrEmpty(urlPath) || urlPath.Trim() == "/")
            {
                urlPath = "root";
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
                    .Where(a => a.UrlPath == urlPath && activeStatusCodes.Contains(a.StatusCode))
                    .OrderByDescending(o => o.VersionNumber)
                    .FirstOrDefaultAsync();
                return await base.BuildArticleViewModel(article, lang);
            }
        }

        #region LISTS

        /// <summary>
        /// Get a list of article redirects.
        /// </summary>
        /// <returns></returns>
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
        ///     Gets the latest versions of articles that are in the trash.
        /// </summary>
        /// <returns>Gets article number, version number, last data published (if applicable).</returns>
        /// <remarks>
        /// </remarks>
        public async Task<List<ArticleListItem>> GetArticleTrashList()
        {
            var data = await
                (from x in DbContext.Articles
                 where x.StatusCode == (int)StatusCodeEnum.Deleted
                 select new
                 {
                     x.ArticleNumber,
                     x.VersionNumber,
                     x.Published,
                     x.StatusCode,
                     x.Title
                 }).ToListAsync();

            var model =
                (from x in data
                 where x.StatusCode == (int)StatusCodeEnum.Deleted
                 group x by x.ArticleNumber
                    into g
                 select new ArticleListItem
                 {
                     ArticleNumber = g.Key,
                     Title = g.FirstOrDefault().Title,
                     VersionNumber = g.Max(i => i.VersionNumber),
                     LastPublished = g.Max(m => m.Published),
                     Status = g.Max(f => f.StatusCode) == 0 ? "Active" : "Inactive"
                 }).ToList();

            return model;
        }

        #endregion

        #endregion

        #region PAGE EXPORT

        /// <summary>
        /// Exports and article as HTML with layout elements.
        /// </summary>
        /// <param name="article"></param>
        /// <param name="blobPublicAbsoluteUrl"></param>
        /// <param name="viewRenderService"></param>
        /// <returns>web page.</returns>
        public async Task<string> ExportArticle(ArticleViewModel article, Uri blobPublicAbsoluteUrl, Services.IViewRenderService viewRenderService)
        {
            var htmlUtilities = new Services.HtmlUtilities();

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

        #endregion

        #region RESERVED PATHS

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
                // { "pub", "api", "GetSupportedLanguages", "GetTOC", "AccessPending", "GetMicrosoftIdentityAssociation", "Error" };
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
        /// <param name="model"></param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        /// <exception cref="Exception">Cannot add path because it is already reserved.</exception>
        public async Task AddOrUpdateReservedPath(ReservedPath model)
        {
            if (model.CosmosRequired)
            {
                throw new Exception($"Reserved path {model.Path} cannot be modified.");
            }

            model.Path = model.Path.ToLower().Replace("\\", "/").Trim('/').TrimStart('*');

            var paths = await GetReservedPaths();

            // check for update
            var entity = paths.FirstOrDefault(f => f.Id == model.Id);

            if (entity == null)
            {
                if (paths.Any(a => a.Path.ToLower() == model.Path.ToLower()))
                {
                    throw new Exception($"Cannot add path '{model.Path}' because it is already reserved.");
                }

                paths.Add(model);
            }
            else
            {
                if (paths.Any(a => a.Path.ToLower() == model.Path.ToLower() && a.Id != model.Id))
                {
                    throw new Exception($"Cannot rename to path '{model.Path}' because it is already reserved.");
                }

                entity.Notes = model.Notes;
                entity.Path = model.Path;
            }

            var setting = await DbContext.Settings.FirstOrDefaultAsync(f => f.Name == "ReservedPaths");
            setting.Value = JsonConvert.SerializeObject(paths);
            await DbContext.SaveChangesAsync();
        }

        /// <summary>
        /// Removes a reserved path.
        /// </summary>
        /// <param name="Id"></param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        /// <exception cref="Exception">Path not found.</exception>
        public async Task RemoveReservedPath(Guid Id)
        {
            var paths = await GetReservedPaths();

            var doomed = paths.FirstOrDefault(a => a.Id == Id);

            if (doomed == null)
            {
                throw new Exception($"Path Id '{Id}' not found, could not delete.");
            }

            if (doomed.CosmosRequired)
            {
                throw new Exception($"Reserved path {doomed.Path} cannot be deleted.");
            }

            paths.Remove(doomed);

            var setting = await DbContext.Settings.FirstOrDefaultAsync(f => f.Name == "ReservedPaths");
            setting.Value = JsonConvert.SerializeObject(paths);
            await DbContext.SaveChangesAsync();
        }

        #endregion

        /// <summary>
        /// Deletes a catalog entry.
        /// </summary>
        /// <param name="articleNumber">Article number</param>
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
        /// Update catalog entry.
        /// </summary>
        /// <param name="articleNumber">Article number.</param>
        /// <param name="code">Article status code.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        private async Task UpdateCatalogEntry(int articleNumber, StatusCodeEnum code)
        {
            var versions = await DbContext.Articles.Where(w => w.ArticleNumber == articleNumber).ToListAsync();

            var catalogEntry = await DbContext.ArticleCatalog.FirstOrDefaultAsync(f => f.ArticleNumber == articleNumber);

            if (catalogEntry == null)
            {
                var data = versions.FirstOrDefault();

                catalogEntry = new CatalogEntry()
                {
                    ArticleNumber = articleNumber,
                    Updated = data.Updated,
                    Status = code == StatusCodeEnum.Active ? "Active" : "Inactive",
                    Published = data.Published,
                    Title = data.Title,
                    UrlPath = data.UrlPath,
                    TemplateId = data.TemplateId
                };

                DbContext.ArticleCatalog.Add(catalogEntry);
            }
            else
            {
                var data = (from v in versions
                            group v by v.Title into summary
                            select new CatalogEntry
                            {
                                Title = summary.Key,
                                ArticleNumber = articleNumber,
                                Published = summary.Max(m => m.Published),
                                Updated = summary.Max(m => m.Updated),
                                UrlPath = catalogEntry.UrlPath
                            }).FirstOrDefault();

                catalogEntry.Updated = data.Updated;
                catalogEntry.Status = code == StatusCodeEnum.Active ? "Active" : "Inactive";
                catalogEntry.Published = data.Published;
                catalogEntry.Title = data.Title;
                catalogEntry.UrlPath = data.UrlPath;
            }

            await DbContext.SaveChangesAsync();
        }
    }
}

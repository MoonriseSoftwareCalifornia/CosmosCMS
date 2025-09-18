﻿// <copyright file="ArticleLogic.cs" company="Moonrise Software, LLC">
// Copyright (c) Moonrise Software, LLC. All rights reserved.
// Licensed under the GNU Public License, Version 3.0 (https://www.gnu.org/licenses/gpl-3.0.html)
// See https://github.com/MoonriseSoftwareCalifornia/CosmosCMS
// for more information concerning the license and the contributors participating to this project.
// </copyright>

namespace Cosmos.Common.Data.Logic
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Text.RegularExpressions;
    using System.Threading.Tasks;
    using Cosmos.Cms.Common.Services.Configurations;
    using Cosmos.Common.Data;
    using Cosmos.Common.Models;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.Caching.Distributed;
    using Microsoft.Extensions.Caching.Memory;
    using Microsoft.Extensions.Options;
    using Newtonsoft.Json;
    using X.Web.Sitemap;

    /// <summary>
    ///     Main logic behind getting and maintaining web site articles.
    /// </summary>
    /// <remarks>An article is the "content" behind a web page.</remarks>
    public class ArticleLogic
    {
        private readonly bool isEditor;
        private readonly IMemoryCache memoryCache;
        private readonly string publisherUrl;
        private readonly string blobPublicUrl;

        /// <summary>
        /// Initializes a new instance of the <see cref="ArticleLogic"/> class.
        /// </summary>
        /// <param name="dbContext">Database context.</param>
        /// <param name="config">Cosmos configuration.</param>
        /// <param name="memoryCache">Memory cache used only by Publishers.</param>
        /// <param name="publisherUrl">Publisher URL override.</param>
        /// <param name="blobPublicUrl">Blob public URL.</param>
        /// <param name="isEditor">Is in edit mode or not.</param>
        public ArticleLogic(
            ApplicationDbContext dbContext,
            IOptions<CosmosConfig> config,
            IMemoryCache memoryCache,
            string publisherUrl,
            string blobPublicUrl,
            bool isEditor = false)
        {
            this.memoryCache = memoryCache;
            DbContext = dbContext;
            CosmosOptions = config;
            this.isEditor = isEditor;
            this.publisherUrl = publisherUrl;
            this.blobPublicUrl = blobPublicUrl;
        }

        /// <summary>
        /// Gets provides cache hit information.
        /// </summary>
        public string[] CacheResult { get; internal set; }

        /// <summary>
        ///     Gets database Content.
        /// </summary>
        protected ApplicationDbContext DbContext { get; }

        /// <summary>
        ///     Gets site customization config.
        /// </summary>
        protected IOptions<CosmosConfig> CosmosOptions { get; }

        /// <summary>
        /// Determines if a publisher can serve requests.
        /// </summary>
        /// <returns>Indicates the publisher is healthy using a <see cref="bool"/>.</returns>
        public static bool GetPublisherHealth() => true;

        /// <summary>
        ///     Deserializes an object using <see cref="Newtonsoft.Json.JsonConvert.DeserializeObject(string)" />
        ///     and <see cref="System.Text.Encoding.UTF32" />.
        /// </summary>
        /// <param name="bytes">Byte array.</param>
        /// <typeparam name="T">The generic type to convert the bytes to.</typeparam>
        /// <returns>Returns the bytes as an object.</returns>
        public static T Deserialize<T>(byte[] bytes)
        {
            var data = Encoding.UTF32.GetString(bytes);
            return JsonConvert.DeserializeObject<T>(data);
        }

        /// <summary>
        ///     Serializes an object using <see cref="Newtonsoft.Json.JsonConvert.SerializeObject(object)" />
        ///     and <see cref="System.Text.Encoding.UTF32" />.
        /// </summary>
        /// <param name="obj">Object to serialize into a byte array.</param>
        /// <returns>Returns a <see cref="byte"/> array.</returns>
        public static byte[] Serialize(object obj)
        {
            if (obj == null)
            {
                return null;
            }

            return Encoding.UTF32.GetBytes(JsonConvert.SerializeObject(obj));
        }

        /// <summary>
        /// Gets the sitemap for a website.
        /// </summary>
        /// <returns>Sitemap.</returns>
        public async Task<Sitemap> GetSiteMap()
        {
            var publicUrl = "/";
            var dt = DateTimeOffset.UtcNow.AddMinutes(10);
            var query = from t in DbContext.ArticleCatalog
                        where t.Published <= dt
                        select new
                        {
                            t.UrlPath,
                            t.Title,
                            t.Published,
                            t.Updated,
                            t.BannerImage
                        };
            var items = await query.ToListAsync();
            var home = items.FirstOrDefault(f => f.UrlPath == "root");
            var others = items.Where(w => w.UrlPath != "root").ToList();

            var sitemap = new Sitemap();

            var url = new Url
            {
                Location = publicUrl,
                LastMod = (home == null || home.Updated == null) ? DateTimeOffset.UtcNow.ToString("u") : home.Updated.ToString("u"),
                Priority = 1.0,
                Images = new List<Image>()
            };

            if (home != null && !string.IsNullOrWhiteSpace(home.BannerImage))
            {
                url.Images.Add(new Image
                {
                    Location = $"{publicUrl}/images/logo.png"
                });
            }

            sitemap.Add(url);

            foreach (var item in others)
            {
                var images = new List<Image>();

                if (!string.IsNullOrEmpty(item.BannerImage))
                {
                    images.Add(new Image
                    {
                        Location = item.UrlPath.ToLower().StartsWith("http") ? item.UrlPath : $"{publicUrl}/{item.UrlPath.TrimStart('/')}"
                    });
                }

                sitemap.Add(new Url
                {
                    Location = $"{publicUrl}/{item.UrlPath}",
                    LastMod = item.Updated.ToString("u"),
                    Priority = 0.5,
                    Images = images
                });
            }

            return sitemap;
        }

        /// <summary>
        /// Gets the list of child pages for a given page URL.
        /// </summary>
        /// <param name="prefix">Page url.</param>
        /// <param name="pageNo">Zero based index (page 1 is index 0).</param>
        /// <param name="pageSize">Number of records in a page.</param>
        /// <param name="orderByPublishedDate">Order by decending order when was published (most recent on top).</param>
        /// <returns>Returns a <see cref="TableOfContents"/>.</returns>
        public async Task<TableOfContents> GetTableOfContents(string prefix, int pageNo = 0, int pageSize = 10, bool orderByPublishedDate = false)
        {
            if (string.IsNullOrEmpty(prefix) || string.IsNullOrWhiteSpace(prefix) || prefix.Equals("/"))
            {
                prefix = string.Empty;
            }
            else
            {
                prefix = (System.Web.HttpUtility.UrlDecode(prefix.ToLower().Replace("%20", "_").Replace(" ", "_")) + "/").Trim('/');
            }

            var skip = pageNo * pageSize;

            IQueryable<TableOfContentsItem> query;

            if (string.IsNullOrEmpty(prefix))
            {
                query = from t in DbContext.ArticleCatalog
                        where t.Published.HasValue
                        select new TableOfContentsItem
                        {
                            UrlPath = t.UrlPath,
                            Title = t.Title,
                            Published = t.Published.Value,
                            Updated = t.Updated,
                            BannerImage = t.BannerImage,
                            AuthorInfo = t.AuthorInfo,
                            Introduction = t.Introduction
                        };
            }
            else
            {
                var count = prefix.Count(c => c == '/');

                var dcount = "{" + count + "}";
                var epath = prefix.TrimStart('/').Replace("/", "\\/");
                var pattern = $"(?i)(^[{epath}]*)(\\/[^\\/]*){dcount}$";

                query = from t in DbContext.ArticleCatalog
                        where t.Published.HasValue
                        && t.UrlPath != prefix
                        && t.UrlPath.StartsWith(prefix)
                        && Regex.IsMatch(t.UrlPath, pattern)
                        select new TableOfContentsItem
                        {
                            UrlPath = t.UrlPath,
                            Title = t.Title,
                            Published = t.Published.Value,
                            Updated = t.Updated,
                            BannerImage = t.BannerImage,
                            AuthorInfo = t.AuthorInfo,
                            Introduction = t.Introduction
                        };
            }

            var data = await query.ToListAsync();
            var sort = data.AsQueryable();

            if (orderByPublishedDate)
            {
                sort = sort.OrderByDescending(o => o.Published);
            }
            else
            {
                sort = sort.OrderBy(o => o.Title);
            }

            var d = DateTimeOffset.UtcNow;

            var items = sort.Where(w => w.Published.UtcDateTime <= d).Skip(skip).Take(pageSize).ToList();

            var model = new TableOfContents
            {
                TotalCount = items.Count,
                PageNo = pageNo,
                PageSize = pageSize,
                Items = items,
                PublisherUrl = publisherUrl,
                BlobPublicUrl = blobPublicUrl
            };

            return model;
        }

        /// <summary>
        ///     Gets the current *published* version of an article.  Gets the home page if ID is null.
        /// </summary>
        /// <param name="urlPath">URL Encoded path.</param>
        /// <param name="lang">Language to return content as.</param>
        /// <param name="cacheSpan">Length of time for cache to exist.</param>
        /// <param name="layoutCache">Layout cache duration.</param>
        /// <param name="includeLayout">Include layout with request.</param>
        /// <returns>
        ///     <see cref="ArticleViewModel" />.
        /// </returns>
        /// <remarks>
        ///     <para>
        ///     Retrieves an article from the following sources in order:
        ///     </para>
        ///    <list type="number">
        ///       <item>Short term (5 second) Entity Framework Cache</item>
        ///       <item>SQL Database</item>
        ///     </list>
        ///     <para>
        ///         Returns <see cref="ArticleViewModel" />.
        ///     </para>
        ///     <para>NOTE: Cannot access articles that have been deleted.</para>
        /// </remarks>
        public virtual async Task<ArticleViewModel> GetPublishedPageByUrl(string urlPath, string lang = "", TimeSpan? cacheSpan = null, TimeSpan? layoutCache = null, bool includeLayout = true)
        {
            urlPath = urlPath?.ToLower().Trim(new char[] { ' ', '/' });
            if (string.IsNullOrEmpty(urlPath) || urlPath.Trim() == "/")
            {
                urlPath = "root";
            }

            if (memoryCache == null || cacheSpan == null)
            {
                var dt = DateTimeOffset.UtcNow;
                var entity = await DbContext.Pages
               .Where(a => a.UrlPath == urlPath && a.Published <= dt)
               .OrderByDescending(o => o.VersionNumber).AsNoTracking().FirstOrDefaultAsync();

                if (entity == null)
                {
                    return null;
                }

                return await BuildArticleViewModel(entity, lang, includeLayout: includeLayout);
            }

            memoryCache.TryGetValue($"{urlPath}-{lang}-{includeLayout}", out ArticleViewModel model);

            if (model == null)
            {
                // This is for SQLite which does not support DateTimeOffset natively.
                var dt = DateTimeOffset.UtcNow;
                var data = await DbContext.Pages
                   .Where(a => a.UrlPath == urlPath && a.Published.HasValue && a.Published <= dt)
                   .OrderByDescending(o => o.VersionNumber).AsNoTracking().FirstOrDefaultAsync();

                if (data == null)
                {
                    return null;
                }

                model = await BuildArticleViewModel(data, lang, layoutCache, includeLayout);

                memoryCache.Set($"{urlPath}-{lang}-{includeLayout}", model, cacheSpan.Value);
            }

            return model;
        }

        /// <summary>
        ///    Gets the header of the current *published* version of an article.  Gets the home page if ID is null.
        /// </summary>
        /// <param name="urlPath">Path to article.</param>
        /// <returns>ArticleViewModel.</returns>
        public virtual async Task<ArticleViewModel> GetPublishedPageHeaderByUrl(string urlPath)
        {
            urlPath = urlPath?.ToLower().Trim(new char[] { ' ', '/' });
            if (string.IsNullOrEmpty(urlPath) || urlPath.Trim() == "/")
            {
                urlPath = "root";
            }

            // This is for SQLite which does not support DateTimeOffset natively.
            var dt = DateTimeOffset.UtcNow;
            return await DbContext.Pages
                       .Where(a => a.UrlPath == urlPath && a.Published.HasValue && a.Published <= dt)
                       .Select(s => new ArticleViewModel
                       {
                           ArticleNumber = s.ArticleNumber,
                           Id = s.Id,
                           Expires = s.Expires,
                           Updated = s.Updated,
                           VersionNumber = s.VersionNumber
                       })
                       .OrderByDescending(o => o.VersionNumber).AsNoTracking().FirstOrDefaultAsync();
        }

        /// <summary>
        ///     Gets the default layout, including navigation menu.
        /// </summary><param name="layoutCache">Length of time to cache layout.</param>
        /// <returns>Returns a <see cref="LayoutViewModel"/>.</returns>
        /// <remarks>
        ///     <para>
        ///         Inserts a Bootstrap style nav bar where this '&lt;!--{COSMOS-UL-NAV}--&gt;' is placed in the
        ///         <see cref="LayoutViewModel.HtmlHeader" />.
        ///     </para>
        /// </remarks>
        public async Task<LayoutViewModel> GetDefaultLayout(TimeSpan? layoutCache = null)
        {
            if (memoryCache == null || layoutCache == null)
            {
                var entity = await DbContext.Layouts.AsNoTracking().FirstOrDefaultAsync(a => a.IsDefault);
                return new LayoutViewModel(entity);
            }

            memoryCache.TryGetValue("defLayout", out LayoutViewModel model);

            if (model == null)
            {
                var entity = await DbContext.Layouts.AsNoTracking().FirstOrDefaultAsync(a => a.IsDefault);
                DbContext.Entry(entity).State = EntityState.Detached;
                model = new LayoutViewModel(entity);
                memoryCache.Set("defLayout", model, layoutCache.Value);
            }

            return model;
        }

        /// <summary>
        /// Searches for articles.  This is a full text search.  This is a very expensive operation and should be used sparingly.
        /// </summary>
        /// <param name="text">Search text.</param>
        /// <returns>List of articles.</returns>
        public async Task<List<TableOfContentsItem>> Search(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return new List<TableOfContentsItem>();
            }

            text = text.ToLower();

            var dt = DateTimeOffset.UtcNow;
            var query = DbContext.Pages
                .Where(a => a.StatusCode == 0 && a.Published <= dt && (a.Content.ToLower().Contains(text) || a.Title.ToLower().Contains(text))).AsQueryable();

            var terms = text.Split(' ');

            if (terms.Length > 1)
            {
                foreach (var term in terms)
                {
                    query = query.Where(a => a.Content.ToLower().Contains(term) || a.Title.ToLower().Contains(term));
                }
            }

            query = query.OrderByDescending(o => o.Title);

            var results = await query.Select(s => new TableOfContentsItem
            {
                UrlPath = "/" + s.UrlPath,
                Title = s.Title,
                Published = s.Published.Value,
                Updated = s.Updated,
                BannerImage = "/" + s.BannerImage,
                AuthorInfo = s.AuthorInfo
            }).ToListAsync();

            return results.Select(s => new TableOfContentsItem()
            {
                AuthorInfo = s.AuthorInfo,
                BannerImage = string.IsNullOrEmpty(s.BannerImage) ? string.Empty : "/" + s.BannerImage,
                Published = s.Published,
                Title = s.Title,
                Updated = s.Updated,
                UrlPath = s.UrlPath == "/root" ? "/" : s.UrlPath
            }).ToList();
        }

        /// <summary>
        ///     This method creates an <see cref="ArticleViewModel" /> ready for display and edit.
        /// </summary>
        /// <param name="article">Article entity.</param>
        /// <param name="lang">Language acronym.</param>
        /// <param name="includeLayout">Include layout with request (default: true).</param>
        /// <returns>
        ///     <para>Returns <see cref="ArticleViewModel" /> that includes:</para>
        ///     <list type="bullet">
        ///         <item>
        ///             Current ArticleVersionInfo
        ///         </item>
        ///         <item>
        ///             If the site is in authoring or publishing mode (<see cref="ArticleViewModel.ReadWriteMode" />)
        ///         </item>
        ///     </list>
        /// </returns>
        protected async Task<ArticleViewModel> BuildArticleViewModel(Article article, string lang, bool includeLayout = true)
        {
            var author = string.Empty;
            if (!string.IsNullOrEmpty(article.UserId))
            {
                var authorInfo = await DbContext.AuthorInfos.AsNoTracking().FirstOrDefaultAsync(f => f.Id == article.UserId);
                if (authorInfo != null)
                {
                    author = JsonConvert.SerializeObject(authorInfo).Replace("\"", "'");
                }
            }

            return new ArticleViewModel
            {
                ArticleNumber = article.ArticleNumber,
                LanguageCode = lang,
                LanguageName = string.Empty,
                CacheDuration = 10,
                Content = article.Content,
                StatusCode = (StatusCodeEnum)article.StatusCode,
                Id = article.Id,
                Published = article.Published ?? null,
                Title = article.Title,
                UrlPath = article.UrlPath,
                Updated = article.Updated,
                VersionNumber = article.VersionNumber,
                HeadJavaScript = article.HeaderJavaScript,
                FooterJavaScript = article.FooterJavaScript,
                Layout = includeLayout ? await GetDefaultLayout() : null,
                ReadWriteMode = isEditor,
                Expires = article.Expires ?? null,
                BannerImage = article.BannerImage,
                AuthorInfo = author
            };
        }

        /// <summary>
        ///     This method creates an <see cref="ArticleViewModel" /> ready for display and edit.
        /// </summary>
        /// <param name="article">Published page.</param>
        /// <param name="lang">Language code.</param>
        /// <param name="layoutCache">Layout cache duration.</param>
        /// <param name="includeLayout">Include layout with request.</param>
        /// <returns>
        ///     <para>Returns <see cref="ArticleViewModel" /> that includes:</para>
        ///     <list type="bullet">
        ///         <item>
        ///             Current ArticleVersionInfo
        ///         </item>
        ///         <item>
        ///             If the site is in authoring or publishing mode (<see cref="ArticleViewModel.ReadWriteMode" />)
        ///         </item>
        ///     </list>
        /// </returns>
        protected async Task<ArticleViewModel> BuildArticleViewModel(PublishedPage article, string lang, TimeSpan? layoutCache = null, bool includeLayout = true)
        {
            return new ArticleViewModel
            {
                ArticleNumber = article.ArticleNumber,
                BannerImage = article.BannerImage,
                LanguageCode = lang,
                LanguageName = string.Empty,
                CacheDuration = 10,
                Content = article.Content,
                StatusCode = (StatusCodeEnum)article.StatusCode,
                Id = article.Id,
                Published = article.Published ?? null,
                Title = article.Title,
                UrlPath = article.UrlPath,
                Updated = article.Updated,
                VersionNumber = article.VersionNumber,
                HeadJavaScript = article.HeaderJavaScript,
                FooterJavaScript = article.FooterJavaScript,
                Layout = includeLayout ? await GetDefaultLayout(layoutCache) : null,
                ReadWriteMode = isEditor,
                Expires = article.Expires ?? null,
                AuthorInfo = article.AuthorInfo,
                OGDescription = string.Empty,
                OGImage = string.IsNullOrEmpty(article.BannerImage) ? string.Empty : article.BannerImage.StartsWith("http") ? article.BannerImage : publisherUrl.TrimEnd('/') + "/" + article.BannerImage.TrimStart('/'),
                OGUrl = GetOGUrl(article.UrlPath),
            };
        }

        private string GetOGUrl(string urlPath)
        {
            if (string.IsNullOrWhiteSpace(publisherUrl))
            {
                return urlPath;
            }

            return publisherUrl.TrimEnd('/') + "/" + urlPath.TrimStart('/');
        }
    }
}
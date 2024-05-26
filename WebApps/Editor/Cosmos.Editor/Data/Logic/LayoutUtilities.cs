// <copyright file="LayoutUtilities.cs" company="Cosmos Website Solutions, Inc.">
// Copyright (c) Cosmos Website Solutions, Inc.. All rights reserved.
// Licensed under the GNU Public License, Version 3.0 (https://www.gnu.org/licenses/gpl-3.0.html)
// See https://github.com/MoonriseSoftwareCalifornia/CosmosCMS
// for more information concerning the license and the contributors participating to this project.
// </copyright>

namespace Cosmos.Cms.Data.Logic
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.Linq;
    using System.Net.Http;
    using System.Text;
    using System.Threading.Tasks;
    using Cosmos.Common.Data;
    using Cosmos.Common.Data.Logic;
    using HtmlAgilityPack;
    using Newtonsoft.Json;

    /// <summary>
    /// Loads external layouts and associated page templates into Comsos CMS.
    /// </summary>
    /// <remarks>Layouts are loaded from here: <see href="https://cosmos-layouts.moonrise.net"/>.</remarks>
    public class LayoutUtilities
    {
        /// <summary>
        /// Default online catalog location.
        /// </summary>
        private const string COSMOSLAYOUTSREPO = "https://cosmos-layouts.moonrise.net";

        private string ParseAttributes(HtmlAttributeCollection collection)
        {
            if (collection == null)
            {
                return string.Empty;
            }

            var builder = new StringBuilder();
            foreach (var attribute in collection)
            {
                builder.Append($"{attribute.Name}=\"{attribute.Value}\" ");
            }

            return builder.ToString().Trim();
        }

        private async Task LoadCatalog()
        {
            using var client = new HttpClient();
            var data = await client.GetStringAsync($"{COSMOSLAYOUTSREPO}/catalog.json");
            CommunityCatalog = JsonConvert.DeserializeObject<Root>(data);
            if (CommunityCatalog != null && CommunityCatalog.LayoutCatalog != null && CommunityCatalog.LayoutCatalog.Any())
            {
                CommunityCatalog.LayoutCatalog = CommunityCatalog.LayoutCatalog.OrderBy(o => o.Name).ToList();
            }
        }

        /// <summary>
        /// Get template pages for a layout.
        /// </summary>
        /// <param name="id"></param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public async Task<List<Page>> GetPageTemplates(string id)
        {
            using var client = new HttpClient();
            var data = await client.GetStringAsync($"{COSMOSLAYOUTSREPO}/Layouts/{id}/catalog.json");
            var root = JsonConvert.DeserializeObject<PageRoot>(data);
            return root.Pages.OrderBy(o => o.Title).ToList();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="LayoutUtilities"/> class.
        /// Constructor.
        /// </summary>
        public LayoutUtilities()
        {
            var task = LoadCatalog();
            task.Wait();
        }

        /// <summary>
        /// Gets or sets default layout ID.
        /// </summary>
        /// <remarks>Default is the Bootstrap 5 Starter Template (bs5-strt).</remarks>
        public string DefaultLayoutId { get; set; } = "bs5-strt";

        /// <summary>
        /// Gets catalog of layouts.
        /// </summary>
        public Root CommunityCatalog { get; private set; }

        /// <summary>
        /// Loads the default community layout.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        /// <remarks>This is used when setting up Cosmos.</remarks>
        public async Task<Layout> GetDefaultCommunityLayout()
        {
            return await GetCommunityLayout(DefaultLayoutId, true);
        }

        /// <summary>
        /// Gets a specified layout.
        /// </summary>
        /// <param name="layoutId"></param>
        /// <param name="isDefault"></param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public async Task<Layout> GetCommunityLayout(string layoutId, bool isDefault)
        {
            var item = CommunityCatalog.LayoutCatalog.FirstOrDefault(f => f.Id == layoutId);
            using var client = new HttpClient();
            var url = $"{COSMOSLAYOUTSREPO}/Layouts/{layoutId}/layout.html";
            var html = await client.GetStringAsync(url);

            var layout = ParseHtml(html);
            layout.CommunityLayoutId = layoutId;
            layout.IsDefault = isDefault;
            layout.LayoutName = item.Name;
            layout.Notes = item.Description;

            return layout;
        }

        /// <summary>
        /// Creates a new layout from HTML.
        /// </summary>
        /// <param name="html"></param>
        /// <returns></returns>
        public Layout ParseHtml(string html)
        {
            var contentHtmlDocument = new HtmlDocument();
            contentHtmlDocument.LoadHtml(html);

            var head = contentHtmlDocument.DocumentNode.SelectSingleNode("//head");
            var body = contentHtmlDocument.DocumentNode.SelectSingleNode("//body");
            var bodyHeader = contentHtmlDocument.DocumentNode.SelectSingleNode("//body/cosmos-layout-header");
            var bodyFooter = contentHtmlDocument.DocumentNode.SelectSingleNode("//body/cosmos-layout-footer");

            var layout = new Layout()
            {
                IsDefault = false,
                CommunityLayoutId = string.Empty,
                LayoutName = string.Empty,
                Notes = string.Empty,
                Head = head?.InnerHtml,
                BodyHtmlAttributes = ParseAttributes(body?.Attributes),
                HtmlHeader = bodyHeader?.InnerHtml,
                FooterHtmlContent = bodyFooter?.InnerHtml
            };

            return layout;
        }

        /// <summary>
        /// Gets a page template.
        /// </summary>
        /// <param name="communityLayoutId"></param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public async Task<List<Template>> GetCommunityTemplatePages(string communityLayoutId = "")
        {
            var tempates = new List<Template>();

            if (string.IsNullOrEmpty(communityLayoutId))
            {
                communityLayoutId = DefaultLayoutId;
            }

            var pages = await GetPageTemplates(communityLayoutId);

            using var client = new HttpClient();

            foreach (var page in pages)
            {
                try
                {
                    var url = $"{COSMOSLAYOUTSREPO}/Layouts/{communityLayoutId}/{page.Path}";
                    var uglifiedUrl = NUglify.Uglify.Html(url);
                    var html = await client.GetStringAsync(uglifiedUrl.Code);

                    var template = ParseHtml<Template>(html);
                    template.PageType = page.Type;
                    template.Description = page.Description;
                    template.Title = page.Type == "home" ? "Home Page" : page.Title;
                    template.CommunityLayoutId = communityLayoutId;
                    tempates.Add(template);
                }
                catch (Exception e)
                {
                    var t = e; // Debugging
                    throw;
                }
            }

            return tempates.Distinct().ToList();
        }

        /// <summary>
        /// Gets all the layouts in the community catalog.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public async Task<List<Layout>> GetAllCommunityLayouts()
        {
            var layoutIds = CommunityCatalog.LayoutCatalog.Select(s => s.Id).ToList();
            var layouts = new List<Layout>();

            foreach (var id in layoutIds)
            {
                layouts.Add(await GetCommunityLayout(id, id == DefaultLayoutId));
            }

            return layouts.OrderBy(o => o.LayoutName).ToList();
        }

        /// <summary>
        /// Parses an HTML page and loads it as either a <see cref="Template"/> or an <see cref="Article"/>.
        /// </summary>
        /// <param name="html"></param>
        /// <returns>Body content of a page.</returns>
        /// <remarks>Template pages should NOT contain any layout components.</remarks>
        public T ParseHtml<T>(string html)
        {
            var contentHtmlDocument = new HtmlDocument();

            contentHtmlDocument.LoadHtml(html);

            // Remove layout elements
            var bodyHeader = contentHtmlDocument.DocumentNode.SelectSingleNode("//body/cosmos-layout-header");
            var bodyFooter = contentHtmlDocument.DocumentNode.SelectSingleNode("//body/cosmos-layout-footer");
            if (bodyHeader != null)
            {
                bodyHeader.Remove();
            }

            if (bodyFooter != null)
            {
                bodyFooter.Remove();
            }

            // Save what remains in the body
            var body = contentHtmlDocument.DocumentNode.SelectSingleNode("//body");

            object model = null;
            if (typeof(T) == typeof(Template))
            {
                model = new Template()
                {
                    Content = body.InnerHtml,
                    Description = string.Empty,
                    Title = string.Empty
                };
            }
            else if (typeof(T) == typeof(Article))
            {
                model = new Article()
                {
                    Content = body.InnerHtml,
                    Title = string.Empty,
                    StatusCode = (int)StatusCodeEnum.Active
                };
            }
            else
            {
                throw new Exception($"Type {typeof(T)} not supported for this operation.");
            }

            return (T)model;
        }
    }

    /// <summary>
    /// Template page.
    /// </summary>
    public class Page
    {
        /// <summary>
        /// Gets or sets template page title.
        /// </summary>
        public string Title { get; set; }

        /// <summary>
        /// Gets or sets type is either home or content.
        /// </summary>
        public string Type { get; set; }

        /// <summary>
        /// Gets or sets description of this page.
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Gets or sets path where page is located.
        /// </summary>
        public string Path { get; set; }
    }

    /// <summary>
    /// Layout catalog item.
    /// </summary>
    public class LayoutCatalogItem
    {
        /// <summary>
        /// Gets or sets unique ID of the layout.
        /// </summary>
        [Key]
        public string Id { get; set; }

        /// <summary>
        /// Gets or sets layout name.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets description or notes regarding layout.
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Gets or sets license for this layout.
        /// </summary>
        public string License { get; set; }
    }

    /// <summary>
    /// Catalog feed root.
    /// </summary>
    public class Root
    {
        /// <summary>
        /// Gets or sets layout catalog.
        /// </summary>
        public List<LayoutCatalogItem> LayoutCatalog { get; set; }
    }

    /// <summary>
    /// Pages used with layout.
    /// </summary>
    public class PageRoot
    {
        /// <summary>
        /// Gets or sets template pages that can be used with this layout.
        /// </summary>
        public List<Page> Pages { get; set; }
    }
}

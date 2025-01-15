// <copyright file="DesignerConfig.cs" company="Moonrise Software, LLC">
// Copyright (c) Moonrise Software, LLC. All rights reserved.
// Licensed under the GNU Public License, Version 3.0 (https://www.gnu.org/licenses/gpl-3.0.html)
// See https://github.com/MoonriseSoftwareCalifornia/CosmosCMS
// for more information concerning the license and the contributors participating to this project.
// </copyright>

using System.Collections.Generic;
using System.Linq;
using Cosmos.Common.Data;
using Cosmos.Editor.Data.Logic;

namespace Cosmos.Editor.Models.GrapesJs
{
    /// <summary>
    /// Designer configuration.
    /// </summary>
    public class DesignerConfig
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DesignerConfig"/> class.
        /// </summary>
        public DesignerConfig()
        {
            Styles = new List<string>();
            Scripts = new List<string>();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DesignerConfig"/> class.
        /// </summary>
        /// <param name="layout">Layout object.</param>
        /// <param name="id">Layout, template, or article ID.</param>
        /// <param name="title">Layout, template, or article title.</param>
        public DesignerConfig(Layout layout, string id, string title)
        {
            var designerUtils = new DesignerUtilities();
            Styles = new List<string>();
            Scripts = new List<string>();
            ImageAssets = new List<string>();
            Plugins = new List<DesignerPlugin>();

            Styles.AddRange(designerUtils.ExtractStyleReferences(layout.Head));
            Scripts.AddRange(designerUtils.ExtractScriptReferences(layout.Head));
            Scripts.AddRange(designerUtils.ExtractScriptReferences(layout.FooterHtmlContent));

            //if (Styles.Any(a => a.Contains("bootstrap", System.StringComparison.OrdinalIgnoreCase) && a.Contains("5")))
            //{
            //    Plugins.Add(new DesignerPlugin
            //    {
            //        Name = "grapesjs-blocks-bootstrap-5",
            //        Url = "/lib/grapesjs-blocks-bootstrap-5/grapesjs-blocks-bootstrap-5.min.js",
            //        Options = "'grapesjs-blocks-bootstrap-5': { blocks: {}, blockCategories: {}, labels: {}, gridDevicesPanel: true, formPredefinedActions: [ {name: 'Contact', value: '/contact'}, {name: 'landing', value: '/landing'}, ] }"
            //    });
            //}
            //else if (Styles.Any(a => a.Contains("bootstrap", System.StringComparison.OrdinalIgnoreCase) && a.Contains("4")))
            //{
            //    Plugins.Add(new DesignerPlugin
            //    {
            //        Name = "grapesjs-blocks-bootstrap4",
            //        Url = "/lib/grapesjs-blocks-bootstrap4/grapesjs-blocks-bootstrap4.min.js",
            //        Options = "'grapesjs-blocks-bootstrap4': { blocks: {}, blockCategories: {}, labels: {}, gridDevicesPanel: true, formPredefinedActions: [ {name: 'Contact', value: '/contact'}, {name: 'landing', value: '/landing'}, ] }"
            //    });
            //}

            if (layout.Head.Contains("cdn.tailwindcss.com", System.StringComparison.OrdinalIgnoreCase))
            {
                Plugins.Add(new DesignerPlugin
                {
                    Name = "grapesjs-tailwind",
                    Url = "https://unpkg.com/grapesjs-tailwind",
                    Options = "'grapesjs-tailwind': { }"
                });
            }

            Id = id;
            Title = title;
        }

        /// <summary>
        /// Gets or sets the ID.
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// Gets or sets the HTML content.
        /// </summary>
        public string HtmlContent { get; set; }

        /// <summary>
        /// Gets or sets the CSS content.
        /// </summary>

        public string CssContent { get; set; }

        /// <summary>
        /// Gets or sets the item title.
        /// </summary>
        public string Title { get; set; }

        /// <summary>
        /// Gets or sets the project image assets.
        /// </summary>
        public List<string> ImageAssets { get; set; }

        /// <summary>
        /// Gets or sets the project style URLs.
        /// </summary>
        public List<string> Styles { get; set; }

        /// <summary>
        /// Gets or sets the project script URLs.
        /// </summary>
        public List<string> Scripts { get; set; }

        /// <summary>
        /// Gets or sets the designer plugins.
        /// </summary>
        public List<DesignerPlugin> Plugins { get; set; }
    }
}

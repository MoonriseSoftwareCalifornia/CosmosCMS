// <copyright file="DesignerConfig.cs" company="Moonrise Software, LLC">
// Copyright (c) Moonrise Software, LLC. All rights reserved.
// Licensed under the GNU Public License, Version 3.0 (https://www.gnu.org/licenses/gpl-3.0.html)
// See https://github.com/MoonriseSoftwareCalifornia/CosmosCMS
// for more information concerning the license and the contributors participating to this project.
// </copyright>

namespace Cosmos.Editor.Models.GrapesJs
{
    using System;
    using System.Collections.Generic;
    using Cosmos.Common.Data;
    using Cosmos.Editor.Data.Logic;

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
        /// <param name="id">Item ID.</param>
        public DesignerConfig(Layout layout, Guid id)
        {
            var designerUtils = new DesignerUtilities();
            Styles = new List<string>();
            Scripts = new List<string>();

            Styles.AddRange(designerUtils.ExtractStyleReferences(layout.Head));
            Scripts.AddRange(designerUtils.ExtractScriptReferences(layout.Head));
            Scripts.AddRange(designerUtils.ExtractScriptReferences(layout.FooterHtmlContent));

            Id = id;
        }

        /// <summary>
        /// Gets or sets the ID.
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// Gets or sets the HTML content.
        /// </summary>
        public string HtmlContent { get; set; }

        /// <summary>
        /// Gets or sets the CSS content.
        /// </summary>

        public string CssContent { get; set; }

        /// <summary>
        /// Gets or sets the project style URLs.
        /// </summary>
        public List<string> Styles { get; set; }

        /// <summary>
        /// Gets or sets the project script URLs.
        /// </summary>
        public List<string> Scripts { get; set; }
    }
}

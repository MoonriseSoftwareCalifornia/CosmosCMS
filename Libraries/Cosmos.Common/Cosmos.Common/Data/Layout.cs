// <copyright file="Layout.cs" company="Moonrise Software, LLC">
// Copyright (c) Moonrise Software, LLC. All rights reserved.
// Licensed under the GNU Public License, Version 3.0 (https://www.gnu.org/licenses/gpl-3.0.html)
// See https://github.com/MoonriseSoftwareCalifornia/CosmosCMS
// for more information concerning the license and the contributors participating to this project.
// </copyright>

using System;
using System.ComponentModel.DataAnnotations;

namespace Cosmos.Common.Data
{
    /// <summary>
    ///     Website layout content.
    /// </summary>
    [Serializable]
    public class Layout
    {
        /// <summary>
        /// Gets or sets identity key for this entity.
        /// </summary>
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        /// <summary>
        /// Gets or sets the community layout ID.
        /// </summary>
        [Display(Name = "Community Layout Id")]
        public string CommunityLayoutId { get; set; }

        /// <summary>
        ///     Gets or sets a value indicating whether if true this is the default layout for website.
        /// </summary>
        public bool IsDefault { get; set; } = false;

        /// <summary>
        ///     Gets or sets friendly name of layout.
        /// </summary>
        [Display(Name = "Layout Name")]
        [StringLength(128)]
        public string LayoutName { get; set; }

        /// <summary>
        ///     Gets or sets notes about the layout.
        /// </summary>
        [Display(Name = "Notes")]
        [DataType(DataType.Html)]
        public string Notes { get; set; }

        /// <summary>
        ///     Gets or sets content injected into the web page HEAD.
        /// </summary>
        [Display(Name = "HEAD Content")]
        [DataType(DataType.Html)]
        public string Head { get; set; }

        /// <summary>
        ///     Gets or sets body tag attributes.
        /// </summary>
        [Display(Name = "BODY Html Attributes", GroupName = "Body")]
        [StringLength(256)]
        public string BodyHtmlAttributes { get; set; }

        /// <summary>
        ///     Gets or sets web page header content.
        /// </summary>
        [Display(Name = "Header Html Content", GroupName = "Header")]
        [DataType(DataType.Html)]
        public string HtmlHeader { get; set; }

        /// <summary>
        ///     Gets or sets content injected into the web site footer.
        /// </summary>
        [Display(Name = "Footer Html Content", GroupName = "Footer")]
        [DataType(DataType.Html)]
        public string FooterHtmlContent { get; set; }
    }
}
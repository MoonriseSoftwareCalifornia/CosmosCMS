// <copyright file="LayoutViewModel.cs" company="Moonrise Software, LLC">
// Copyright (c) Moonrise Software, LLC. All rights reserved.
// Licensed under the GNU Public License, Version 3.0 (https://www.gnu.org/licenses/gpl-3.0.html)
// See https://github.com/MoonriseSoftwareCalifornia/CosmosCMS
// for more information concerning the license and the contributors participating to this project.
// </copyright>

using Cosmos.Common.Data;
using System;
using System.ComponentModel.DataAnnotations;
using System.Web;

namespace Cosmos.Common.Models
{
    /// <summary>
    ///     VSiew model used on layout list page.
    /// </summary>
    [Serializable]
    public class LayoutViewModel
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="LayoutViewModel"/> class.
        /// </summary>
        public LayoutViewModel()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="LayoutViewModel"/> class.
        /// </summary>
        /// <param name="layout">Base layout entity.</param>
        public LayoutViewModel(Layout layout)
        {
            if (layout != null)
            {
                Id = layout.Id;
                LayoutName = layout.LayoutName;
                Notes = layout.Notes;
                Head = layout.Head;
                HtmlHeader = layout.HtmlHeader;
                FooterHtmlContent = layout.FooterHtmlContent;
            }
        }

        /// <summary>
        ///     Gets or sets identity key of the entity.
        /// </summary>
        [Key]
        public Guid Id { get; set; }

        /// <summary>
        ///     Gets or sets a value indicating whether indicates if this is the default layout of the site.
        /// </summary>
        [Display(Name = "Is default layout?")]
        public bool IsDefault { get; set; } = false;

        /// <summary>
        ///     Gets or sets friendly name of layout.
        /// </summary>
        [Display(Name = "Layout Name")]
        [StringLength(128)]
        public string LayoutName { get; set; }

        /// <summary>
        ///     Gets or sets notes regarding this layout.
        /// </summary>
        [Display(Name = "Notes")]
        [DataType(DataType.Html)]
        public string Notes { get; set; }

        /// <summary>
        ///     Gets or sets content injected into the head tag.
        /// </summary>
        [Display(Name = "HEAD Content")]
        [DataType(DataType.Html)]
        public string Head { get; set; }

        /// <summary>
        ///     Gets or sets content injected into page header.
        /// </summary>
        [Display(Name = "Header Html Content", GroupName = "Header")]
        [DataType(DataType.Html)]
        public string HtmlHeader { get; set; }

        /// <summary>
        ///     Gets or sets content injected into the page footer.
        /// </summary>
        [Display(Name = "Footer Html Content", GroupName = "Footer")]
        [DataType(DataType.Html)]
        public string FooterHtmlContent { get; set; }

        /// <summary>
        ///     Gets a detached entity.
        /// </summary>
        /// <param name="decode">Decode layout or not (default = false).</param>
        /// <returns>Gets a <see cref="Layout"/>.</returns>
        public Layout GetLayout(bool decode = false)
        {
            if (decode)
            {
                return new Layout
                {
                    Id = Id,
                    IsDefault = IsDefault,
                    LayoutName = LayoutName,
                    Notes = HttpUtility.HtmlDecode(Notes),
                    Head = HttpUtility.HtmlDecode(Head),
                    HtmlHeader = HttpUtility.HtmlDecode(HtmlHeader),
                    FooterHtmlContent = HttpUtility.HtmlDecode(FooterHtmlContent)
                };
            }

            return new Layout
            {
                Id = Id,
                IsDefault = IsDefault,
                LayoutName = LayoutName,
                Notes = Notes,
                Head = Head,
                HtmlHeader = HtmlHeader,
                FooterHtmlContent = FooterHtmlContent
            };
        }
    }
}
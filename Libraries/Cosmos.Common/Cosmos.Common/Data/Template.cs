// <copyright file="Template.cs" company="Moonrise Software, LLC">
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
    ///     A page template.
    /// </summary>
    public class Template
    {
        /// <summary>
        ///     Gets or sets identity key for this entity.
        /// </summary>
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        /// <summary>
        /// Gets or sets the layout ID.
        /// </summary>
        [Display(Name = "Layout ID")]
        public Guid? LayoutId { get; set; }

        /// <summary>
        /// Gets or sets the community layout ID.
        /// </summary>
        [Display(Name = "Community Layout Id")]
        public string CommunityLayoutId { get; set; }

        /// <summary>
        ///     Gets or sets friendly name or title of this page template.
        /// </summary>
        [Display(Name = "Template Title")]
        [StringLength(128)]
        public string Title { get; set; }

        /// <summary>
        ///     Gets or sets description or notes about how to use this template.
        /// </summary>
        [Display(Name = "Description/Notes")]
        public string Description { get; set; }

        /// <summary>
        ///     Gets or sets the HTML content of this page template.
        /// </summary>
        [Display(Name = "HTML Content")]
        [DataType(DataType.Html)]
        public string Content { get; set; }

        /// <summary>
        ///    Gets or sets the template page type.
        /// </summary>
        /// <remarks>
        /// This is either 'home' or 'content'.
        /// </remarks>
        [Display(Name = "Page Type")]
        public string PageType { get; set; }
    }
}
// <copyright file="ArticleListItem.cs" company="Moonrise Software, LLC">
// Copyright (c) Moonrise Software, LLC. All rights reserved.
// Licensed under the GNU Public License, Version 3.0 (https://www.gnu.org/licenses/gpl-3.0.html)
// See https://github.com/MoonriseSoftwareCalifornia/CosmosCMS
// for more information concerning the license and the contributors participating to this project.
// </copyright>

namespace Cosmos.Cms.Models
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;

    /// <summary>
    ///     Article list item used primarily on page list page.
    /// </summary>
    public class ArticleListItem
    {
        /// <summary>
        ///     Gets or sets a value indicating whether indicates if this is the "Home" page.
        /// </summary>
        [Display(Name = "Is home page?")]
        public bool IsDefault { get; set; } = false;

        /// <summary>
        ///     Gets or sets article number.
        /// </summary>
        [Display(Name = "Article#")]
        [Key]
        public int ArticleNumber { get; set; }

        /// <summary>
        ///     Gets or sets version number of the article number.
        /// </summary>
        [Display(Name = "Version")]
        public int VersionNumber { get; set; }

        /// <summary>
        ///     Gets or sets title of the page, also used as the basis for the URL.
        /// </summary>
        [Display(Name = "Title")]
        public string Title { get; set; }

        /// <summary>
        ///     Gets or sets disposition of the page.
        /// </summary>
        [Display(Name = "Status")]
        public string Status { get; set; }

        /// <summary>
        ///     Gets date/time of when this page was last updated.
        /// </summary>
        [Display(Name = "Updated")]
        public DateTimeOffset Updated { get; internal set; }

        /// <summary>
        ///     Gets or sets date and time of when this item was published, and made public.
        /// </summary>
        [Display(Name = "Publish date/time")]
        public DateTimeOffset? LastPublished { get; set; }

        /// <summary>
        ///     Gets or sets url of this item.
        /// </summary>
        [Display(Name = "Url")]
        public string UrlPath { get; set; }

        /// <summary>
        /// Gets or sets a list of who has view permissions on the public website.
        /// </summary>
        [Display(Name = "Permissions")]
        public List<string> Permissions { get; set; } = new List<string>();
    }
}
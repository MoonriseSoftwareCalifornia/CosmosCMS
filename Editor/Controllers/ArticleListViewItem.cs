// <copyright file="ArticleListViewItem.cs" company="Moonrise Software, LLC">
// Copyright (c) Moonrise Software, LLC. All rights reserved.
// Licensed under the GNU Public License, Version 3.0 (https://www.gnu.org/licenses/gpl-3.0.html)
// See https://github.com/MoonriseSoftwareCalifornia/CosmosCMS
// for more information concerning the license and the contributors participating to this project.
// </copyright>

namespace Sky.Cms.Controllers
{
    using System;

    /// <summary>
    /// Represents a single article in the list view.
    /// </summary>
    public class ArticleListViewItem
    {
        /// <summary>
        /// Gets or sets the article number.
        /// </summary>
        public int ArticleNumber { get; set; }

        /// <summary>
        /// Gets or sets the article title.
        /// </summary>
        public string Title { get; set; }

        /// <summary>
        /// Gets or sets the article URL path.
        /// </summary>
        public string UrlPath { get; set; }

        /// <summary>
        /// Gets or sets the article published date.
        /// </summary>
        public DateTimeOffset? Published { get; set; }

        /// <summary>
        /// Gets or sets the article updated date.
        /// </summary>
        public DateTimeOffset Updated { get; set; }
    }
}
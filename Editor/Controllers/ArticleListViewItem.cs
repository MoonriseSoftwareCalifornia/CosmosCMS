// <copyright file="EditorController.cs" company="Moonrise Software, LLC">
// Copyright (c) Moonrise Software, LLC. All rights reserved.
// Licensed under the GNU Public License, Version 3.0 (https://www.gnu.org/licenses/gpl-3.0.html)
// See https://github.com/MoonriseSoftwareCalifornia/CosmosCMS
// for more information concerning the license and the contributors participating to this project.
// </copyright>

using System;

namespace Cosmos.Cms.Controllers
{
    /// <summary>
    /// Represents a single article in the list view.
    /// </summary>
    internal class ArticleListViewItem
    {
        /// <summary>
        /// Gets or sets the article number.
        /// </summary>
        internal int ArticleNumber { get; set; }

        /// <summary>
        /// Gets or sets the article status.
        /// </summary>
        internal string Status { get; set; }

        /// <summary>
        /// Gets or sets the article title.
        /// </summary>
        internal string Title { get; set; }

        /// <summary>
        /// Gets or sets the article URL path.
        /// </summary>
        internal string UrlPath { get; set; }

        /// <summary>
        /// Gets or sets the article published date.
        /// </summary>
        internal DateTimeOffset? Published { get; set; }

        /// <summary>
        /// Gets or sets the article updated date.
        /// </summary>
        internal DateTimeOffset Updated { get; set; }
    }
}
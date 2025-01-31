// <copyright file="TableOfContentsItem.cs" company="Moonrise Software, LLC">
// Copyright (c) Moonrise Software, LLC. All rights reserved.
// Licensed under the GNU Public License, Version 3.0 (https://www.gnu.org/licenses/gpl-3.0.html)
// See https://github.com/MoonriseSoftwareCalifornia/CosmosCMS
// for more information concerning the license and the contributors participating to this project.
// </copyright>

namespace Cosmos.Common.Models
{
    using System;

    /// <summary>
    /// Table of Contents (TOC) Item.
    /// </summary>
    public class TableOfContentsItem
    {
        /// <summary>
        /// Gets or sets uRL Path to page.
        /// </summary>
        public string UrlPath { get; set; }

        /// <summary>
        /// Gets or sets title of page.
        /// </summary>
        public string Title { get; set; }

        /// <summary>
        /// Gets or sets published date and time.
        /// </summary>
        public DateTimeOffset Published { get; set; }

        /// <summary>
        /// Gets or sets when last updated.
        /// </summary>
        public DateTimeOffset Updated { get; set; }

        /// <summary>
        /// Gets or sets banner or preview image.
        /// </summary>
        public string BannerImage { get; set; }

        /// <summary>
        /// Gets or sets author name.
        /// </summary>
        public string AuthorInfo { get; set; }

        /// <summary>
        /// Gets or sets page content.
        /// </summary>
        /// <remarks>Not stored in the database.</remarks>
        public string Content { get; set; }
    }
}

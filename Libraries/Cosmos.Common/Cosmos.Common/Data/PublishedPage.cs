// <copyright file="PublishedPage.cs" company="Cosmos Website Solutions, Inc.">
// Copyright (c) Cosmos Website Solutions, Inc.. All rights reserved.
// Licensed under the GNU Public License, Version 3.0 (https://www.gnu.org/licenses/gpl-3.0.html)
// See https://github.com/MoonriseSoftwareCalifornia/CosmosCMS
// for more information concerning the license and the contributors participating to this project.
// </copyright>

namespace Cosmos.Common.Data
{
    using System;
    using System.ComponentModel.DataAnnotations;
    using Cosmos.Common.Data.Logic;

    /// <summary>
    /// Published article or page.
    /// </summary>
    public class PublishedPage
    {
        /// <summary>
        ///     Gets or sets unique article entity primary key number (not to be confused with article number).
        /// </summary>
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        /// <summary>
        ///     Gets or sets article number.
        /// </summary>
        /// <remarks>An article number.</remarks>
        public int ArticleNumber { get; set; }

        /// <summary>
        ///     Gets or sets status of this article.
        /// </summary>
        /// <remarks>See <see cref="StatusCodeEnum" /> enum for code numbers.</remarks>
        public int StatusCode { get; set; } = 0;

        /// <summary>
        ///     Gets or sets this is the URL of the article.
        /// </summary>
        [MaxLength(128)]
        [StringLength(128)]
        public string UrlPath { get; set; }

        /// <summary>
        /// Gets or sets the parent URL page.
        /// </summary>
        [Display(Name = "Parent URL Path")]
        public string ParentUrlPath { get; set; }

        /// <summary>
        ///     Gets or sets version number of the article.
        /// </summary>
        [Display(Name = "Article version")]
        public int VersionNumber { get; set; }

        /// <summary>
        ///     Gets or sets date/time of when this article is published.
        /// </summary>
        /// <remarks>Null value here means this article is not published.</remarks>
        [Display(Name = "Publish on (UTC):")]
        [DataType(DataType.DateTime)]
        public DateTimeOffset? Published { get; set; }

        /// <summary>
        ///     Gets or sets if set, is the date/time when this version of the article expires.
        /// </summary>
        [Display(Name = "Expires on (UTC):")]
        [DataType(DataType.DateTime)]
        public DateTimeOffset? Expires { get; set; }

        /// <summary>
        ///     Gets or sets title of the article.
        /// </summary>
        [MaxLength(254)]
        [StringLength(254)]
        [Display(Name = "Article title")]
        public string Title { get; set; }

        /// <summary>
        ///     Gets or sets hTML content of the page.
        /// </summary>
        [DataType(DataType.Html)]
        public string Content { get; set; }

        /// <summary>
        ///     Gets or sets date/time of when this article was last updated.
        /// </summary>
        [Display(Name = "Article last saved")]
        public DateTimeOffset Updated { get; set; } = DateTimeOffset.UtcNow;

        /// <summary>
        /// Gets or sets article banner image.
        /// </summary>
        [DataType(DataType.ImageUrl)]
        [Required(AllowEmptyStrings = true)]
        public string BannerImage { get; set; }

        /// <summary>
        ///     Gets or sets javaScript injected into the header of the web page.
        /// </summary>
        [DataType(DataType.Html)]
        public string HeaderJavaScript { get; set; }

        /// <summary>
        ///     Gets or sets javaScript injected into the footer of the web page.
        /// </summary>
        [DataType(DataType.Html)]
        public string FooterJavaScript { get; set; }

        /// <summary>
        /// Gets or sets information about the person who authored this document.
        /// </summary>
        public string AuthorInfo { get; set; }
    }
}

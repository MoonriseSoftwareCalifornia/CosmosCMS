// <copyright file="ArticleViewModel.cs" company="Moonrise Software, LLC">
// Copyright (c) Moonrise Software, LLC. All rights reserved.
// Licensed under the GNU Public License, Version 3.0 (https://www.gnu.org/licenses/gpl-3.0.html)
// See https://github.com/MoonriseSoftwareCalifornia/CosmosCMS
// for more information concerning the license and the contributors participating to this project.
// </copyright>

namespace Cosmos.Common.Models
{
    using System;
    using System.ComponentModel.DataAnnotations;
    using Cosmos.Common.Data.Logic;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Extensions.Options;

    /// <summary>
    ///     Article view model, used to display content on a web page.
    /// </summary>
    [Serializable]
    public class ArticleViewModel
    {
        /// <summary>
        ///     Gets or sets entity key for the article.
        /// </summary>
        [Key]
        public Guid Id { get; set; }

        /// <summary>
        ///     Gets or sets status code of the article.
        /// </summary>
        public StatusCodeEnum StatusCode { get; set; }

        /// <summary>
        ///     Gets or sets article number.
        /// </summary>
        public int ArticleNumber { get; set; }

        /// <summary>
        ///     Gets or sets iSO language code of this article.
        /// </summary>
        public string LanguageCode { get; set; } = "en";

        /// <summary>
        ///     Gets or sets language name.
        /// </summary>
        public string LanguageName { get; set; } = "English";

        /// <summary>
        ///     Gets or sets url of this page.
        /// </summary>
        [MaxLength(128)]
        [StringLength(128)]
        public string UrlPath { get; set; }

        /// <summary>
        ///     Gets or sets version number of this article.
        /// </summary>
        [Display(Name = "Article version")]
        public int VersionNumber { get; set; }

        /// <summary>
        ///     Gets or sets article title.
        /// </summary>
        [MaxLength(80)]
        [StringLength(80)]
        [Display(Name = "Article title")]
        [Remote("CheckTitle", "Edit", AdditionalFields = "ArticleNumber")]
        public string Title { get; set; }

        /// <summary>
        ///     Gets or sets hTML Content of the page.
        /// </summary>
        [DataType(DataType.Html)]
        public string Content { get; set; }

        /// <summary>
        /// Gets or sets javaScript block injected into HEAD for this particular page (article).
        /// </summary>
        [DataType(DataType.Html)]
        public string HeadJavaScript { get; set; }

        /// <summary>
        ///     Gets or sets javaScript block injected into the footer.
        /// </summary>
        [DataType(DataType.Html)]
        public string FooterJavaScript { get; set; }

        /// <summary>
        ///     Gets or sets layout used by this page.
        /// </summary>
        public LayoutViewModel Layout { get; set; }

        /// <summary>
        ///     Gets or sets date and time of when this article was last updated.
        /// </summary>
        [Display(Name = "Article last saved")]
        public virtual DateTimeOffset Updated { get; set; }

        /// <summary>
        ///    Gets or sets information about the editor or author who created the article.
        /// </summary>
        [Display(Name = "Author information")]
        public virtual string AuthorInfo { get; set; } = string.Empty;

        /// <summary>
        ///     Gets or sets date and time of when this was published.
        /// </summary>
        [Display(Name = "Publish on date/time (PST):")]
        [DataType(DataType.DateTime)]
        [DateTimeUtcKind]
        public virtual DateTimeOffset? Published { get; set; }

        /// <summary>
        ///     Gets or sets if set, is the date/time when this version of the article expires.
        /// </summary>
        /// <remarks>
        ///     This is calculated based on either this article's expiration date, or, the default cache duration set as a <see cref="DateTimeOffset"/>.
        /// </remarks>
        [Display(Name = "Expires on (UTC):")]
        [DataType(DataType.DateTime)]
        public virtual DateTimeOffset? Expires { get; set; }

        /// <summary>
        ///     Gets or sets a value indicating whether indicates if this is in authoring (true) or publishing (false) mode, Default is false.
        /// </summary>
        /// <remarks>
        ///     Is the value set by SiteSettings.ReadWriteMode which
        ///     is set in Startup and injected into controllers using <see cref="IOptions{TOptions}" />.
        /// </remarks>
        public bool ReadWriteMode { get; set; } = false;

        /// <summary>
        ///     Gets or sets a value indicating whether indicates is page is in preview model. Default is false.
        /// </summary>
        public bool PreviewMode { get; set; } = false;

        /// <summary>
        ///     Gets or sets a value indicating whether indicates if page is in edit, or authoring mode. Default is false.
        /// </summary>
        public bool EditModeOn { get; set; } = false;

        /// <summary>
        ///     Gets or sets cache during for this article.
        /// </summary>
        /// <remarks>
        /// Calculated using the expires value (if present).
        /// </remarks>
        public int CacheDuration { get; set; } = 0;

        /// <summary>
        /// Gets or sets article banner image.
        /// </summary>
        public string BannerImage { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the open graph image.
        /// </summary>
        public string OGImage { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the open graph description.
        /// </summary>
        public string OGDescription { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the open graph URL.
        /// </summary>
        public string OGUrl { get; set; } = string.Empty;
    }
}
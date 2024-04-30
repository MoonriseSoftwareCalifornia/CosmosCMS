// <copyright file="HtmlEditorViewModel.cs" company="Cosmos Website Solutions, Inc.">
// Copyright (c) Cosmos Website Solutions, Inc.. All rights reserved.
// Licensed under the GNU Public License, Version 3.0 (https://www.gnu.org/licenses/gpl-3.0.html)
// See https://github.com/MoonriseSoftwareCalifornia/CosmosCMS
// for more information concerning the license and the contributors participating to this project.
// </copyright>

namespace Cosmos.Cms.Models
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using Cosmos.Cms.Data;
    using Cosmos.Common.Data;
    using Cosmos.Common.Models;
    using Microsoft.AspNetCore.Mvc;

    /// <summary>
    ///     Article edit model returned when an article has been saved.
    /// </summary>
    public class HtmlEditorViewModel
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="HtmlEditorViewModel"/> class.
        /// </summary>
        public HtmlEditorViewModel()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="HtmlEditorViewModel"/> class.
        /// </summary>
        /// <param name="model">Article view model.</param>
        /// <param name="catalogEntry">Article catalog entry.</param>
        public HtmlEditorViewModel(ArticleViewModel model, CatalogEntry catalogEntry)
        {
            Id = model.Id;
            ArticleNumber = model.ArticleNumber;
            ArticlePermissions = catalogEntry.ArticlePermissions;
            UrlPath = model.UrlPath;
            VersionNumber = model.VersionNumber;
            this.Published = model.Published;
            Title = model.Title;
            Content = model.Content;
            Updated = model.Updated;
            BannerImage = model.BannerImage;
        }

        /// <summary>
        ///     Gets or sets entity key for the article.
        /// </summary>
        [Key]
        public Guid Id { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether when saving, save as new version.
        /// </summary>
        public bool SaveAsNewVersion { get; set; } = false;

        /// <summary>
        ///     Gets or sets article number.
        /// </summary>
        public int ArticleNumber { get; set; }

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
        [ArticleTitleValidation]
        [Display(Name = "Article title")]
        [Remote("CheckTitle", "Edit", AdditionalFields = "ArticleNumber")]
        public string Title { get; set; }

        /// <summary>
        ///     Gets or sets hTML Content of the page.
        /// </summary>
        [DataType(DataType.Html)]
        public string Content { get; set; }

        /// <summary>
        ///     Gets or sets roles allowed to view this page.
        /// </summary>
        /// <remarks>If this value is null, it assumes page can be viewed anonymously.</remarks>
        public List<ArticlePermission> ArticlePermissions { get; set; }

        /// <summary>
        ///     Gets or sets date and time of when this was published.
        /// </summary>
        [Display(Name = "Publish on date/time (PST):")]
        [DataType(DataType.DateTime)]
        [DateTimeUtcKind]
        public DateTimeOffset? Published { get; set; }

        /// <summary>
        ///     Gets or sets date and time of when this was updated.
        /// </summary>
        [Display(Name = "Publish on date/time (PST):")]
        [DataType(DataType.DateTime)]
        [DateTimeUtcKind]
        public DateTimeOffset? Updated { get; set; }

        /// <summary>
        /// Gets article banner image.
        /// </summary>
        public string BannerImage { get; private set; }

        /// <summary>
        /// Gets or sets a value indicating whether update existing version.
        /// </summary>
        public bool UpdateExisting { get; set; } = true;
    }
}
// <copyright file="EditCodeViewModel.cs" company="Moonrise Software, LLC">
// Copyright (c) Moonrise Software, LLC. All rights reserved.
// Licensed under the GNU Public License, Version 3.0 (https://www.gnu.org/licenses/gpl-3.0.html)
// See https://github.com/MoonriseSoftwareCalifornia/CosmosCMS
// for more information concerning the license and the contributors participating to this project.
// </copyright>

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Cosmos.Cms.Data;
using Cosmos.Cms.Models.Interfaces;
using Cosmos.Common.Data;
using Cosmos.Common.Models;

namespace Cosmos.Cms.Models
{
    /// <summary>
    /// Edit code post view model.
    /// </summary>
    public class EditCodePostModel : ICodeEditorViewModel
    {
        /// <summary>
        /// Gets or sets article ID.
        /// </summary>
        [Key]
        public Guid Id { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether when saving, save as new version.
        /// </summary>
        public bool SaveAsNewVersion { get; set; } = false;

        /// <summary>
        /// Gets or sets article number.
        /// </summary>
        public int ArticleNumber { get; set; }

        /// <summary>
        /// Gets or sets version number.
        /// </summary>
        public int VersionNumber { get; set; }

        /// <summary>
        /// Gets or sets title. 
        /// </summary>
        [MaxLength(80)]
        [StringLength(80)]
        [ArticleTitleValidation]
        [Display(Name = "Article title")]
        public string Title { get; set; }

        /// <summary>
        ///     Gets or sets roles allowed to view this page.
        /// </summary>
        /// <remarks>If this value is null, it assumes page can be viewed anonymously.</remarks>
        public string RoleList { get; set; }

        /// <summary>
        ///     Gets or sets date and time of when this was published. 
        /// </summary>
        [Display(Name = "Publish on date/time (PST):")]
        [DataType(DataType.DateTime)]
        [DateTimeUtcKind]
        public virtual DateTimeOffset? Published { get; set; }

        /// <summary>
        ///     Gets or sets date and time of when this was updated.
        /// </summary>
        [Display(Name = "Publish on date/time (PST):")]
        [DataType(DataType.DateTime)]
        [DateTimeUtcKind]
        public virtual DateTimeOffset? Updated { get; set; }

        /// <summary>
        /// Gets or sets content within the BODY tag.
        /// </summary>
        [DataType(DataType.Html)]
        public string Content { get; set; }

        /// <summary>
        /// Gets or sets content injected into the HEAD tag.
        /// </summary>
        [DataType(DataType.Html)]
        public string HeadJavaScript { get; set; }

        /// <summary>
        /// Gets or sets content injected just above the closing BODY tag below the Content inject.
        /// </summary>
        [DataType(DataType.Html)]
        public string FooterJavaScript { get; set; }

        /// <summary>
        /// Gets or sets code editor mode.
        /// </summary>
        public EditorMode EditorMode { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether content is valid and OK to save.
        /// </summary>
        public bool IsValid { get; set; }

        /// <summary>
        /// Gets or sets editing field.
        /// </summary>
        public string EditingField { get; set; }

        /// <summary>
        /// Gets or sets editor title (different than Article title).
        /// </summary>
        public string EditorTitle { get; set; }

        /// <summary>
        /// Gets or sets uRL Path.
        /// </summary>
        public string UrlPath { get; set; }

        /// <summary>
        /// Gets or sets array of editor fields.
        /// </summary>
        public IEnumerable<EditorField> EditorFields { get; set; }

        /// <summary>
        /// Gets or sets array of custom buttons for this editor.
        /// </summary>
        public IEnumerable<string> CustomButtons { get; set; }

        /// <summary>
        /// Gets or sets editor type.
        /// </summary>
        public string EditorType { get; set; } = nameof(EditCodePostModel);

        /// <summary>
        /// Gets or sets a value indicating whether update existing version.
        /// </summary>
        public bool UpdateExisting { get; set; } = true;

        /// <summary>
        /// Gets or sets article Banner Image.
        /// </summary>
        public string BannerImage { get; set; }

        /// <summary>
        /// Gets or sets the article permissions properity.
        /// </summary>
        public List<ArticlePermission> ArticlePermissions { get; set; }
    }
}
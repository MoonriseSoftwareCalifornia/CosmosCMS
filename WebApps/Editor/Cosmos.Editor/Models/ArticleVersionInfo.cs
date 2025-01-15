// <copyright file="ArticleVersionInfo.cs" company="Moonrise Software, LLC">
// Copyright (c) Moonrise Software, LLC. All rights reserved.
// Licensed under the GNU Public License, Version 3.0 (https://www.gnu.org/licenses/gpl-3.0.html)
// See https://github.com/MoonriseSoftwareCalifornia/CosmosCMS
// for more information concerning the license and the contributors participating to this project.
// </copyright>

using System;
using System.ComponentModel.DataAnnotations;
using Cosmos.Common.Data;

namespace Cosmos.Cms.Models
{
    /// <summary>
    ///     Article version list info item.
    /// </summary>
    [Serializable]
    public class ArticleVersionInfo
    {
        /// <inheritdoc cref="Article.Id" />
        [Key]
        public Guid Id { get; set; }

        /// <inheritdoc cref="Article.VersionNumber" />
        public int VersionNumber { get; set; }

        /// <inheritdoc cref="Article.Title" />
        public string Title { get; set; }

        /// <inheritdoc cref="Article.Updated" />
        public DateTimeOffset Updated { get; set; }

        /// <inheritdoc cref="Article.Published" />
        public DateTimeOffset? Published { get; set; }

        /// <inheritdoc cref="Article.Expires" />
        public DateTimeOffset? Expires { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether can use Live editor.
        /// </summary>
        public bool UsesHtmlEditor { get; set; }
    }
}
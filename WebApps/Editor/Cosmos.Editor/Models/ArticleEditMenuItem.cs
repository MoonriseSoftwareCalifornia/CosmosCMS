// <copyright file="ArticleEditMenuItem.cs" company="Moonrise Software, LLC">
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
    /// Article Edit Menu Item.
    /// </summary>
    public class ArticleEditMenuItem
    {
        /// <inheritdoc cref="Article.Id" />
        [Key]
        public Guid Id { get; set; }

        /// <summary>
        /// Gets or sets article Number.
        /// </summary>
        public int ArticleNumber { get; set; }

        /// <inheritdoc cref="Article.VersionNumber" />
        public int VersionNumber { get; set; }

        /// <inheritdoc cref="Article.Published" />
        public DateTimeOffset? Published { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether can use Live editor.
        /// </summary>
        public bool UsesHtmlEditor { get; set; }
    }
}

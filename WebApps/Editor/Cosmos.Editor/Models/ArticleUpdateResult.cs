// <copyright file="ArticleUpdateResult.cs" company="Cosmos Website Solutions, Inc.">
// Copyright (c) Cosmos Website Solutions, Inc.. All rights reserved.
// Licensed under the GNU Public License, Version 3.0 (https://www.gnu.org/licenses/gpl-3.0.html)
// See https://github.com/MoonriseSoftwareCalifornia/CosmosCMS
// for more information concerning the license and the contributors participating to this project.
// </copyright>

namespace Cosmos.Cms.Models
{
    using System.Collections.Generic;
    using Azure.ResourceManager;
    using Cosmos.Cms.Data.Logic;
    using Cosmos.Common.Models;

    /// <summary>
    ///     <see cref="ArticleEditLogic.Save(HtmlEditorViewModel, string)" /> result.
    /// </summary>
    public class ArticleUpdateResult
    {
        /// <summary>
        /// Gets or sets a value indicating whether server indicates the content was saved successfully in the database.
        /// </summary>
        public bool ServerSideSuccess { get; set; }

        /// <summary>
        ///     Gets or sets updated or Inserted model.
        /// </summary>
        public ArticleViewModel Model { get; set; }

        /// <summary>
        /// Gets or sets will return an ARM Operation if CDN purged.
        /// </summary>
        public ArmOperation ArmOperation { get; set; } = null;

        /// <summary>
        ///     Gets or sets urls that need to be flushed.
        /// </summary>
        public List<string> Urls { get; set; } = new List<string>();
    }
}
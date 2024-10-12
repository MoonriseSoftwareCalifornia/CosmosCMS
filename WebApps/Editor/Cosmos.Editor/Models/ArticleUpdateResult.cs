// <copyright file="ArticleUpdateResult.cs" company="Moonrise Software, LLC">
// Copyright (c) Moonrise Software, LLC. All rights reserved.
// Licensed under the GNU Public License, Version 3.0 (https://www.gnu.org/licenses/gpl-3.0.html)
// See https://github.com/MoonriseSoftwareCalifornia/CosmosCMS
// for more information concerning the license and the contributors participating to this project.
// </copyright>

namespace Cosmos.Cms.Models
{
    using System.Collections.Generic;
    using Cosmos.Common.Models;
    using Cosmos.Editor.Controllers;

    /// <summary>
    ///   The result of a update operation.
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
        public List<CdnResult> CdnResults { get; set; } = null;

        /// <summary>
        ///     Gets or sets urls that need to be flushed.
        /// </summary>
        public List<string> Urls { get; set; } = new List<string>();
    }
}
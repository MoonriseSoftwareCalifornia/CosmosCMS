// <copyright file="PreloadViewModel.cs" company="Cosmos Website Solutions, Inc.">
// Copyright (c) Cosmos Website Solutions, Inc.. All rights reserved.
// Licensed under the GNU Public License, Version 3.0 (https://www.gnu.org/licenses/gpl-3.0.html)
// See https://github.com/MoonriseSoftwareCalifornia/CosmosCMS
// for more information concerning the license and the contributors participating to this project.
// </copyright>

namespace Cosmos.Cms.Models
{
    /// <summary>
    /// Website preload options.
    /// </summary>
    public class PreloadViewModel
    {
        /// <summary>
        /// Gets or sets a value indicating whether preload CDN.
        /// </summary>
        public bool PreloadCdn { get; set; } = true;

        /// <summary>
        /// Gets or sets redis objects created.
        /// </summary>
        public int? PageCount { get; set; }

        /// <summary>
        /// Gets or sets number of editors involved with preload operation.
        /// </summary>
        public int EditorCount { get; set; } = 0;
    }
}

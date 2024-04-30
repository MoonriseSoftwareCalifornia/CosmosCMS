// <copyright file="ImageSizeModel.cs" company="Cosmos Website Solutions, Inc.">
// Copyright (c) Cosmos Website Solutions, Inc.. All rights reserved.
// Licensed under the GNU Public License, Version 3.0 (https://www.gnu.org/licenses/gpl-3.0.html)
// See https://github.com/MoonriseSoftwareCalifornia/CosmosCMS
// for more information concerning the license and the contributors participating to this project.
// </copyright>

namespace Cosmos.Cms.Models
{
    /// <summary>
    ///     Image size mode used for thumbnail generator.
    /// </summary>
    public class ImageSizeModel
    {
        /// <summary>
        ///     Gets or sets width in pixels.
        /// </summary>
        public int Width { get; set; } = 80;

        /// <summary>
        ///     Gets or sets height in pixels.
        /// </summary>
        public int Height { get; set; } = 80;
    }
}
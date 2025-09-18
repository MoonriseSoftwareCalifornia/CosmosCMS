// <copyright file="RenameBlobViewModel.cs" company="Moonrise Software, LLC">
// Copyright (c) Moonrise Software, LLC. All rights reserved.
// Licensed under the GNU Public License, Version 3.0 (https://www.gnu.org/licenses/gpl-3.0.html)
// See https://github.com/MoonriseSoftwareCalifornia/CosmosCMS
// for more information concerning the license and the contributors participating to this project.
// </copyright>

namespace Sky.Cms.Models
{
    /// <summary>
    /// Rename blob item view model.
    /// </summary>
    public class RenameBlobViewModel
    {
        /// <summary>
        /// Gets or sets from blob name.
        /// </summary>
        public string FromBlobName { get; set; }

        /// <summary>
        /// Gets or sets rename to blob name.
        /// </summary>
        public string ToBlobName { get; set; }

        /// <summary>
        /// Gets or sets the folder where the blob is located.
        /// </summary>
        public string BlobRootPath { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether item being renamed is a directory.
        /// </summary>
        public bool IsDirectory { get; set; }
    }
}

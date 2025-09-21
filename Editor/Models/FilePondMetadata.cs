// <copyright file="FilePondMetadata.cs" company="Moonrise Software, LLC">
// Copyright (c) Moonrise Software, LLC. All rights reserved.
// Licensed under the GNU Public License, Version 3.0 (https://www.gnu.org/licenses/gpl-3.0.html)
// See https://github.com/MoonriseSoftwareCalifornia/CosmosCMS
// for more information concerning the license and the contributors participating to this project.
// </copyright>

namespace Sky.Cms.Models
{
    using Newtonsoft.Json;

    /// <summary>
    /// Filepond upload metadata.
    /// </summary>
    public class FilePondMetadata
    {
        /// <summary>
        /// Gets or sets upload path or folder.
        /// </summary>
        [JsonProperty("path")]
        public string Path { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets subfolder (if appplicable) of upload.
        /// </summary>
        [JsonProperty("relativePath")]
        public string RelativePath { get; set; }

        /// <summary>
        /// Gets or sets name of file being uploaded.
        /// </summary>
        [JsonProperty("fileName")]
        public string FileName { get; set; }

        /// <summary>
        /// Gets or sets the image width.
        /// </summary>
        [JsonProperty("imageWidth")]
        public string ImageWidth { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the image height.
        /// </summary>
        [JsonProperty("imageHeight")]
        public string ImageHeight { get; set; } = string.Empty;
    }
}

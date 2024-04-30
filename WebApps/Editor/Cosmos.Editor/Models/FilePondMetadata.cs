// <copyright file="FilePondMetadata.cs" company="Cosmos Website Solutions, Inc.">
// Copyright (c) Cosmos Website Solutions, Inc.. All rights reserved.
// Licensed under the GNU Public License, Version 3.0 (https://www.gnu.org/licenses/gpl-3.0.html)
// See https://github.com/MoonriseSoftwareCalifornia/CosmosCMS
// for more information concerning the license and the contributors participating to this project.
// </copyright>

namespace Cosmos.Cms.Models
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
        public string Path { get; set; }

        /// <summary>
        /// Gets or sets subfolder (of appplicable) of upload.
        /// </summary>
        [JsonProperty("relativePath")]
        public string RelativePath { get; set; }

        /// <summary>
        /// Gets or sets name of file being uploaded.
        /// </summary>
        [JsonProperty("fileName")]
        public string FileName { get; set; }
    }
}

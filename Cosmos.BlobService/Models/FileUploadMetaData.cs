// <copyright file="FileUploadMetaData.cs" company="Moonrise Software, LLC">
// Copyright (c) Moonrise Software, LLC. All rights reserved.
// Licensed under the GNU Public License, Version 3.0 (https://www.gnu.org/licenses/gpl-3.0.html)
// See https://github.com/MoonriseSoftwareCalifornia/CosmosCMS
// for more information concerning the license and the contributors participating to this project.
// </copyright>

namespace Cosmos.BlobService.Models
{
    /// <summary>
    /// File upload metadata.
    /// </summary>
    public class FileUploadMetaData
    {
        /// <summary>
        /// Gets or sets chunk upload ID.
        /// </summary>
        public string UploadUid { get; set; }

        /// <summary>
        /// Gets or sets the file name.
        /// </summary>
        public string FileName { get; set; }

        /// <summary>
        /// Gets or sets the relative path.
        /// </summary>
        public string RelativePath { get; set; }

        /// <summary>
        /// Gets or sets the MIME content type.
        /// </summary>
        public string ContentType { get; set; }

        /// <summary>
        /// Gets or sets the chunk number.
        /// </summary>
        public long ChunkIndex { get; set; }

        /// <summary>
        /// Gets or sets the total number of chunks being uploaded.
        /// </summary>
        public long TotalChunks { get; set; }

        /// <summary>
        /// Gets or sets the total file size in bytes.
        /// </summary>
        public long TotalFileSize { get; set; }

        /// <summary>
        /// Gets or sets the image width.
        /// </summary>
        public string ImageWidth { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the image height.
        /// </summary>
        public string ImageHeight { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the cache control (ie CACHE-CONTROL) string.
        /// </summary>
        /// <remarks>Defaults to "max-age=3600, must-revalidate".</remarks>
        public string CacheControl { get; set; } = "max-age=3600, must-revalidate";
}
}
// <copyright file="FileMetadata.cs" company="Moonrise Software, LLC">
// Copyright (c) Moonrise Software, LLC. All rights reserved.
// Licensed under the GNU Public License, Version 3.0 (https://www.gnu.org/licenses/gpl-3.0.html)
// See https://github.com/MoonriseSoftwareCalifornia/CosmosCMS
// for more information concerning the license and the contributors participating to this project.
// </copyright>

namespace Cosmos.BlobService.Models
{
    using System;

    /// <summary>
    /// File metadata.
    /// </summary>
    public class FileMetadata
    {
        /// <summary>
        /// Gets or sets full file name including folder path for blob.
        /// </summary>
        public string FileName { get; set; }

        /// <summary>
        /// Gets or sets mime type.
        /// </summary>
        public string ContentType { get; set; }

        /// <summary>
        /// Gets or sets total number of bytes.
        /// </summary>
        public long ContentLength { get; set; }

        /// <summary>
        /// Gets or sets eTag.
        /// </summary>
        public string ETag { get; set; }

        /// <summary>
        /// Gets or sets last modified.
        /// </summary>
        public DateTimeOffset LastModified { get; set; }

        /// <summary>
        /// Gets or sets upon upload, the UTC date time for the upload is saved as a 'tick'.
        /// </summary>
        /// <remarks>
        /// This is used when blobs are being synchronized between storage accounts.
        /// </remarks>
        public long? UploadDateTime { get; set; }

        /// <summary>
        /// Gets or sets unique ID of the upload.
        /// </summary>
        /// <remarks>
        /// This is used when blobs are being synchronized between storage accounts.
        /// </remarks>
        public string UploadUid { get; set; }

        /// <summary>
        /// Gets or sets size if file when uploaded.
        /// </summary>
        /// <remarks>
        /// This is used when blobs are being synchronized between storage accounts.
        /// </remarks>
        public long? UploadSize { get; set; }
    }
}

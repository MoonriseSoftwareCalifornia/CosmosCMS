// <copyright file="AwsGetObjectsResult.cs" company="Moonrise Software, LLC">
// Copyright (c) Moonrise Software, LLC. All rights reserved.
// Licensed under the GNU Public License, Version 3.0 (https://www.gnu.org/licenses/gpl-3.0.html)
// See https://github.com/MoonriseSoftwareCalifornia/CosmosCMS
// for more information concerning the license and the contributors participating to this project.
// </copyright>

namespace Cosmos.BlobService.Models
{
    using System.Collections.Generic;
    using Amazon.S3.Model;

    /// <summary>
    /// AWS Get Objects Result.
    /// </summary>
    public class AwsGetObjectsResult
    {
        /// <summary>
        /// Gets or sets the Blob object return list.
        /// </summary>
        public List<S3Object> Blobs { get; set; } = new List<S3Object>();

        /// <summary>
        /// Gets or sets the common prefixes (like folders).
        /// </summary>
        public List<string> CommonPrefixes { get; set; } = new List<string>();
    }
}

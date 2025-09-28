// <copyright file="AmazonStorageConfig.cs" company="Moonrise Software, LLC">
// Copyright (c) Moonrise Software, LLC. All rights reserved.
// Licensed under the GNU Public License, Version 3.0 (https://www.gnu.org/licenses/gpl-3.0.html)
// See https://github.com/MoonriseSoftwareCalifornia/CosmosCMS
// for more information concerning the license and the contributors participating to this project.
// </copyright>

namespace Cosmos.BlobService.Config
{
    using System.ComponentModel.DataAnnotations;

    /// <summary>
    ///     Amazon S3 configuration.
    /// </summary>
    public class AmazonStorageConfig
    {
        /// <summary>
        ///     Gets or sets access key Id.
        /// </summary>
        [Display(Name = "Key Id")]
        public string KeyId { get; set; }

        /// <summary>
        ///     Gets or sets AWS secret access key.
        /// </summary>
        [Display(Name = "Key")]
        public string Key { get; set; }

        /// <summary>
        ///     Gets or sets amazon bucket name.
        /// </summary>
        [Display(Name = "Bucket")]
        public string BucketName { get; set; }

        /// <summary>
        ///     Gets or sets amazon region.
        /// </summary>
        [Display(Name = "Region")]
        [UIHint("AmazonRegions")]
        public string AmazonRegion { get; set; }

        /// <summary>
        ///  Gets or sets the Cloudflare account ID for R2 storage.
        /// </summary>
        [Display(Name = "Cloudflare Account ID (for R2)")]
        public string AccountId { get; set; }
    }
}
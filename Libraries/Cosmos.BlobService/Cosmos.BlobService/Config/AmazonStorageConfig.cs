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
        public string AmazonAwsAccessKeyId { get; set; }

        /// <summary>
        ///     Gets or sets aWS secret access key.
        /// </summary>
        [Display(Name = "Key")]
        public string AmazonAwsSecretAccessKey { get; set; }

        /// <summary>
        ///     Gets or sets amazon bucket name.
        /// </summary>
        [Display(Name = "Bucket")]
        public string AmazonBucketName { get; set; }

        /// <summary>
        ///     Gets or sets amazon region.
        /// </summary>
        [Display(Name = "Region")]
        [UIHint("AmazonRegions")]
        public string AmazonRegion { get; set; }

        /// <summary>
        ///     Gets or sets service URL.
        /// </summary>
        [Display(Name = "Website URL")]
        public string ServiceUrl { get; set; }

        /// <summary>
        ///     Gets or sets profile name.
        /// </summary>
        [Display(Name = "Conn. Name")]
        public string ProfileName { get; set; }
    }
}
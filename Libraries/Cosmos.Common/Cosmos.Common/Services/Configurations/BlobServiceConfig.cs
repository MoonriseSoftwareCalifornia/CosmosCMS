// <copyright file="BlobServiceConfig.cs" company="Moonrise Software, LLC">
// Copyright (c) Moonrise Software, LLC. All rights reserved.
// Licensed under the GNU Public License, Version 3.0 (https://www.gnu.org/licenses/gpl-3.0.html)
// See https://github.com/MoonriseSoftwareCalifornia/CosmosCMS
// for more information concerning the license and the contributors participating to this project.
// </copyright>

using System.ComponentModel.DataAnnotations;

namespace Cosmos.Cms.Common.Services.Configurations
{
    /// <summary>
    ///     Azure blob storage config.
    /// </summary>
    public class BlobServiceConfig
    {
        /// <summary>
        ///     Gets or sets id of the provider.
        /// </summary>
        [Key]
        [Display(Name = "Id")]
        public int Id { get; set; }

        /// <summary>
        ///     Gets or sets cloud provider.
        /// </summary>
        [Required]
        [UIHint("CloudProvider")]
        [Display(Name = "Cloud Provider")]
        public string CloudName { get; set; }

        /// <summary>
        ///     Gets or sets a value indicating whether is primary storage for this website.
        /// </summary>
        public bool IsPrimary { get; set; }

        /// <summary>
        ///     Gets or sets blob storage connection string.
        /// </summary>
        [Required]
        [Display(Name = "Blob storage connection string")]
        public string ConnectionString { get; set; }
    }
}
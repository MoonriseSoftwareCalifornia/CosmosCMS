// <copyright file="StorageConfig.cs" company="Moonrise Software, LLC">
// Copyright (c) Moonrise Software, LLC. All rights reserved.
// Licensed under the GNU Public License, Version 3.0 (https://www.gnu.org/licenses/gpl-3.0.html)
// See https://github.com/MoonriseSoftwareCalifornia/CosmosCMS
// for more information concerning the license and the contributors participating to this project.
// </copyright>

namespace Cosmos.BlobService.Config
{
    using System.Collections.Generic;

    /// <summary>
    ///     Storage provider configuration.
    /// </summary>
    public class StorageConfig
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="StorageConfig"/> class.
        /// </summary>
        public StorageConfig()
        {
            this.AmazonConfigs = new List<AmazonStorageConfig>();
            this.AzureConfigs = new List<AzureStorageConfig>();
            this.GoogleConfigs = new List<GoogleStorageConfig>();
        }

        /// <summary>
        ///     Gets or sets amazon configuration.
        /// </summary>
        public List<AmazonStorageConfig> AmazonConfigs { get; set; }

        /// <summary>
        ///     Gets or sets azure configuration.
        /// </summary>
        public List<AzureStorageConfig> AzureConfigs { get; set; }

        /// <summary>
        ///     Gets or sets google configuration.
        /// </summary>
        public List<GoogleStorageConfig> GoogleConfigs { get; set; }
    }
}
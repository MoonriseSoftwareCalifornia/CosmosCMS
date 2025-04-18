// <copyright file="CosmosStorageConfig.cs" company="Moonrise Software, LLC">
// Copyright (c) Moonrise Software, LLC. All rights reserved.
// Licensed under the GNU Public License, Version 3.0 (https://www.gnu.org/licenses/gpl-3.0.html)
// See https://github.com/MoonriseSoftwareCalifornia/CosmosCMS
// for more information concerning the license and the contributors participating to this project.
// </copyright>

namespace Cosmos.BlobService.Config
{
    using System.ComponentModel.DataAnnotations;

    /// <summary>
    ///     Cosmos configuration model.
    /// </summary>
    public class CosmosStorageConfig
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CosmosStorageConfig"/> class.
        /// </summary>
        public CosmosStorageConfig()
        {
            this.StorageConfig = new StorageConfig();
        }

        /// <summary>
        ///    Gets or sets the maximum cache seconds.
        /// </summary>
        public int MaxCacheSeconds { get; set; } = 3600;

        /// <summary>
        ///     Gets or sets primary cloud for this installation.
        /// </summary>
        public string PrimaryCloud { get; set; }

        /// <summary>
        ///     Gets or sets blob service configuration.
        /// </summary>
        public StorageConfig StorageConfig { get; set; }
    }
}
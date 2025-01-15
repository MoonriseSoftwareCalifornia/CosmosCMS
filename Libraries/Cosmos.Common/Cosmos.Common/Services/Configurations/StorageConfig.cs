// <copyright file="StorageConfig.cs" company="Moonrise Software, LLC">
// Copyright (c) Moonrise Software, LLC. All rights reserved.
// Licensed under the GNU Public License, Version 3.0 (https://www.gnu.org/licenses/gpl-3.0.html)
// See https://github.com/MoonriseSoftwareCalifornia/CosmosCMS
// for more information concerning the license and the contributors participating to this project.
// </copyright>

namespace Cosmos.Cms.Common.Services.Configurations
{
    using Cosmos.Cms.Common.Services.Configurations.Storage;
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
            AzureConfigs = new List<AzureStorageConfig>();
        }

        /// <summary>
        ///     Gets or sets azure configuration.
        /// </summary>
        public List<AzureStorageConfig> AzureConfigs { get; set; }
    }
}
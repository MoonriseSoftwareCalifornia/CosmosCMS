// <copyright file="CdnConfig.cs" company="Moonrise Software, LLC">
// Copyright (c) Moonrise Software, LLC. All rights reserved.
// Licensed under the GNU Public License, Version 3.0 (https://www.gnu.org/licenses/gpl-3.0.html)
// See https://github.com/MoonriseSoftwareCalifornia/CosmosCMS
// for more information concerning the license and the contributors participating to this project.
// </copyright>

namespace Cosmos.Cms.Common.Services.Configurations
{
    using System.ComponentModel.DataAnnotations;

    /// <summary>
    ///     CDN Configurations.
    /// </summary>
    public class CdnConfig
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CdnConfig"/> class.
        /// </summary>
        public CdnConfig()
        {
            AzureCdnConfig = new AzureCdnConfig();
        }

        /// <summary>
        ///     Gets or sets number of seconds to cache data (20 minutes default).
        /// </summary>
        [Display(Name = "Cache duration (seconds)")]
        public int CacheDuration { get; set; } = 1200;

        /// <summary>
        ///     Gets or sets azure CDN Configuration.
        /// </summary>
        public AzureCdnConfig AzureCdnConfig { get; set; }
    }
}
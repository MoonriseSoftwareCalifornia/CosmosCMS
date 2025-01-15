// <copyright file="SimpleProxyConfigs.cs" company="Moonrise Software, LLC">
// Copyright (c) Moonrise Software, LLC. All rights reserved.
// Licensed under the GNU Public License, Version 3.0 (https://www.gnu.org/licenses/gpl-3.0.html)
// See https://github.com/MoonriseSoftwareCalifornia/CosmosCMS
// for more information concerning the license and the contributors participating to this project.
// </copyright>

using System.ComponentModel.DataAnnotations;

namespace Cosmos.Cms.Common.Services.Configurations
{
    /// <summary>
    ///     Simple proxy service config.
    /// </summary>
    public class SimpleProxyConfigs
    {
        /// <summary>
        ///     Gets or sets array of configurations.
        /// </summary>
        [Display(Name = "Proxy configuration(s)")]
        public ProxyConfig[] Configs { get; set; }
    }
}
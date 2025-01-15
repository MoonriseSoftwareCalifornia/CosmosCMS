// <copyright file="ProxyConfig.cs" company="Moonrise Software, LLC">
// Copyright (c) Moonrise Software, LLC. All rights reserved.
// Licensed under the GNU Public License, Version 3.0 (https://www.gnu.org/licenses/gpl-3.0.html)
// See https://github.com/MoonriseSoftwareCalifornia/CosmosCMS
// for more information concerning the license and the contributors participating to this project.
// </copyright>

namespace Cosmos.Cms.Common.Services.Configurations
{
    using System.ComponentModel.DataAnnotations;

    /// <summary>
    ///     Proxy configuration.
    /// </summary>
    public class ProxyConfig
    {
        /// <summary>
        ///     Gets or sets name of the connection.
        /// </summary>
        [Display(Name = "Connection name")]
        public string Name { get; set; }

        /// <summary>
        ///     Gets or sets method (i.e. GET or POST).
        /// </summary>
        [Display(Name = "Method (i.e. GET or POST)")]
        public string Method { get; set; }

        /// <summary>
        ///     Gets or sets uRL end point.
        /// </summary>
        [Display(Name = "URL end point")]
        public string UriEndpoint { get; set; }

        /// <summary>
        ///     Gets or sets gET string or POST data.
        /// </summary>
        [Display(Name = "GET or POST data")]
        public string Data { get; set; }

        /// <summary>
        ///     Gets or sets user name to use when accessing end point.
        /// </summary>
        [Display(Name = "User name")]
        public string UserName { get; set; } = string.Empty;

        /// <summary>
        ///     Gets or sets password to use when accessing end point.
        /// </summary>
        [Display(Name = "Password")]
        public string Password { get; set; } = string.Empty;

        /// <summary>
        ///     Gets or sets content type.
        /// </summary>
        [Display(Name = "Content type")]
        public string ContentType { get; set; } = "application/x-www-form-urlencoded";

        /// <summary>
        ///     Gets or sets rBAC roles allowed to use end point.
        /// </summary>
        /// <remarks>
        ///     <para>
        ///         Anonymous role enables anyone to use end point. Authenticated role allows any authenticated user access.
        ///         Otherwise the specifc roles who have access are listed here.
        ///     </para>
        /// </remarks>
        [Display(Name = "Roles")]
        public string[] Roles { get; set; }
    }
}
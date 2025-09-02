// <copyright file="SiteSettings.cs" company="Moonrise Software, LLC">
// Copyright (c) Moonrise Software, LLC. All rights reserved.
// Licensed under the GNU Public License, Version 3.0 (https://www.gnu.org/licenses/gpl-3.0.html)
// See https://github.com/MoonriseSoftwareCalifornia/CosmosCMS
// for more information concerning the license and the contributors participating to this project.
// </copyright>

namespace Cosmos.Cms.Common.Services.Configurations
{
    using System.ComponentModel.DataAnnotations;

    /// <summary>
    ///     Startup.ConfigureServices method captures the site customization options found in "secrets" in this
    ///     object.
    /// </summary>
    public class SiteSettings
    {
        /// <summary>
        ///     Gets or sets allowed file type extensions.
        /// </summary>
        [Display(Name = "File types")]
        [Required]
        public string AllowedFileTypes { get; set; } = ".js,.css,.htm,.html,.mov,.webm,.avi,.mp4,.mpeg,.ts,.svg,.json";

        /// <summary>
        ///     Gets or sets for training purposes, allows a full reset of site to factory settings.
        /// </summary>
        [Display(Name = "Allow reset")]
        public bool? AllowReset { get; set; } = false;

        /// <summary>
        ///     Gets or sets a value indicating whether allows a website to go into setup mode. For use only on fresh sites.
        /// </summary>
        [Display(Name = "Allow setup")]
        public bool AllowSetup { get; set; } = false;

        /// <summary>
        /// Gets or sets a value indicating whether allows the advanced configuration editor to be available to Administrators.
        /// </summary>
        public bool AllowConfigEdit { get; set; } = false;

        /// <summary>
        /// Gets or sets a value indicating whether allows local accounts (default = true).
        /// </summary>
        /// <remarks>
        /// If disabled then assumes the use of Microsoft, Google or other supported OAuth provider.
        /// </remarks>
        public bool AllowLocalAccounts { get; set; } = false;

        /// <summary>
        /// Gets or sets a value indicating what kind of architecture is being used.
        /// </summary>
        /// <remarks>Valid values are: Static, Decoupled, Api, Hybrid.</remarks>
        public string CosmosArchitecture { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets a value indicating whether publisher requires authentication.
        /// </summary>
        public bool CosmosRequiresAuthentication { get; set; } = false;

        /// <summary>
        /// Gets or sets a value indicating whether the editor supports multiple websites.
        /// </summary>
        public bool MultiTenantEditor { get; set; } = false;

        /// <summary>
        /// Gets or sets x-Frame-Options.
        /// </summary>
        /// <remarks>
        /// <para>The X-Frame-Options HTTP response header can be used to indicate whether or not a
        /// browser should be allowed to render a page in a frame, iframe, embed or object.
        /// Sites can use this to avoid click-jacking attacks, by ensuring that their content
        /// is not embedded into other sites.</para>
        /// <para>See: <see href="https://developer.mozilla.org/en-US/docs/Web/HTTP/Headers/X-Frame-Options"/>.</para>
        /// </remarks>
        [Display(Name = "X-Frame-Options")]
        public string XFrameOptions { get; set; } = string.Empty;
    }
}
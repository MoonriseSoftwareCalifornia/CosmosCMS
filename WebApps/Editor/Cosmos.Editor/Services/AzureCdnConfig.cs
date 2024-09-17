// <copyright file="AzureCdnConfig.cs" company="Moonrise Software, LLC">
// Copyright (c) Moonrise Software, LLC. All rights reserved.
// Licensed under the GNU Public License, Version 3.0 (https://www.gnu.org/licenses/gpl-3.0.html)
// See https://github.com/MoonriseSoftwareCalifornia/CosmosCMS
// for more information concerning the license and the contributors participating to this project.
// </copyright>

namespace Cosmos.Editor.Services
{
    using System;
    using System.ComponentModel.DataAnnotations;

    /// <summary>
    ///     Configuration for Azure Front Door, Edgio or Microsoft CDN.
    /// </summary>
    public class AzureCdnConfig
    {
        /// <summary>
        ///     Gets or sets end Point Name.
        /// </summary>
        [Display(Name = "End Point Name")]
        [Required(AllowEmptyStrings = false, ErrorMessage = "End Point Name is required.")]
        public string EndPointName { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets a value indicating whether this is Azure frontdoor.
        /// </summary>
        [Display(Name = "Is Azure Front Door?")]
        public bool IsFrontDoor { get; set; } = false;

        /// <summary>
        /// Gets or sets the CDN profile name.
        /// </summary>
        [Display(Name = "Profile Name")]
        [Required(AllowEmptyStrings = false, ErrorMessage = "Profile Name is required.")]
        public string ProfileName { get; set; } = string.Empty;

        /// <summary>
        ///     Gets or sets azure Resource Group.
        /// </summary>
        [Display(Name = "Resource Group Name")]
        [Required(AllowEmptyStrings = false, ErrorMessage = "Resource Group Name is required.")]
        public string ResourceGroup { get; set; } = string.Empty;

        /// <summary>
        ///     Gets or sets subscription Id.
        /// </summary>
        [Display(Name = "Subscription Id")]
        [Required(ErrorMessage = "Resource Group Name is required.")]
        public Guid? SubscriptionId { get; set; }

        /// <summary>
        /// Indicates if CDN integration is configured.
        /// </summary>
        /// <returns>If true then a CDN or Front Door integration is configured.</returns>
        public bool IsConfigured()
        {
            return ProfileName != string.Empty
                && SubscriptionId != null
                && ResourceGroup != string.Empty
                && EndPointName != string.Empty;
        }

    }
}
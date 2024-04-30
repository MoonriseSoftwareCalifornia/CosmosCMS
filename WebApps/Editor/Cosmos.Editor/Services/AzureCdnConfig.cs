// <copyright file="AzureCdnConfig.cs" company="Cosmos Website Solutions, Inc.">
// Copyright (c) Cosmos Website Solutions, Inc.. All rights reserved.
// Licensed under the GNU Public License, Version 3.0 (https://www.gnu.org/licenses/gpl-3.0.html)
// See https://github.com/MoonriseSoftwareCalifornia/CosmosCMS
// for more information concerning the license and the contributors participating to this project.
// </copyright>

namespace Cosmos.Cms.Common.Services.Configurations
{
    using System.ComponentModel.DataAnnotations;
    using Cosmos.Common.Services.Configurations;

    /// <summary>
    ///     Configuration for Azure Front Door, Edgio or Microsoft CDN.
    /// </summary>
    public class AzureCdnConfig : OAuth
    {
        /// <summary>
        ///     Gets or sets subscription Id.
        /// </summary>
        [Display(Name = "Subscription Id")]
        public string SubscriptionId { get; set; } = string.Empty;

        /// <summary>
        ///     Gets or sets tenant Id.
        /// </summary>
        [Display(Name = "Tenant Id")]
        public string TenantId { get; set; } = string.Empty;

        /// <summary>
        ///     Gets or sets azure Resource Group.
        /// </summary>
        [Display(Name = "Azure Resource Group")]
        public string ResourceGroup { get; set; } = string.Empty;

        /// <summary>
        ///     Gets or sets end Point Name.
        /// </summary>
        [Display(Name = "End Point Name")]
        public string EndPointName { get; set; } = string.Empty;

        /// <summary>
        ///     Gets or sets CDN Provider Name.
        /// </summary>
        /// <remarks>Supported includes: Front Door, Microsoft or Edgio.</remarks>
        [Display(Name = "CDN Provider Name")]
        public string CdnProvider { get; set; } = string.Empty;

        // ===========================================================
        // Edgio or Microsoft speciifc fields.

        /// <summary>
        ///     Gets or sets tenant Domain Name.
        /// </summary>
        [Display(Name = "Tenant Domain Name")]
        public string TenantDomainName { get; set; } = string.Empty;

        /// <summary>
        ///     Gets or sets CDN Profile Name.
        /// </summary>
        [Display(Name = "CDN Profile Name")]
        public string CdnProfileName { get; set; } = string.Empty;

        // ===========================================================
        // Front Door specific properties if not Edgio or Microsoft.

        /// <summary>
        /// Gets or sets frontdoor name.
        /// </summary>
        public string FrontDoorName { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets comma delimited list of DNS names to purge.
        /// </summary>
        public string DnsNames { get; set; } = string.Empty;

        /// <summary>
        /// Indicates if CDN integration is configured.
        /// </summary>
        /// <returns>If true then a CDN or Front Door integration is configured.</returns>
        public override bool IsConfigured()
        {
            return base.IsConfigured()
                && SubscriptionId != string.Empty
                && TenantId != string.Empty
                && ResourceGroup != string.Empty
                && EndPointName != string.Empty
                && CdnProvider != string.Empty;
        }

        /// <summary>
        /// Indicates if CDN (Edgio or Microsoft) is configured.
        /// </summary>
        /// <returns>Returns true if configured.</returns>
        public bool IsCdnConfigured()
        {
            return IsConfigured() && TenantDomainName != string.Empty && CdnProfileName != string.Empty;
        }

        /// <summary>
        /// Indicates if Front Door is configured.
        /// </summary>
        /// <returns>Returns true if configured.</returns>
        public bool IsFrontDoorConfigured()
        {
            return IsConfigured() && FrontDoorName != string.Empty && DnsNames != string.Empty;
        }
    }
}
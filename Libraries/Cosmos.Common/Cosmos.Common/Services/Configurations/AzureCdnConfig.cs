// <copyright file="AzureCdnConfig.cs" company="Moonrise Software, LLC">
// Copyright (c) Moonrise Software, LLC. All rights reserved.
// Licensed under the GNU Public License, Version 3.0 (https://www.gnu.org/licenses/gpl-3.0.html)
// See https://github.com/MoonriseSoftwareCalifornia/CosmosCMS
// for more information concerning the license and the contributors participating to this project.
// </copyright>

namespace Cosmos.Cms.Common.Services.Configurations
{
    using System.ComponentModel.DataAnnotations;

    /// <summary>
    ///     Configuration for Azure CDN.
    /// </summary>
    public class AzureCdnConfig
    {
        /// <summary>
        ///     Gets or sets client Id.
        /// </summary>
        [Display(Name = "Client Id")]
        public string ClientId { get; set; }

        /// <summary>
        ///     Gets or sets cDN Provider Name.
        /// </summary>
        [UIHint("AzureCdnProviders")]
        [Display(Name = "CDN Provider Name")]
        public string CdnProvider { get; set; }

        /// <summary>
        ///     Gets or sets client Secret.
        /// </summary>
        [Display(Name = "Client Secret")]
        public string ClientSecret { get; set; }

        /// <summary>
        ///     Gets or sets tenant Id.
        /// </summary>
        [Display(Name = "Tenant Id")]
        public string TenantId { get; set; }

        /// <summary>
        ///     Gets or sets tenant Domain Name.
        /// </summary>
        [Display(Name = "Tenant Domain Name")]
        public string TenantDomainName { get; set; }

        /// <summary>
        ///     Gets or sets cDN Profile Name.
        /// </summary>
        [Display(Name = "CDN Profile Name")]
        public string CdnProfileName { get; set; }

        /// <summary>
        ///     Gets or sets end Point Name.
        /// </summary>
        [Display(Name = "End Point Name")]
        public string EndPointName { get; set; }

        /// <summary>
        ///     Gets or sets azure Resource Group.
        /// </summary>
        [Display(Name = "Azure Resource Group")]
        public string ResourceGroup { get; set; }

        /// <summary>
        ///     Gets or sets subscription Id.
        /// </summary>
        [Display(Name = "Subscription Id")]
        public string SubscriptionId { get; set; }
    }
}
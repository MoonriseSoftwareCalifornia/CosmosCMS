// <copyright file="AzureCdnEndpoint.cs" company="Moonrise Software, LLC">
// Copyright (c) Moonrise Software, LLC. All rights reserved.
// Licensed under the GNU Public License, Version 3.0 (https://www.gnu.org/licenses/gpl-3.0.html)
// See https://github.com/MoonriseSoftwareCalifornia/CosmosCMS
// for more information concerning the license and the contributors participating to this project.
// </copyright>

namespace Cosmos.Cms.Models
{
    /// <summary>
    /// Parameters used to identity an Azure CDN endpoint.
    /// </summary>
    public class AzureCdnEndpoint
    {
        /// <summary>
        /// Gets or sets end point unique ID.
        /// </summary>
        public string EndPointId { get; set; }

        /// <summary>
        /// Gets or sets subscription ID.
        /// </summary>
        public string SubscriptionId { get; set; }

        /// <summary>
        /// Gets or sets name of resource group where CDN is located.
        /// </summary>
        public string ResourceGroupName { get; set; }

        /// <summary>
        /// Gets or sets name of CDN Profile.
        /// </summary>
        public string CdnProfileName { get; set; }

        /// <summary>
        /// Gets or sets unique endpoint name.
        /// </summary>
        public string EndpointName { get; set; }

        /// <summary>
        /// Gets or sets cDN Type (or SKU).
        /// </summary>
        public string SkuName { get; set; }

        /// <summary>
        /// Gets or sets end point host name.
        /// </summary>
        public string EndPointHostName { get; set; }
    }
}

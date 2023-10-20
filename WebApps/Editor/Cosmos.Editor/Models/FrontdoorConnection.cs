// <copyright file="FrontdoorConnection.cs" company="Moonrise Software, LLC">
// Copyright (c) Moonrise Software, LLC. All rights reserved.
// Licensed under the GNU Public License, Version 3.0 (https://www.gnu.org/licenses/gpl-3.0.html)
// See https://github.com/MoonriseSoftwareCalifornia/CosmosCMS
// for more information concerning the license and the contributors participating to this project.
// </copyright>

namespace Cosmos.Editor.Models
{
    /// <summary>
    /// Azure Frontdoor connection information.
    /// </summary>
    public class FrontdoorConnection
    {
        /// <summary>
        /// All values are configured.
        /// </summary>
        /// <returns></returns>
        public bool IsConfigured()
        {
            return !string.IsNullOrEmpty(SubscriptionId)
                && !string.IsNullOrEmpty(ResourceGroupName)
                && !string.IsNullOrEmpty(FrontDoorName)
                && !string.IsNullOrEmpty(EndpointName)
                && !string.IsNullOrEmpty(TenantId)
                && !string.IsNullOrEmpty(ClientId)
                && !string.IsNullOrEmpty(ClientSecret)
                && !string.IsNullOrEmpty(DnsNames);
        }

        /// <summary>
        /// Gets or sets subscription ID of where FD is located.
        /// </summary>
        public string SubscriptionId { get; set; }

        /// <summary>
        /// Gets or sets resource group name where FD is located.
        /// </summary>
        public string ResourceGroupName { get; set; }

        /// <summary>
        /// Gets or sets frontdoor name.
        /// </summary>
        public string FrontDoorName { get; set; }

        /// <summary>
        /// Gets or sets front door endpoint name (specific to a website).
        /// </summary>
        public string EndpointName { get; set; }

        /// <summary>
        /// Gets or sets tenent ID of where FD is located.
        /// </summary>
        public string TenantId { get; set; }

        /// <summary>
        /// Gets or sets registered application ID (client ID) that has access to this FD.
        /// </summary>
        public string ClientId { get; set; }

        /// <summary>
        /// Gets or sets client secret of the application ID (client ID) that has access to this FD.
        /// </summary>
        public string ClientSecret { get; set; }

        /// <summary>
        /// Gets or sets comma delimited list of DNS names to purge.
        /// </summary>
        public string DnsNames { get; set; }
    }
}

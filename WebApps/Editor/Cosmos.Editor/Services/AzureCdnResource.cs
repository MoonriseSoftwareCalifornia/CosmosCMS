// <copyright file="AzureCdnResource.cs" company="Moonrise Software, LLC">
// Copyright (c) Moonrise Software, LLC. All rights reserved.
// Licensed under the GNU Public License, Version 3.0 (https://www.gnu.org/licenses/gpl-3.0.html)
// See https://github.com/MoonriseSoftwareCalifornia/CosmosCMS
// for more information concerning the license and the contributors participating to this project.
// </copyright>

namespace Cosmos.Cms.Services
{
    using Azure.ResourceManager.Resources;

    /// <summary>
    /// Azure subscription.
    /// </summary>
    public class AzureSubscription
    {
        /// <summary>
        /// Gets or sets azure subscription.
        /// </summary>
        public SubscriptionResource Subscription { get; set; }
    }
}

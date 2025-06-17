// <copyright file="StoryDeskConfig.cs" company="Moonrise Software, LLC">
// Copyright (c) Moonrise Software, LLC. All rights reserved.
// Licensed under the GNU Public License, Version 3.0 (https://www.gnu.org/licenses/gpl-3.0.html)
// See https://github.com/MoonriseSoftwareCalifornia/CosmosCMS
// for more information concerning the license and the contributors participating to this project.
// </copyright>

namespace Cosmos.Common.Models
{
    /// <summary>
    /// Configuration settings for StoryDesk integration.
    /// </summary>
    public class StoryDeskConfig
    {
        /// <summary>
        /// Gets or sets the Azure Active Directory Tenant ID.
        /// </summary>
        public string TenantId { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the Client ID for the Azure Active Directory application.
        /// </summary>
        public string ClientId { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the Client Secret for the Azure Active Directory application.
        /// </summary>
        public string ClientSecret { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the shared mailbox address for StoryDesk.
        /// </summary>
        public string Mailbox { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the API key for StoryDesk integration.
        /// </summary>
        public string ApiKey { get; set; } = string.Empty;
    }
}

// <copyright file="AzureCommunicationEmailProviderOptions.cs" company="Moonrise Software, LLC">
// Copyright (c) Moonrise Software, LLC. All rights reserved.
// Licensed under the GNU Public License, Version 3.0 (https://www.gnu.org/licenses/gpl-3.0.html)
// See https://github.com/MoonriseSoftwareCalifornia/CosmosCMS
// for more information concerning the license and the contributors participating to this project.
// </copyright>

namespace Cosmos.EmailServices
{
    /// <summary>
    /// Azure communications email provider options.
    /// </summary>
    public class AzureCommunicationEmailProviderOptions
    {
        /// <summary>
        /// Gets or sets connection string to the Azure communications email service.
        /// </summary>
        required public string ConnectionString { get; set; }

        /// <summary>
        /// Gets or sets default 'from' email address (if not using the default).
        /// </summary>
        public string DefaultFromEmailAddress { get; set; } = "DoNotReply@moonrise.net";
    }
}

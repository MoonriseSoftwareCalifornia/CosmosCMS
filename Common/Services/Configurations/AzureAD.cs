// <copyright file="AzureAD.cs" company="Moonrise Software, LLC">
// Copyright (c) Moonrise Software, LLC. All rights reserved.
// Licensed under the GNU Public License, Version 3.0 (https://www.gnu.org/licenses/gpl-3.0.html)
// See https://github.com/MoonriseSoftwareCalifornia/CosmosCMS
// for more information concerning the license and the contributors participating to this project.
// </copyright>

namespace Cosmos.Common.Services.Configurations
{
    /// <summary>
    /// Azure AD standard configuration options for ASP.NET Core.
    /// </summary>
    public class AzureAD : OAuth
    {
        /// <summary>
        /// Gets or sets the instance of the Azure AD service (if needed).
        /// </summary>
        /// <example>https://login.microsoftonline.com/.</example>
        public string Instance { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the domain of the Azure AD service (if needed).
        /// </summary>
        public string Domain { get; set; } = string.Empty;
    }
}

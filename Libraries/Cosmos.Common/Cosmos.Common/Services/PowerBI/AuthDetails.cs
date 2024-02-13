// <copyright file="AuthDetails.cs" company="Moonrise Software, LLC">
// Copyright (c) Moonrise Software, LLC. All rights reserved.
// Licensed under the GNU Public License, Version 3.0 (https://www.gnu.org/licenses/gpl-3.0.html)
// See https://github.com/MoonriseSoftwareCalifornia/CosmosCMS
// for more information concerning the license and the contributors participating to this project.
// </copyright>

namespace Cosmos.Common.Services.PowerBI
{
    /// <summary>
    /// Entry ID Token for Power BI.
    /// </summary>
    public class AuthDetails
    {
        /// <summary>
        /// Gets or sets the Entra ID user name.
        /// </summary>
        public string UserName { get; set; }

        /// <summary>
        /// Gets or sets the Access Token for this user to be used with Power BI.
        /// </summary>
        public string AccessToken { get; set; }
    }
}

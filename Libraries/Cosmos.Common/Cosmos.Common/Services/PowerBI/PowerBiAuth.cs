// <copyright file="PowerBiAuth.cs" company="Moonrise Software, LLC">
// Copyright (c) Moonrise Software, LLC. All rights reserved.
// Licensed under the GNU Public License, Version 3.0 (https://www.gnu.org/licenses/gpl-3.0.html)
// See https://github.com/MoonriseSoftwareCalifornia/CosmosCMS
// for more information concerning the license and the contributors participating to this project.
// </copyright>

namespace Cosmos.Common.Services.PowerBI
{
    using System.Linq;
    using Cosmos.Common.Services.Configurations;

    /// <summary>
    /// Power BI Entra ID App configuration.
    /// </summary>
    /// <remarks>This application must have permissions to get a token from the Power BI API.</remarks>
    public class PowerBiAuth : OAuth
    {
        /// <summary>
        /// Gets or sets the tenant ID of the client application.
        /// </summary>
        public string TenantId { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the Power BI scope base array.
        /// </summary>
        public string[] ScopeBase { get; set; } = ["https://analysis.windows.net/powerbi/api/.default"];

        /// <inheritdoc/>
        public override bool IsConfigured()
        {
            return base.IsConfigured() && TenantId != string.Empty && ScopeBase.Any();
        }
    }
}

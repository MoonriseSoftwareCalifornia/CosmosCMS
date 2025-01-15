// <copyright file="EmbedParams.cs" company="Moonrise Software, LLC">
// Copyright (c) Moonrise Software, LLC. All rights reserved.
// Licensed under the GNU Public License, Version 3.0 (https://www.gnu.org/licenses/gpl-3.0.html)
// See https://github.com/MoonriseSoftwareCalifornia/CosmosCMS
// for more information concerning the license and the contributors participating to this project.
// </copyright>

using System.Collections.Generic;

namespace Cosmos.Common.Services.PowerBI
{
    /// <summary>
    /// Embed parameters.
    /// </summary>
    public class EmbedParams
    {
        /// <summary>
        /// Gets or sets the type of the object to be embedded.
        /// </summary>
        public string Type { get; set; }

        /// <summary>
        /// Gets or sets the reports to be embedded.
        /// </summary>
        public List<EmbedReport> EmbedReport { get; set; }

        /// <summary>
        /// Gets or sets the embed Token for the Power BI report.
        /// </summary>
        public EmbedToken EmbedToken { get; set; }
    }
}

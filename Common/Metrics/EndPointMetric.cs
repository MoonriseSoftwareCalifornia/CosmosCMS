// <copyright file="EndPointMetric.cs" company="Moonrise Software, LLC">
// Copyright (c) Moonrise Software, LLC. All rights reserved.
// Licensed under the GNU Public License, Version 3.0 (https://www.gnu.org/licenses/gpl-3.0.html)
// See https://github.com/MoonriseSoftwareCalifornia/CosmosCMS
// for more information concerning the license and the contributors participating to this project.
// </copyright>

namespace Cosmos.Common.Metrics
{
    using System;

    /// <summary>
    /// Summary statistics for the FrontDoorAccessLog.
    /// </summary>
    /// <remarks>
    /// The following KQL Log Analytics query can get data from this table:
    /// AzureDiagnostics | where ResourceProvider == "MICROSOFT.CDN" | where Category == "FrontDoorAccessLog".
    /// </remarks>
    public class EndPointMetric
    {
        /// <summary>
        /// Gets or sets the date (day) during which bytes are recorded.
        /// </summary>
        public DateOnly Date { get; set; }

        /// <summary>
        /// Gets or sets the total response bytes for the date indicated.
        /// </summary>
        public long? ResponseBytes { get; set; }

        /// <summary>
        /// Gets or sets the total request bytes for the date indicated.
        /// </summary>
        public long? RequestBytes { get; set; }

        /// <summary>
        /// Gets or sets the host header.
        /// </summary>
        public string Host { get; set; } = string.Empty;
    }
}

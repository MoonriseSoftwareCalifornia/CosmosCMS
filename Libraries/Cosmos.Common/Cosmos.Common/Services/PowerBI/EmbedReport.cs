// <copyright file="EmbedReport.cs" company="Moonrise Software, LLC">
// Copyright (c) Moonrise Software, LLC. All rights reserved.
// Licensed under the GNU Public License, Version 3.0 (https://www.gnu.org/licenses/gpl-3.0.html)
// See https://github.com/MoonriseSoftwareCalifornia/CosmosCMS
// for more information concerning the license and the contributors participating to this project.
// </copyright>

namespace Cosmos.Common.Services.PowerBI
{
    using System;

    /// <summary>
    /// PowerBI Embed Report.
    /// </summary>
    public class EmbedReport
    {
        /// <summary>
        /// Gets or sets the report ID.
        /// </summary>
        public Guid ReportId { get; set; }

        /// <summary>
        /// Gets or sets the report name.
        /// </summary>
        public string ReportName { get; set; }

        /// <summary>
        /// Gets or sets the report embed URL.
        /// </summary>
        public string EmbedUrl { get; set; }
    }
}

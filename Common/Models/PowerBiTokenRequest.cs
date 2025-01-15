// <copyright file="PowerBiTokenRequest.cs" company="Moonrise Software, LLC">
// Copyright (c) Moonrise Software, LLC. All rights reserved.
// Licensed under the GNU Public License, Version 3.0 (https://www.gnu.org/licenses/gpl-3.0.html)
// See https://github.com/MoonriseSoftwareCalifornia/CosmosCMS
// for more information concerning the license and the contributors participating to this project.
// </copyright>

namespace Cosmos.Common.Models
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Power BI Token request model.
    /// </summary>
    public class PowerBiTokenRequest
    {
        /// <summary>
        /// Gets or sets the Power BI Workspace ID.
        /// </summary>
        public Guid PowerBiWorkspaceId { get; set; }

        /// <summary>
        /// Gets or sets the report ID.
        /// </summary>
        public Guid ReportId { get; set; }

        /// <summary>
        /// Gets or sets the dataset IDs.
        /// </summary>
        public IList<Guid> DatasetIds { get; set; }
    }
}

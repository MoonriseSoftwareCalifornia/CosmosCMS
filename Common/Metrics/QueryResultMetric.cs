// <copyright file="QueryResultMetric.cs" company="Moonrise Software, LLC">
// Copyright (c) Moonrise Software, LLC. All rights reserved.
// Licensed under the GNU Public License, Version 3.0 (https://www.gnu.org/licenses/gpl-3.0.html)
// See https://github.com/MoonriseSoftwareCalifornia/CosmosCMS
// for more information concerning the license and the contributors participating to this project.
// </copyright>

namespace Cosmos.Common.Metrics
{
    using System;

    /// <summary>
    /// Represents a metric result for Azure resources.
    /// </summary>
    public class QueryResultMetric
    {
        /// <summary>
        /// Gets or sets the resource type.
        /// </summary>
        public string ResourceType { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the resource ID.
        /// </summary>
        public string ResourceId { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the resource name.
        /// </summary>
        public string ResourceName { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the time stamp of the metric.
        /// </summary>
        public DateTimeOffset TimeStamp { get; set; }

        /// <summary>
        /// Gets or sets the total value of the metric.
        /// </summary>
        public double? Total { get; set; }

        /// <summary>
        /// Gets or sets the minimum value of the metric.
        /// </summary>
        public double? Minimum { get; set; }

        /// <summary>
        /// Gets or sets the maximum value of the metric.
        /// </summary>
        public double? Maximum { get; set; }

        /// <summary>
        /// Gets or sets the count of the metric.
        /// </summary>
        public double? Count { get; set; }

        /// <summary>
        /// Gets or sets the average value of the metric.
        /// </summary>
        public double? Average { get; set; }

        /// <summary>
        /// Gets or sets the metric name.
        /// </summary>
        public string MetricName { get; set; } = string.Empty;
    }
}

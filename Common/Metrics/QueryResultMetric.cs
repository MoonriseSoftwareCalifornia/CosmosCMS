using System;

namespace Cosmos.Common.Metrics
{
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

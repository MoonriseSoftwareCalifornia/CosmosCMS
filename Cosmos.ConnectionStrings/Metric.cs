// <copyright file="Metrics.cs" company="Moonrise Software, LLC">
// Copyright (c) Moonrise Software, LLC. All rights reserved.
// Licensed under the GNU Public License, Version 3.0 (https://www.gnu.org/licenses/gpl-3.0.html)
// See https://github.com/MoonriseSoftwareCalifornia/CosmosCMS
// for more information concerning the license and the contributors participating to this project.
// </copyright>

namespace Cosmos.DynamicConfig
{
    using System;
    using System.ComponentModel.DataAnnotations;

    /// <summary>
    /// Cosmos CMS Metrics Class.
    /// </summary>
    public class Metric
    {
        /// <summary>
        /// Gets or sets the unique identifier for the metric.
        /// </summary>
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        /// <summary>
        /// Gets or sets the unique identifier for the connection associated with the metric.
        /// </summary>
        public Guid ConnectionId { get; set; } = Guid.Empty;

        /// <summary>
        /// Gets or sets the date and time offset of when the metric was last updated.
        /// </summary>
        public DateTimeOffset TimeStamp { get; set; }

        /// <summary>
        /// Gets or sets the total size of blob storage in bytes for the day.
        /// </summary>
        public double? BlobStorageBytes { get; set; } = 0;

        /// <summary>
        /// Gets or sets the total size of blob storage outbound bandwidth in bytes for the day.
        /// </summary>
        public double? BlobStorageEgressBytes { get; set; } = 0;

        /// <summary>
        /// Gets or sets the total size of blob storage inbound bandwidth in bytes for the day.
        /// </summary>
        public double? BlobStorageIngressBytes { get; set; } = 0;

        /// <summary>
        /// Gets or sets the total size of database storage in bytes for the day.
        /// </summary>
        public double? DatabaseDataUsageBytes { get; set; }

        /// <summary>
        /// Gets or sets the total size of database storage index usage in bytes for the day.
        /// </summary>
        public double? DatabaseIndexUsageBytes { get; set; }

        /// <summary>
        /// Gets or sets the total size of RU usage for the day.
        /// </summary>
        public double? DatabaseRuUsage { get; set; } = 0;

        /// <summary>
        /// Gets or sets the total size of RU usage for the day.
        /// </summary>
        public long? FrontDoorRequestBytes { get; set; } = 0;

        /// <summary>
        /// Gets or sets the total size of RU usage for the day.
        /// </summary>
        public long? FrontDoorResponseBytes { get; set; } = 0;
    }
}

// <copyright file="StorageAccountMetrics.cs" company="Moonrise Software, LLC">
// Copyright (c) Moonrise Software, LLC. All rights reserved.
// Licensed under the GNU Public License, Version 3.0 (https://www.gnu.org/licenses/gpl-3.0.html)
// See https://github.com/MoonriseSoftwareCalifornia/CosmosCMS
// for more information concerning the license and the contributors participating to this project.
// </copyright>

namespace Cosmos.Common.Metrics
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Azure.Core;
    using Azure.Identity;
    using Azure.Monitor.Query;
    using Azure.Monitor.Query.Models;
    using Azure.Storage.Blobs;

    /// <summary>
    /// Storage account metrics class.
    /// </summary>
    public class StorageAccountMetrics
    {
        private readonly MetricsQueryClient client;
        private readonly LogsQueryClient logsQueryClient;
        private string resourceId;

        /// <summary>
        /// Initializes a new instance of the <see cref="StorageAccountMetrics"/> class.
        /// </summary>
        /// <param name="subscriptionId">Subscription ID.</param>
        /// <param name="resourceGroup">Resource group name.</param>
        /// <param name="accountName">Storage account name.</param>
        public StorageAccountMetrics(string subscriptionId, string resourceGroup, string accountName)
        {
            client = new MetricsQueryClient(new DefaultAzureCredential());
            logsQueryClient = new LogsQueryClient(new DefaultAzureCredential());
            resourceId = $"/subscriptions/{subscriptionId}/resourceGroups/{resourceGroup}/providers/Microsoft.Storage/storageAccounts/{accountName}";
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="StorageAccountMetrics"/> class.
        /// </summary>
        /// <param name="tokenCredential">An existing Azure Default Credential.</param>
        /// <param name="subscriptionId">Subscription ID.</param>
        /// <param name="resourceGroup">Resource group name.</param>
        /// <param name="accountName">Storage account name.</param>
        public StorageAccountMetrics(TokenCredential tokenCredential, string subscriptionId, string resourceGroup, string accountName)
        {
            client = new MetricsQueryClient(tokenCredential);
            logsQueryClient = new LogsQueryClient(tokenCredential);
            resourceId = $"/subscriptions/{subscriptionId}/resourceGroups/{resourceGroup}/providers/Microsoft.Storage/storageAccounts/{accountName}";
        }

        /// <summary>
        ///  Initializes a new instance of the <see cref="StorageAccountMetrics"/> class.
        /// </summary>
        /// <param name="tenantId">Tenant ID.</param>
        /// <param name="clientId">Registered application client ID.</param>
        /// <param name="clientSecret">Registered application secret value.</param>
        /// <param name="subscriptionId">Subscription ID.</param>
        /// <param name="resourceGroup">Resource group name.</param>
        /// <param name="accountName">Storage account name.</param>
        public StorageAccountMetrics(string tenantId, string clientId, string clientSecret, string subscriptionId, string resourceGroup, string accountName)
        {
            var credential = new ClientSecretCredential(tenantId, clientId, clientSecret);
            client = new MetricsQueryClient(credential);
            resourceId = $"/subscriptions/{subscriptionId}/resourceGroups/{resourceGroup}/providers/Microsoft.Storage/storageAccounts/{accountName}";
        }

        /// <summary>
        /// Retrieves Azure Storage Account blob container storage consumption metrics for the specified time range and granularity.
        /// </summary>
        /// <param name="startDateTime">Start date and time.</param>
        /// <param name="endDateTime">End date and time.</param>
        /// <param name="granularity">Time span.</param>
        /// <returns>List of metrics results.</returns>
        /// <example>
        /// RetrieveMetricsAsync(DateTime.UtcNow.AddHours(-1), DateTime.UtcNow, TimeSpan.FromHours(1)).Wait();
        /// </example>
        public async Task<List<QueryResultMetric>> RetrieveMetricsAsync(DateTimeOffset startDateTime, DateTimeOffset endDateTime, TimeSpan granularity)
        {
            // Start with Monitor query to get metrics data on Egress and Ingress.
            var options = new MetricsQueryOptions();
            options.TimeRange = new QueryTimeRange(startDateTime, endDateTime);
            options.Granularity = granularity;
            options.Aggregations.Clear();
            options.Aggregations.Add(MetricAggregationType.Total);
            options.Aggregations.Add(MetricAggregationType.Count);

            var response = await client.QueryResourceAsync(
                resourceId,
                new[] { "Transactions", "Egress", "Ingress" },
                options
            );

            var results = new List<QueryResultMetric>();

            foreach (var metric in response.Value.Metrics)
            {
                foreach (var timeSeriesElement in metric.TimeSeries)
                {
                    foreach (var data in timeSeriesElement.Values)
                    {
                        var met = new QueryResultMetric();
                        met.MetricName = metric.Unit.ToString();
                        met.ResourceType = metric.ResourceType;
                        met.ResourceId = metric.Id;
                        met.ResourceName = metric.Name;
                        met.TimeStamp = data.TimeStamp;
                        met.Total = data.Total;
                        met.Minimum = data.Minimum;
                        met.Maximum = data.Maximum;
                        met.Average = data.Average;
                        met.Count = data.Count;
                        results.Add(met);
                    }
                }
            }

            return results;
        }
    }
}
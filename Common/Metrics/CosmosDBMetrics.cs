// <copyright file="CosmosDBMetrics.cs" company="Moonrise Software, LLC">
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

    /// <summary>
    /// Represents a class for retrieving Cosmos DB metrics.
    /// </summary>
    public class CosmosDBMetrics
    {
        private MetricsQueryClient client;
        private string resourceId;

        /// <summary>
        /// Initializes a new instance of the <see cref="CosmosDBMetrics"/> class.
        /// </summary>
        /// <param name="subscriptionId">Subscription ID.</param>
        /// <param name="resourceGroupName">Resource group name.</param>
        /// <param name="accountName">Cosmos DB Account Name.</param>
        public CosmosDBMetrics(string subscriptionId, string resourceGroupName, string accountName)
        {
            this.client = new MetricsQueryClient(new DefaultAzureCredential());
            this.resourceId = $"/subscriptions/{subscriptionId}/resourceGroups/{resourceGroupName}/providers/Microsoft.DocumentDB/databaseAccounts/{accountName}";
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CosmosDBMetrics"/> class.
        /// </summary>
        /// <param name="credential">Token or default credential.</param>
        /// <param name="subscriptionId">Subscription ID.</param>
        /// <param name="resourceGroup">Resource group name.</param>
        /// <param name="accountName">Cosmos DB Account Name.</param>
        public CosmosDBMetrics(TokenCredential credential, string subscriptionId, string resourceGroup, string accountName)
        {
            client = new MetricsQueryClient(credential);
            resourceId = $"/subscriptions/{subscriptionId}/resourceGroups/{resourceGroup}/providers/Microsoft.DocumentDB/databaseAccounts/{accountName}";
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CosmosDBMetrics"/> class.
        /// </summary>
        /// <param name="subscriptionId">Subscription ID.</param>
        /// <param name="resourceGroup">Resource group name.</param>
        /// <param name="accountName">Cosmos DB Account Name.</param>
        /// <param name="tenantId">Tenant Id.</param>
        /// <param name="clientId">Registerd App. Client Id.</param>
        /// <param name="clientSecret">Registered App. secret.</param>
        public CosmosDBMetrics(string subscriptionId, string resourceGroup, string accountName, string tenantId, string clientId, string clientSecret)
        {
            var credential = new ClientSecretCredential(tenantId, clientId, clientSecret);
            client = new MetricsQueryClient(credential);
            resourceId = $"/subscriptions/{subscriptionId}/resourceGroups/{resourceGroup}/providers/Microsoft.DocumentDB/databaseAccounts/{accountName}";
        }

        /// <summary>
        /// Retrieves Cosmos DB storage and RU metrics for the specified time range and granularity.
        /// </summary>
        /// <param name="startDateTime">Start date and time.</param>
        /// <param name="endDateTime">End date and time.</param>
        /// <param name="granularity">Time span.</param>
        /// <returns>Query results list.</returns>
        /// <example>
        /// RetrieveMetricsAsync(DateTime.UtcNow.AddHours(-1), DateTime.UtcNow, TimeSpan.FromDays(1)).Wait();.
        /// </example>
        public async Task<List<QueryResultMetric>> RetrieveMetricsAsync(DateTimeOffset startDateTime, DateTimeOffset endDateTime, TimeSpan granularity)
        {
            return await RetrieveMetricsAsync(startDateTime, endDateTime, granularity, new string[] { "DataUsage", "IndexUsage", "TotalRequestUnits" });
        }

        /// <summary>
        /// Retrieves Cosmos DB storage and RU metrics for the specified time range, granularity, and metrics.
        /// </summary>
        /// <param name="startDateTime">Start date and time.</param>
        /// <param name="endDateTime">End date and time.</param>
        /// <param name="granularity">Time span.</param>
        /// <param name="metrics">Metric name array.</param>
        /// <returns>A list of metrics.</returns>
        public async Task<List<QueryResultMetric>> RetrieveMetricsAsync(DateTimeOffset startDateTime, DateTimeOffset endDateTime, TimeSpan granularity, IEnumerable<string> metrics)
        {
            var options = new MetricsQueryOptions();
            options.TimeRange = new QueryTimeRange(startDateTime, endDateTime);
            options.Granularity = granularity;
            options.Aggregations.Clear();
            options.Aggregations.Add(MetricAggregationType.Count);
            options.Aggregations.Add(MetricAggregationType.Minimum);
            options.Aggregations.Add(MetricAggregationType.Maximum);
            options.Aggregations.Add(MetricAggregationType.Average);
            options.Aggregations.Add(MetricAggregationType.Total);

            var response = await client.QueryResourceAsync(
                resourceId,
                metrics,
                options);

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

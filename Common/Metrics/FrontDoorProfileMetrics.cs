// <copyright file="FrontDoorProfileMetrics.cs" company="Moonrise Software, LLC">
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
    using Azure;
    using Azure.Core;
    using Azure.Identity;
    using Azure.Monitor.Query;
    using Azure.Monitor.Query.Models;

    /// <summary>
    /// Gets statistics from an Azure Front Door profile.
    /// </summary>
    public class FrontDoorProfileMetrics
    {
        private readonly LogsQueryClient client;
        private readonly string workspaceId;
        private readonly string query = @"
            AzureDiagnostics
            | where ResourceProvider == 'MICROSOFT.CDN' and Category == 'FrontDoorAccessLog'
            | summarize RequestBytes = sum(tolong(requestBytes_s)), ResponseBytes = sum(tolong(responseBytes_s)) by Date = format_datetime(endofday(bin(TimeGenerated, 1d)), 'yyyy-MM-dd'), Hostname = hostName_s";

        /// <summary>
        /// Initializes a new instance of the <see cref="FrontDoorProfileMetrics"/> class.
        /// </summary>
        /// <param name="logAnalyticsWorkspaceId">Log analytics workspace ID ID.</param>
        public FrontDoorProfileMetrics(Guid logAnalyticsWorkspaceId)
        {
            client = new LogsQueryClient(new DefaultAzureCredential());
            workspaceId = logAnalyticsWorkspaceId.ToString();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="FrontDoorProfileMetrics"/> class.
        /// </summary>
        /// <param name="logAnalyticsWorkspaceId">Log analytics workspace ID.</param>
        /// <param name="credential">Token credential.</param>
        public FrontDoorProfileMetrics(Guid logAnalyticsWorkspaceId, TokenCredential credential)
        {
            client = new LogsQueryClient(credential);
            workspaceId = logAnalyticsWorkspaceId.ToString();
        }

        /// <summary>
        /// Retrieves FrontDoor egress metrics for the specified time range.
        /// </summary>
        /// <param name="startDateTime">Start date and time.</param>
        /// <param name="endDateTime">End date and time.</param>
        /// <param name="hostName">Filter on a particular host name (optional).</param>
        /// <returns>EndPointMetric list.</returns>
        public async Task<List<EndPointMetric>> RetrieveMetricsAsync(DateTimeOffset startDateTime, DateTimeOffset endDateTime, string hostName = "")
        {
            var q = query;

            if (!string.IsNullOrEmpty(hostName))
            {
                q += q.Replace("Category == 'FrontDoorAccessLog'", $"Category == 'FrontDoorAccessLog' and hostName_s == '{hostName}'");
            }

            Response<LogsQueryResult> response = await client.QueryWorkspaceAsync(
            workspaceId,
            q,
            new QueryTimeRange(startDateTime, endDateTime));

            var data = new List<EndPointMetric>();

            foreach (var table in response.Value.AllTables)
            {
                foreach (var row in table.Rows)
                {
                    data.Add(new EndPointMetric
                    {
                        Date = DateOnly.FromDateTime(DateTime.Parse(row["Date"].ToString())),
                        ResponseBytes = row["ResponseBytes"] == null ? 0 : long.Parse(row["ResponseBytes"].ToString()),
                        RequestBytes = row["RequestBytes"] == null ? 0 : long.Parse(row["RequestBytes"].ToString()),
                        Host = row["Hostname"] == null ? string.Empty : row["Hostname"].ToString()
                    });
                }
            }

            return data;
        }
    }
}

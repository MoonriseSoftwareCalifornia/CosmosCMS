using Azure.Core;
using Azure.Identity;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Blobs;
using Cosmos.Common.Metrics;
using Cosmos.DynamicConfig;
using Microsoft.Azure.Functions.Worker;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
namespace Cosmos.MultiTenant.Metrics
{
    public class GetMetrics
    {
        private readonly ILogger _logger;
        private readonly IConfiguration configuration;

        /// <summary>
        /// Creates a new instance of the function.
        /// </summary>
        /// <param name="loggerFactory"></param>
        /// <param name="configuration"></param>
        public GetMetrics(ILoggerFactory loggerFactory, IConfiguration configuration)
        {
            _logger = loggerFactory.CreateLogger<GetMetrics>();
            this.configuration = configuration;
        }

        /// <summary>
        /// This schedule will trigger the function at 9:30 AM and 10:30 AM daily.
        /// </summary>
        /// <param name="myTimer"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="ArgumentException"></exception>
        /// <remarks>
        /// •	0: The seconds field, set to 0.
        /// •	30: The minutes field, set to 30 (runs at 9:30 AM and 10:30 AM).
        /// •	9,10: The hours field, specifying 9 AM and 10 AM.
        /// •	* * *: The day, month, and day-of-week fields, meaning it runs every day.
        /// </remarks>
        [Function("Function1")]
        public async Task Run([TimerTrigger("0 30 9,10 * * *")] TimerInfo myTimer)
        {
            _logger.LogInformation($"C# Timer trigger function executed at: {DateTime.Now}");

            var connectionString = configuration.GetConnectionString("DefaultConnection");
            if (connectionString == null)
            {
                throw new ArgumentNullException("DefaultConnection not found.");
            }

            var logAnalyticsWorkspaceId = configuration["LogWorkspaceId"];
            if (logAnalyticsWorkspaceId == null)
            {
                throw new ArgumentException("LogWorkspaceId not found.");
            }

            var subscriptionId = configuration["SubscriptionId"];
            if (subscriptionId == null)
            {
                throw new ArgumentException("SubscriptionId not found.");
            }

            var clientSecret = configuration["ClientSecret"];
            var clientId = configuration["ClientId"];
            var tenantId = configuration["TenantId"];

            var dbOptions = new DbContextOptionsBuilder<Cosmos.DynamicConfig.DynamicConfigDbContext>()
                .UseCosmos(connectionString: connectionString, databaseName: "configs")
                .Options;

            using var dbContext = new DynamicConfigDbContext(dbOptions);
            await dbContext.Database.EnsureCreatedAsync();
            var connections = await dbContext.Connections.ToListAsync();

            DateTimeOffset? lastRunDateTime = await dbContext.Metrics.OrderByDescending(o => o.TimeStamp).Select(s => s.TimeStamp).FirstOrDefaultAsync();
            DateTimeOffset now = new DateTimeOffset(DateTimeOffset.UtcNow.Year, DateTimeOffset.UtcNow.Month, DateTimeOffset.UtcNow.Day, 0, 0, 0, 0, TimeSpan.Zero);

            if (lastRunDateTime != null && !IsAtLeastOneDayBehind(lastRunDateTime.Value, now))
            {
                _logger.LogInformation($"Last run date {lastRunDateTime} is not more than a day behind {now}. Exiting.");
                return;
            }

            // This is the state date and time.
            var baseDate = new DateTimeOffset(new DateTime(2025, 5, 1, 0, 0, 0, 0, 0, DateTimeKind.Utc));

            DateTimeOffset startDateTime;
            if (lastRunDateTime == null || lastRunDateTime < baseDate)
            {
                startDateTime = baseDate;
            }
            else
            {
                startDateTime = new DateTimeOffset(new DateTime(lastRunDateTime.Value.Year, lastRunDateTime.Value.Month, lastRunDateTime.Value.Day, 0, 0, 0, 0, 0, DateTimeKind.Utc)).AddDays(1);
            }

            TokenCredential tokenCredential = string.IsNullOrEmpty(clientSecret) ? new DefaultAzureCredential()
                : new ClientSecretCredential(tenantId, clientId, clientSecret);


            var frontDoorMetrics = new FrontDoorProfileMetrics(Guid.Parse(logAnalyticsWorkspaceId), tokenCredential);
            var frontDoorResults = frontDoorMetrics.RetrieveMetricsAsync(startDateTime, startDateTime.AddDays(1)).Result;

            var dates = GetDateRange(startDateTime.ToUniversalTime(), DateTimeOffset.UtcNow.AddDays(-1));

            // Loop through all the tests.
            foreach (var date in dates)
            {
                await GetStatsForTheDay(dbContext, subscriptionId, logAnalyticsWorkspaceId, tokenCredential, date, connections, frontDoorResults);
                _logger.LogInformation($"Added metrics for {date.ToShortDateString()}.");
            }
            

            if (myTimer.ScheduleStatus is not null)
            {
                _logger.LogInformation($"Next timer schedule at: {myTimer.ScheduleStatus.Next}");
            }
        }

        private async Task GetStatsForTheDay(DynamicConfigDbContext dbContext, string subscriptionId, string logAnalyticsWorkspaceId, TokenCredential tokenCredential, DateTimeOffset startDateTime, IEnumerable<DynamicConfig.Connection> connections, List<EndPointMetric> frontDoorResults)
        {
            foreach (var connection in connections)
            {
                var metric = await dbContext.Metrics
                    .Where(x => x.ConnectionId == connection.Id && x.TimeStamp == startDateTime)
                    .FirstOrDefaultAsync();

                if (metric == null)
                {
                    metric = new Metric()
                    {
                        ConnectionId = connection.Id,
                        TimeStamp = startDateTime,
                    };
                }

                // Front Door Metrics
                GetFrontDoorMetrics(ref metric, connection, frontDoorResults);

                // Cosmos DB Metrics
                GetCosmosDbMetrics(ref metric, connection, tokenCredential, subscriptionId, startDateTime);

                // Storage Account Metrics
                GetStorageMetrics(ref metric, connection, tokenCredential, subscriptionId, startDateTime);

                dbContext.Add(metric);
                await dbContext.SaveChangesAsync();
            }
        }

        private void GetFrontDoorMetrics(ref Metric metric, DynamicConfig.Connection connection, List<EndPointMetric> frontDoorResults)
        {
            var hostName = GetDomainNameFromUrl(connection.WebsiteUrl);
            var frontDoorResult = frontDoorResults.FirstOrDefault(x => x.Host.ToLower() == hostName.ToLower());
            if (frontDoorResult != null)
            {
                metric.FrontDoorResponseBytes = frontDoorResult.ResponseBytes;
                metric.FrontDoorRequestBytes = frontDoorResult.RequestBytes;
            }
        }

        private string GetDomainNameFromUrl(string url)
        {
            Uri uri = new Uri(url);
            return uri.Host.ToLower();
        }

        private void GetCosmosDbMetrics(ref Metric metric, DynamicConfig.Connection connection, TokenCredential credential, string subscriptionId, DateTimeOffset startDateTime)
        {
            var dbAccountName = GetCosmosAccountName(connection.DbConn);
            var cosmosDbMetrics = new CosmosDBMetrics(credential, subscriptionId, connection.ResourceGroup, dbAccountName);
            var cosmosDbResults = cosmosDbMetrics.RetrieveMetricsAsync(startDateTime, DateTimeOffset.UtcNow, TimeSpan.FromHours(1)).Result;

            /*
                Choosing the Right Aggregation
                * Use Average for trends.
                * Use Maximum/Minimum for thresholds and alerting.
                * Use Total for cumulative metrics.
            */

            // Cumulative metric.
            metric.DatabaseRuUsage = cosmosDbResults.Where(x => x.ResourceName == "TotalRequestUnits")
                .Sum(x => x.Total);

            // Threadshold metric.
            metric.DatabaseDataUsageBytes = cosmosDbResults.Where(x => x.ResourceName == "DataUsage")
                .Max(x => x.Maximum);

            // Threadshold metric.
            metric.DatabaseIndexUsageBytes = cosmosDbResults.Where(x => x.ResourceName == "IndexUsage")
                .Max(x => x.Maximum);
        }

        private string GetCosmosAccountName(string connectionString)
        {
            // Split the connection string into parts
            var parts = connectionString.Split(';');
            string endpoint = parts.FirstOrDefault(part => part.StartsWith("AccountEndpoint=", StringComparison.OrdinalIgnoreCase))
                    ?.Substring("AccountEndpoint=".Length) ?? string.Empty; ;

            if (endpoint != null)
            {
                var uri = new Uri(endpoint);
                var host = uri.Host; // e.g., multi-tenant-westus-01-db.documents.azure.com
                var accountName = host.Split('.')[0]; // e.g., multi-tenant-westus-01-db

                return accountName;
            }
            else
            {
                return string.Empty;
            }
        }

        private void GetStorageMetrics(ref Metric metric, DynamicConfig.Connection connection, TokenCredential credential, string subscriptionId, DateTimeOffset startDateTime)
        {
            var accountName = GetStorageAccountName(connection.StorageConn);
            var storageMetrics = new StorageAccountMetrics(credential, subscriptionId, connection.ResourceGroup, accountName);

            var storageResults = storageMetrics.RetrieveMetricsAsync(startDateTime, DateTimeOffset.UtcNow, TimeSpan.FromHours(1)).Result;

            // Threadshold metric.
            metric.BlobStorageBytes = CalculateTotalBlobSizeAsync(connection.StorageConn).Result;

            // Cumulative metric.
            metric.BlobStorageEgressBytes = storageResults.Where(x => x.ResourceName == "Egress")
                .Max(x => x.Total);

            // Cumulative metric.
            metric.BlobStorageIngressBytes = storageResults.Where(x => x.ResourceName == "Ingress")
                .Max(x => x.Total);
        }

        private async Task<long> CalculateTotalBlobSizeAsync(string connectionString)
        {
            long totalBytes = 0;

            BlobServiceClient serviceClient = new BlobServiceClient(connectionString);

            await foreach (BlobContainerItem container in serviceClient.GetBlobContainersAsync())
            {
                BlobContainerClient containerClient = serviceClient.GetBlobContainerClient(container.Name);

                await foreach (BlobItem blobItem in containerClient.GetBlobsAsync())
                {
                    totalBytes += blobItem.Properties.ContentLength ?? 0;
                }
            }

            return totalBytes;
        }

        private string GetStorageAccountName(string connectionString)
        {
            // Split the connection string into parts

            var parts = connectionString.Split(';');

            return parts.FirstOrDefault(part => part.StartsWith("AccountName=", StringComparison.OrdinalIgnoreCase))
                ?.Substring("AccountName=".Length) ?? string.Empty;
        }

        private List<DateTime> GetDateRange(DateTimeOffset startDate, DateTimeOffset endDate)
        {
            var dates = new List<DateTime>();
            for (var date = startDate.DateTime; date <= endDate.Date; date = date.AddDays(1))
            {
                dates.Add(date);
            }
            return dates;
        }

        private bool IsAtLeastOneDayBehind(DateTimeOffset firstDateTime, DateTimeOffset secondDateTime)
        {
            return (secondDateTime - firstDateTime).TotalDays >= 1;
        }
    }
}

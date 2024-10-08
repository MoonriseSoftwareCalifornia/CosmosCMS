namespace Cosmos.Common.Data
{
    using System.Linq;
    using Azure.Identity;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.DependencyInjection;

    /// <summary>
    /// Cosmos DB service collection extensions.
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Add Cosmos DB to the service collection.
        /// </summary>
        /// <param name="services">Current application services collection.</param>
        /// <param name="connectionString">Current connection string.</param>
        /// <param name="databaseName">Database name.</param>
        /// <param name="cosmosRegionName">Region where the database is.</param>
        public static void AddCosmosDbContext(this IServiceCollection services, string connectionString, string databaseName, string cosmosRegionName = "")
        {
            var conparts = connectionString.Split(';');
            var conpartsDict = conparts.Where(w => !string.IsNullOrEmpty(w)).Select(part => part.Split('=')).ToDictionary(sp => sp[0], sp => sp[1]);

            if (conpartsDict["AccountKey"] == "AccessToken")
            {
                var endpoint = conpartsDict["AccountEndpoint"];
                services.AddDbContext<ApplicationDbContext>(options =>
                {
                    if (string.IsNullOrEmpty(cosmosRegionName))
                    {
                        options.UseCosmos(endpoint, new DefaultAzureCredential(), databaseName);
                    }
                    else
                    {
                        options.UseCosmos(endpoint, new DefaultAzureCredential(), databaseName, cosmosOps => cosmosOps.Region(cosmosRegionName));
                    }
                });
            }
            else
            {
                services.AddDbContext<ApplicationDbContext>(options =>
                {
                    if (string.IsNullOrEmpty(cosmosRegionName))
                    {
                        options.UseCosmos(connectionString, databaseName);
                    }
                    else
                    {
                        options.UseCosmos(connectionString, databaseName, cosmosOps => cosmosOps.Region(cosmosRegionName));
                    }
                });
            }
        }

        /// <summary>
        /// Get a configured Cosmos DbContextOptionsBuilder.
        /// </summary>
        /// <param name="connectionString">Current connection string.</param>
        /// <param name="databaseName">Database name.</param>
        /// <param name="cosmosRegionName">Region where the database is.</param>
        /// <returns>Configured DbContextOptionsBuilder.</returns>
        public static DbContextOptions<ApplicationDbContext> Get(string connectionString, string databaseName, string cosmosRegionName = "")
        {
            var builder = new DbContextOptionsBuilder<ApplicationDbContext>();

            var conparts = connectionString.Split(';');
            var conpartsDict = conparts.Where(w => !string.IsNullOrEmpty(w)).Select(part => part.Split('=')).ToDictionary(sp => sp[0], sp => sp[1]);

            if (conpartsDict["AccountKey"] == "AccessToken")
            {
                var endpoint = conpartsDict["AccountEndpoint"];
                if (string.IsNullOrEmpty(cosmosRegionName))
                {
                    builder.UseCosmos(endpoint, new DefaultAzureCredential(), databaseName);
                }
                else
                {
                    builder.UseCosmos(endpoint, new DefaultAzureCredential(), databaseName, cosmosOps => cosmosOps.Region(cosmosRegionName));
                }
            }
            else
            {
                if (string.IsNullOrEmpty(cosmosRegionName))
                {
                    builder.UseCosmos(connectionString, databaseName);
                }
                else
                {
                    builder.UseCosmos(connectionString, databaseName, cosmosOps => cosmosOps.Region(cosmosRegionName));
                }
            }

            return builder.Options;
        }
    }
}

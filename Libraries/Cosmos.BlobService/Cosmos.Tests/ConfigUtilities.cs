using Cosmos.BlobService.Config;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using System;
using System.IO;
using System.Reflection;

namespace Cosmos.Tests
{
    public class ConfigUtilities
    {
        private static string GetKeyValue(IConfigurationRoot config, string key)
        {
            var data = config[key];
            if (string.IsNullOrEmpty(data))
            {
                data = Environment.GetEnvironmentVariable(key);
                if (string.IsNullOrEmpty(data))
                {
                    data = Environment.GetEnvironmentVariable(key.ToUpper());
                }
            }
            return data;
        }

        internal static IOptions<CosmosStorageConfig> GetCosmosConfig()
        {
            // the type specified here is just so the secrets library can 
            // find the UserSecretId we added in the csproj file
            //var jsonConfig = Path.Combine(Environment.CurrentDirectory, "appsettings.json");

            var builder = new ConfigurationBuilder()
                //.AddJsonFile(jsonConfig, true)
                .AddUserSecrets(Assembly.GetExecutingAssembly(), true); // User secrets override all - put here

            var configRoot = Retry.Do(() => builder.Build(), TimeSpan.FromSeconds(1));

            // From either local secrets or app config, get connection info for Azure Vault.
            var tenantId = GetKeyValue(configRoot, "CosmosAzureVaultTenantId");

            var clientId = GetKeyValue(configRoot, "CosmosAzureVaultClientId");

            var key = GetKeyValue(configRoot, "CosmosAzureVaultClientSecret");

            var vaultUrl = GetKeyValue(configRoot, "CosmosAzureVaultUrl");

            var secretName = GetKeyValue(configRoot, "CosmosSecretName");

            var config = new CosmosStorageConfig();
            config.PrimaryCloud = "azure";
            config.StorageConfig = new StorageConfig();

            config.StorageConfig.AmazonConfigs.Add(new AmazonStorageConfig()
            {
                AmazonAwsAccessKeyId = GetKeyValue(configRoot, "AmazonAwsAccessKeyId"),
                AmazonAwsSecretAccessKey = GetKeyValue(configRoot, "AmazonAwsSecretAccessKey"),
                AmazonBucketName = GetKeyValue(configRoot, "AmazonBucketName"),
                AmazonRegion = GetKeyValue(configRoot, "AmazonRegion"),
                ProfileName = "aws",
                ServiceUrl = GetKeyValue(configRoot, "AmazonServiceUrl")
            });

            config.StorageConfig.AzureConfigs.Add(new AzureStorageConfig()
            {
                AzureBlobStorageConnectionString = GetKeyValue(configRoot, "AzureBlobStorageConnectionString"),
                AzureBlobStorageContainerName = GetKeyValue(configRoot, "AzureBlobStorageContainerName"),
                AzureBlobStorageEndPoint = GetKeyValue(configRoot, "AzureBlobStorageEndPoint"),
                AzureFileShare = GetKeyValue(configRoot, "AzureFileShare")
            });

            return Options.Create(config);
        }

        public static IMemoryCache GetMemoryCache()
        {
            var options = Options.Create(new MemoryCacheOptions()
            {
                SizeLimit = 20000000 // 20 megabytes decimal
            });
            return new MemoryCache(options);
        }
    }
}
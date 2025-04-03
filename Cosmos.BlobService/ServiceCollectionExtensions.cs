// <copyright file="ServiceCollectionExtensions.cs" company="Moonrise Software, LLC">
// Copyright (c) Moonrise Software, LLC. All rights reserved.
// Licensed under the GNU Public License, Version 3.0 (https://www.gnu.org/licenses/gpl-3.0.html)
// See https://github.com/MoonriseSoftwareCalifornia/CosmosCMS
// for more information concerning the license and the contributors participating to this project.
// </copyright>

namespace Cosmos.BlobService
{
    using System;
    using System.Linq;
    using Azure.Identity;
    using Azure.Storage.Blobs;
    using Cosmos.BlobService.Config;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Options;

    /// <summary>
    /// Adds the Cosmos Storage Context to the Services Collection.
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Adds the storage context to the services collection.
        /// </summary>
        /// <param name="services">Services collection.</param>
        /// <param name="config">Startup configuration.</param>
        public static void AddCosmosStorageContext(this IServiceCollection services, IConfiguration config)
        {
            // Azure Parameters
            var azureBlobStorageConnectionString = config.GetConnectionString("AzureBlobStorageConnectionString");

            if (string.IsNullOrEmpty(azureBlobStorageConnectionString))
            {
                azureBlobStorageConnectionString = GetKeyValue(config, "AzureBlobStorageConnectionString");
            }

            var azureBlobStorageContainerName = GetKeyValue(config, "AzureBlobStorageContainerName");
            var azureBlobStorageEndPoint = GetKeyValue(config, "AzureBlobStorageEndPoint");

            var cosmosConfig = new CosmosStorageConfig();

            cosmosConfig.PrimaryCloud = "azure";
            cosmosConfig.StorageConfig = new StorageConfig();

            if (string.IsNullOrEmpty(azureBlobStorageContainerName))
            {
                azureBlobStorageContainerName = "$web";
            }

            if (string.IsNullOrEmpty(azureBlobStorageConnectionString) == false &&
                string.IsNullOrEmpty(azureBlobStorageContainerName) == false &&
                string.IsNullOrEmpty(azureBlobStorageEndPoint) == false)
            {
                cosmosConfig.StorageConfig.AzureConfigs.Add(new AzureStorageConfig()
                {
                    AzureBlobStorageConnectionString = azureBlobStorageConnectionString,
                    AzureBlobStorageContainerName = azureBlobStorageContainerName,
                    AzureBlobStorageEndPoint = azureBlobStorageEndPoint
                });
            }

            services.AddSingleton(Options.Create(cosmosConfig));
            services.AddTransient<StorageContext>();
        }

        /// <summary>
        /// Adds the storage context to the services collection.
        /// </summary>
        /// <param name="config">Startup configuration.</param>
        /// <param name="defaultAzureCredential">Default Azure token credential.</param>
        /// <param name="container">The container to use.</param>
        /// <returns>Blob service client.</returns>
        public static BlobContainerClient GetBlobContainerClient(IConfiguration config, DefaultAzureCredential defaultAzureCredential, string container = "$web")
        {
            var connectionString = config.GetConnectionString("AzureBlobStorageConnectionString");
            var conparts = connectionString.Split(';');
            var conpartsDict = conparts.Where(w => !string.IsNullOrEmpty(w)).Select(part => part.Split('=')).ToDictionary(sp => sp[0], sp => sp[1]);

            BlobServiceClient blobServiceClient = null;

            if (conpartsDict["AccountKey"] == "AccessToken")
            {
                var accountName = conpartsDict["AccountName"];
                blobServiceClient = new BlobServiceClient(new Uri($"https://{accountName}.blob.core.windows.net/"), defaultAzureCredential);
            }
            else
            {
                blobServiceClient = new BlobServiceClient(connectionString);
            }

            return blobServiceClient.GetBlobContainerClient(container);
        }

        private static string GetKeyValue(IConfiguration config, string key)
        {
            var data = (config is IConfigurationRoot) ? ((IConfigurationRoot)config)[key] : config[key];

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
    }
}

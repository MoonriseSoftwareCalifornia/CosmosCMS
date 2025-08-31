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
    using Microsoft.AspNetCore.Builder;
    using Microsoft.AspNetCore.DataProtection;
    using Microsoft.AspNetCore.DataProtection.AuthenticatedEncryption;
    using Microsoft.AspNetCore.DataProtection.AuthenticatedEncryption.ConfigurationModel;
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
            var isMultiTenant = config.GetValue<bool?>("MultiTenantEditor") ?? false;

            var cosmosConfig = new CosmosStorageConfig();

            cosmosConfig.PrimaryCloud = "azure";
            cosmosConfig.StorageConfig = new StorageConfig();

            if (string.IsNullOrEmpty(azureBlobStorageContainerName))
            {
                azureBlobStorageContainerName = "$web";
            }

            if (string.IsNullOrEmpty(azureBlobStorageEndPoint))
            {
                azureBlobStorageEndPoint = "/";
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
        /// Adds the data protection service for Cosmos CMS to the services collection.
        /// </summary>
        /// <param name="services">Service collection.</param>
        /// <param name="config">Configuration.</param>
        /// <param name="defaultAzureCredential">Default azure credential.</param>
        /// <exception cref="ArgumentNullException">Returns error if no connection string found.</exception>
        public static void AddCosmosCmsDataProtection(this IServiceCollection services, IConfiguration config, DefaultAzureCredential defaultAzureCredential)
        {
            var multi = config.GetValue<bool?>("MultiTenantEditor") ?? false;

            var connectionString = multi ? config.GetConnectionString("DataProtectionStorage")
                : config.GetConnectionString("AzureBlobStorageConnectionString");

            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new ArgumentNullException("DataProtectionStorage", "'DataProtectionStorage' or 'AzureBlobStorageConnectionString' connection string is not set.");
            }

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

            var containerClient = blobServiceClient.GetBlobContainerClient("dpkeys");
            containerClient.CreateIfNotExists();
            var blobClient = containerClient.GetBlobClient("keys.xml");

            services.AddDataProtection()
                .UseCryptographicAlgorithms(
                new AuthenticatedEncryptorConfiguration
                {
                    EncryptionAlgorithm = EncryptionAlgorithm.AES_256_CBC,
                    ValidationAlgorithm = ValidationAlgorithm.HMACSHA256
                })
                .PersistKeysToAzureBlobStorage(blobClient);
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

        /// <summary>
        /// Sets the application discriminator for the data protection keys based on the domain name.
        /// </summary>
        /// <param name="app">Application builder.</param>
        /// <returns>IApplicationBuilder.</returns>
        public static IApplicationBuilder UseCosmosCmsDataProtection(this IApplicationBuilder app)
        {
            app.Use(async (context, next) =>
            {
                var dataProtectionOptions = context.RequestServices.GetRequiredService<IOptions<DataProtectionOptions>>().Value;

                // Set the ApplicationDiscriminator based on the domain name
                var domainName = context.Request.Host.Host;
                dataProtectionOptions.ApplicationDiscriminator = domainName;

                await next();
            });

            return app;
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

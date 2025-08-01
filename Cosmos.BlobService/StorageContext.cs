﻿// <copyright file="StorageContext.cs" company="Moonrise Software, LLC">
// Copyright (c) Moonrise Software, LLC. All rights reserved.
// Licensed under the GNU Public License, Version 3.0 (https://www.gnu.org/licenses/gpl-3.0.html)
// See https://github.com/MoonriseSoftwareCalifornia/CosmosCMS
// for more information concerning the license and the contributors participating to this project.
// </copyright>

namespace Cosmos.BlobService
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;
    using Azure.Identity;
    using Azure.Storage.Blobs.Specialized;
    using Cosmos.BlobService.Config;
    using Cosmos.BlobService.Drivers;
    using Cosmos.BlobService.Models;
    using Cosmos.DynamicConfig;
    using Microsoft.Extensions.Caching.Memory;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Options;

    /// <summary>
    ///     Multi cloud blob service context.
    /// </summary>
    public sealed class StorageContext
    {
        /// <summary>
        /// Default Azure token credential.
        /// </summary>
        private readonly DefaultAzureCredential credential;

        /// <summary>
        /// Used to brefly store chuk data while uploading.
        /// </summary>
        private readonly IMemoryCache memoryCache;

        /// <summary>
        /// Represents a provider for dynamic configuration settings.
        /// </summary>
        /// <remarks>This field holds a reference to an implementation of <see
        /// cref="IDynamicConfigurationProvider"/>. It is used to access configuration settings that can change at
        /// runtime.</remarks>
        private readonly IDynamicConfigurationProvider dynamicConfigurationProvider;

        /// <summary>
        /// Multi-tenant editor flag.
        /// </summary>
        private bool isMultiTenant;

        /// <summary>
        /// Cosmos storage configuration.
        /// </summary>
        private IOptions<CosmosStorageConfig> config;

        /// <summary>
        /// Drivers for the storage context.
        /// </summary>
        private List<ICosmosStorage> drivers = new List<ICosmosStorage>();

        private ICosmosStorage primaryDriver;

        /// <summary>
        /// Initializes a new instance of the <see cref="StorageContext"/> class for multitenant instances.
        /// </summary>
        /// <param name="cosmosConfig">Storage context configuration as a <see cref="CosmosStorageConfig"/>.</param>
        /// <param name="cache"><see cref="IMemoryCache"/> used by context.</param>
        /// <param name="defaultAzureCredential">Default Azure Credential.</param>
        /// <param name="dynamicConfiguration">Configuration.</param>
        public StorageContext(
            IOptions<CosmosStorageConfig> cosmosConfig,
            IMemoryCache cache,
            DefaultAzureCredential defaultAzureCredential,
            IDynamicConfigurationProvider dynamicConfiguration)
        {
            this.config = cosmosConfig;
            this.memoryCache = cache;
            this.credential = defaultAzureCredential;
            this.isMultiTenant = true;
            this.dynamicConfigurationProvider = dynamicConfiguration;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="StorageContext"/> class for single tenant instances.
        /// </summary>
        /// <param name="cosmosConfig">Storage context configuration as a <see cref="CosmosStorageConfig"/>.</param>
        /// <param name="cache"><see cref="IMemoryCache"/> used by context.</param>
        /// <param name="defaultAzureCredential">Default Azure Credential.</param>
        public StorageContext(
            IOptions<CosmosStorageConfig> cosmosConfig,
            IMemoryCache cache,
            DefaultAzureCredential defaultAzureCredential)
        {
            this.config = cosmosConfig;
            this.memoryCache = cache;
            this.credential = defaultAzureCredential;
            this.isMultiTenant = false;
        }

        /// <summary>
        ///     Determine if this service is configured.
        /// </summary>
        /// <returns>Returns a <see cref="bool"/> indicating context is configured.</returns>
        public bool IsConfigured()
        {
            // Are there configuration settings at all?
            if (this.config.Value == null || this.config.Value.StorageConfig == null)
            {
                return false;
            }

            // If both AWS and Azure blob storage settings are provided,
            // make sure the distributed cache is provided and there is a primary provider.
            if (this.config.Value.StorageConfig.AmazonConfigs.Any() && this.config.Value.StorageConfig.AzureConfigs.Any())
            {
                return this.memoryCache != null && string.IsNullOrEmpty(this.config.Value.PrimaryCloud) == false;
            }

            // If just AWS is configured, make sure distributed cache exists, as this is needed for uploading files.
            if (this.config.Value.StorageConfig.AmazonConfigs.Any())
            {
                return this.memoryCache != null;
            }

            // Finally, make sure at the very least, Azure blob storage is
            return this.config.Value.StorageConfig.AzureConfigs.Any();
        }

        /// <summary>
        ///     Determines if a blob exists.
        /// </summary>
        /// <param name="path">Path check for a blob.</param>
        /// <returns><see cref="bool"/> indicating existence.</returns>
        public async Task<bool> BlobExistsAsync(string path)
        {
            var driver = this.GetPrimaryDriver();
            return await driver.BlobExistsAsync(path);
        }

        /// <summary>
        ///     Copies a file or folder.
        /// </summary>
        /// <param name="target">Path to the file or folder to copy.</param>
        /// <param name="destination">Path to where to make the copy.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public async Task CopyAsync(string target, string destination)
        {
            await this.CopyObjectsAsync(target, destination, false);
        }

        /// <summary>
        ///     Delete a folder.
        /// </summary>
        /// <param name="path">Path to folder.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public async Task DeleteFolderAsync(string path)
        {
            // Ensure leading slash is removed.
            path = path.TrimStart('/');

            var blobs = await this.GetBlobNamesByPath(path);

            foreach (var blob in blobs)
            {
                this.DeleteFile(blob);
            }
        }

        /// <summary>
        ///     Deletes a file.
        /// </summary>
        /// <param name="path">Path to file.</param>
        public void DeleteFile(string path)
        {
            // Ensure leading slash is removed.
            path = path.TrimStart('/');

            var drivers = this.GetDrivers();
            var tasks = new List<Task>();
            foreach (var cosmosStorage in drivers)
            {
                tasks.Add(cosmosStorage.DeleteIfExistsAsync(path));
            }

            Task.WaitAll(tasks.ToArray());
        }

        /// <summary>
        /// Enables the Azure BLOB storage static website.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public async Task EnableAzureStaticWebsite()
        {
            var drivers = this.GetDrivers();

            foreach (var driver in drivers)
            {
                if (driver.GetType() == typeof(AzureStorage))
                {
                    var azureStorage = (AzureStorage)driver;
                    await azureStorage.EnableStaticWebsite();
                }
            }
        }

        /// <summary>
        /// Disables the static website (when login is required for example).
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public async Task DisableAzureStaticWebsite()
        {
            var drivers = this.GetDrivers();

            foreach (var driver in drivers)
            {
                if (driver.GetType() == typeof(AzureStorage))
                {
                    var azureStorage = (AzureStorage)driver;
                    await azureStorage.DisableStaticWebsite();
                }
            }
        }

        /// <summary>
        /// Gets and Azure storage append blob client.
        /// </summary>
        /// <param name="path">Path to blob.</param>
        /// <returns>Returns an <see cref="AppendBlobClient"/>.</returns>
        public AppendBlobClient GetAppendBlobClient(string path)
        {
            var drivers = this.GetDrivers();

            foreach (var driver in drivers)
            {
                if (driver.GetType() == typeof(AzureStorage))
                {
                    var azureStorage = (AzureStorage)driver;
                    return azureStorage.GetAppendBlobClient(path);
                }
            }

            return null;
        }

        /// <summary>
        /// Gets total bytes consumed for a storage account.
        /// </summary>
        /// <returns>Number of bytes as a <see cref="long"/>.</returns>
        public async Task<long> GetBytesConsumed()
        {
            var drivers = this.GetDrivers();
            long consumption = 0;
            foreach (var driver in drivers)
            {
                consumption += await driver.GetBytesConsumed();
            }

            return consumption;
        }

        /// <summary>
        ///     Gets the metadata for a file.
        /// </summary>
        /// <param name="path">Path to file.</param>
        /// <returns>File metadata as a <see cref="FileManagerEntry"/>.</returns>
        public async Task<FileManagerEntry> GetFileAsync(string path)
        {
            // Ensure leading slash is removed.
            path = path.TrimStart('/');

            var driver = (AzureStorage)this.GetPrimaryDriver();
            var blob = await driver.GetBlobAsync(path);
            var isDirectory = blob.Name.EndsWith("folder.stubxx");
            var hasDirectories = false;
            var fileName = Path.GetFileName(blob.Name);
            var blobName = blob.Name;

            if (isDirectory)
            {
                var children = await driver.GetObjectsAsync(path);
                hasDirectories = children.Any(c => c.IsPrefix);
                fileName = this.ParseFirstFolder(blob.Name)[0];
                blobName = blobName?.Replace("/folder.stubxx", string.Empty);
            }

            var props = await blob.GetPropertiesAsync();

            var fileManagerEntry = new FileManagerEntry
            {
                Created = props.Value.CreatedOn.DateTime,
                CreatedUtc = props.Value.CreatedOn.UtcDateTime,
                Extension = isDirectory ? string.Empty : Path.GetExtension(blob.Name),
                HasDirectories = hasDirectories,
                IsDirectory = isDirectory,
                Modified = props.Value.LastModified.DateTime,
                ModifiedUtc = props.Value.LastModified.UtcDateTime,
                Name = fileName,
                Path = blobName,
                Size = props.Value.ContentLength
            };

            return fileManagerEntry;
        }

        /// <summary>
        ///     Returns a response stream from the primary blob storage provider.
        /// </summary>
        /// <param name="path">Path to blob to open read stream from.</param>
        /// <returns>Data as a <see cref="Stream"/>.</returns>
        public async Task<Stream> OpenBlobReadStreamAsync(string path)
        {
            // Ensure leading slash is removed.
            path = path.TrimStart('/');

            // Get the primary driver based on the configuration.
            var driver = (AzureStorage)this.GetPrimaryDriver();
            var blob = await driver.GetBlobAsync(path);
            return await blob.OpenReadAsync();
        }

        /// <summary>
        ///     Renames a file or folder.
        /// </summary>
        /// <param name="path">Path to file or folder.</param>
        /// <param name="destination">The new name or path.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public async Task RenameAsync(string path, string destination)
        {
            await this.CopyObjectsAsync(path, destination, true);
        }

        /// <summary>
        ///     Append bytes to blob(s).
        /// </summary>
        /// <param name="stream"><see cref="MemoryStream"/> containing data being appended.</param>
        /// <param name="fileMetaData"><see cref="FileUploadMetaData"/> containing metadata about the data 'chunk' and blob.</param>
        /// <param name="mode">Is either append or block.</param>
        public void AppendBlob(MemoryStream stream, FileUploadMetaData fileMetaData, string mode = "append")
        {
            var mark = DateTimeOffset.UtcNow;

            // Gets the primary driver based on the configuration.
            var drivers = this.GetDrivers();

            var data = stream.ToArray();
            var uploadTasks = new List<Task>();
            foreach (var cosmosStorage in drivers)
            {
                var cloneArray = data.ToArray();

                if (stream.Length < 30000000)
                {
                    // If the stream is less than 30MB, we can use the block blob.
                    mode = "block";
                }
                else
                {
                    // If the stream is larger than 30MB, we use append blob.
                    mode = "append";
                }

                uploadTasks.Add(cosmosStorage.AppendBlobAsync(cloneArray, fileMetaData, mark, mode));
            }

            // Wait for all the tasks to complete
            Task.WaitAll(uploadTasks.ToArray());
        }

        /// <summary>
        ///     Creates a folder in all the cloud storage accounts.
        /// </summary>
        /// <param name="path">Path to the folder to create.</param>
        /// <returns>Folder metadata as a <see cref="FileManagerEntry"/>.</returns>
        /// <remarks>Creates the folder if it does not already exist.</remarks>
        public FileManagerEntry CreateFolder(string path)
        {
            var drivers = this.GetDrivers();

            var tasks = new List<Task>();

            // Gets the primary driver based on the configuration.
            var primary = this.GetPrimaryDriver();

            if (!primary.BlobExistsAsync(path + "/folder.stubxx").Result)
            {
                foreach (var cosmosStorage in drivers)
                {
                    tasks.Add(cosmosStorage.CreateFolderAsync(path));
                }

                Task.WaitAll(tasks.ToArray());
                var parts = path.TrimEnd('/').Split('/');

                return new FileManagerEntry
                {
                    Name = parts.Last(),
                    Path = path,
                    Created = DateTime.UtcNow,
                    CreatedUtc = DateTime.UtcNow,
                    Extension = string.Empty,
                    HasDirectories = false,
                    Modified = DateTime.UtcNow,
                    ModifiedUtc = DateTime.UtcNow,
                    IsDirectory = true,
                    Size = 0
                };
            }

            var results = this.GetFolderContents(path).Result;

            var folder = results.FirstOrDefault(f => f.Path == path);

            return folder;
        }

        /// <summary>
        ///    Gets the blob items for a given path.
        /// </summary>
        /// <param name="path">Path to search.</param>
        /// <param name="extensions">Extensions to match.</param>
        /// <returns>FileManagerEntry list.</returns>
        public async Task<List<FileManagerEntry>> GetBlobItemsAsync(string path, string[] extensions)
        {
            if (!string.IsNullOrEmpty(path))
            {
                path = path.TrimStart('/');
            }

            var entries = new List<FileManagerEntry>();

            // Get the primary driver based on the configuration.
            var azureDriver = (AzureStorage)this.GetPrimaryDriver();

            // Get the blob items from Azure storage.
            var azureResults = await azureDriver.GetBlobItemsAsync(path);

            // Filter the results based on the provided extensions.
            foreach (var azureResult in azureResults)
            {
                var extension = Path.GetExtension(azureResult.Name).ToLower();
                if (extensions.Contains(extension))
                {
                    var fileName = Path.GetFileNameWithoutExtension(azureResult.Name);
                    var modified = azureResult.Properties.LastModified?.UtcDateTime ?? DateTime.UtcNow;
                    var entry = new FileManagerEntry
                    {
                        Created = DateTime.Now,
                        CreatedUtc = DateTime.UtcNow,
                        Extension = extension,
                        HasDirectories = false,
                        IsDirectory = false,
                        Modified = modified,
                        ModifiedUtc = modified,
                        Name = fileName,
                        Path = azureResult.Name,
                        Size = azureResult.Properties.ContentLength ?? 0
                    };

                    if (azureResult.Metadata.TryGetValue("ccmsimagex", out var ccmsImageX))
                    {
                        if (azureResult.Metadata.TryGetValue("ccmsimagey", out var ccmsImageY)
                            && azureResult.Metadata.TryGetValue("ccmsimagedpi", out var ccmsImageDpi))
                        {
                            entry.ImageX = int.Parse(ccmsImageX);
                            entry.ImageY = int.Parse(ccmsImageY);
                            entry.ImageDpi = int.Parse(ccmsImageDpi);
                        }
                    }

                    entries.Add(entry);
                }
            }

            return entries;
        }

        /// <summary>
        ///     Gets the contents for a folder.
        /// </summary>
        /// <param name="path">Path to folder.</param>
        /// <returns>Returns metadata as a <see cref="FileManagerEntry"/> <see cref="List{T}"/>.</returns>
        public async Task<List<FileManagerEntry>> GetFolderContents(string path)
        {
            if (!string.IsNullOrEmpty(path))
            {
                path = path.TrimStart('/');

                if (path == "/")
                {
                    path = string.Empty;
                }
                else
                {
                    if (!path.EndsWith("/"))
                    {
                        path = path + "/";
                    }
                }
            }

            return await this.GetObjectsAsync(path);
        }

        /// <summary>
        ///     Gets files and subfolders for a given path.
        /// </summary>
        /// <param name="path">Path to objects.</param>
        /// <returns>Returns metadata for the objects as a <see cref="FileManagerEntry"/> <see cref="List{T}"/>.</returns>
        public async Task<List<FileManagerEntry>> GetObjectsAsync(string path)
        {
            if (!string.IsNullOrEmpty(path))
            {
                path = path.TrimStart('/');
            }

            var entries = new List<FileManagerEntry>();

            // Gets the primary driver based on the configuration.
            var azureDriver = (AzureStorage)this.GetPrimaryDriver();

            var blobOjects = await azureDriver.GetObjectsAsync(path);

            foreach (var blob in blobOjects)
            {
                if (blob.IsBlob)
                {
                    if (blob.Blob.Name.EndsWith("folder.stubxx"))
                    {
                        continue;
                    }

                    var fileName = Path.GetFileNameWithoutExtension(blob.Blob.Name);

                    var modified = blob.Blob.Properties.LastModified?.UtcDateTime ?? DateTime.UtcNow;

                    entries.Add(new FileManagerEntry
                    {
                        Created = DateTime.Now,
                        CreatedUtc = DateTime.UtcNow,
                        Extension = Path.GetExtension(blob.Blob.Name),
                        HasDirectories = false,
                        IsDirectory = false,
                        Modified = modified,
                        ModifiedUtc = modified,
                        Name = fileName,
                        Path = blob.Blob.Name,
                        Size = blob.Blob.Properties.ContentLength ?? 0
                    });
                }
                else
                {
                    var parse = blob.Prefix.TrimEnd('/').Split('/');

                    var subDirectory = await azureDriver.GetObjectsAsync(blob.Prefix);

                    entries.Add(new FileManagerEntry
                    {
                        Created = DateTime.Now,
                        CreatedUtc = DateTime.UtcNow,
                        Extension = string.Empty,
                        HasDirectories = subDirectory.Any(a => a.IsPrefix),
                        IsDirectory = true,
                        Modified = DateTime.Now,
                        ModifiedUtc = DateTime.UtcNow,
                        Name = parse.Last(),
                        Path = blob.Prefix.TrimEnd('/'),
                        Size = 0
                    });
                }
            }

            return entries;
        }

        /// <summary>
        /// Asynchronously copies objects from a source path to a destination path, with an option to delete the source
        /// objects after copying.
        /// </summary>
        /// <remarks>This method ensures that the leading slashes are removed from both the source and
        /// destination paths before processing. It checks for the existence of destination objects before copying and
        /// throws an exception if any destination object already exists. If the copy operation is successful and
        /// <paramref name="deleteSource"/> is <see langword="true"/>, the source objects are deleted.</remarks>
        /// <param name="target">The source path from which objects are to be copied. Must not be null or empty, and cannot be the root
        /// folder.</param>
        /// <param name="destination">The destination path to which objects are to be copied. Must not be null or empty.</param>
        /// <param name="deleteSource">A boolean value indicating whether to delete the source objects after a successful copy. If <see
        /// langword="true"/>, the source objects will be deleted; otherwise, they will be retained.</param>
        /// <returns>Task.</returns>
        /// <exception cref="Exception">Thrown if the <paramref name="target"/> is null or empty, if the root folder is specified as the target, or
        /// if a destination object already exists.</exception>
        private async Task CopyObjectsAsync(string target, string destination, bool deleteSource)
        {
            // Make sure leading slashes are removed.
            target = target.TrimStart('/');
            destination = destination.TrimStart('/');

            if (string.IsNullOrEmpty(target))
            {
                throw new Exception("Cannot move the root folder.");
            }

            // Get the blob storage drivers.
            var drivers = this.GetDrivers();

            // Get a list of blobs to rename.
            var blobNames = await this.GetBlobNamesByPath(target);

            // Work through the list here.
            foreach (var srcBlobName in blobNames)
            {
                var tasks = new List<Task>();

                var destBlobName = srcBlobName.Replace(target, destination);

                // Check to see if the destination already exists
                foreach (var driver in drivers)
                {
                    if (await driver.BlobExistsAsync(destBlobName))
                    {
                        throw new Exception($"Could not copy {srcBlobName} as {destBlobName} already exists.");
                    }
                }

                // Copy the blob here
                foreach (var driver in drivers)
                {
                    await driver.CopyBlobAsync(srcBlobName, destBlobName);
                }

                // Now check to see if files were copied
                var success = true;

                foreach (var driver in drivers)
                {
                    success = await driver.BlobExistsAsync(destBlobName);
                }

                if (success)
                {
                    // Deleting the source is in the case of RENAME.
                    // Copying things does not delete the source
                    if (deleteSource)
                    {
                        // The copy was successful, so delete the old files
                        foreach (var cosmosStorage in drivers)
                        {
                            await cosmosStorage.DeleteIfExistsAsync(srcBlobName);
                        }
                    }
                }
                else
                {
                    // The copy was NOT successfull, delete any copied files and halt, throw an error.
                    foreach (var cosmosStorage in drivers)
                    {
                        await cosmosStorage.DeleteIfExistsAsync(destBlobName);
                    }

                    throw new Exception($"Could not copy: {srcBlobName} to {destBlobName}");
                }
            }
        }

        /// <summary>
        /// Converts a path to an array of folder names.
        /// </summary>
        /// <param name="path">Path to convert to array.</param>
        /// <returns>Array of folder names.</returns>
        private string[] ParseFirstFolder(string path)
        {
            var parts = path.Split('/');
            return parts;
        }

        /// <summary>
        ///    Gets the blob names by path.
        /// </summary>
        /// <param name="path">Path.</param>
        /// <param name="filter">Search filter.</param>
        /// <returns>List of blob names.</returns>
        private async Task<List<string>> GetBlobNamesByPath(string path, string[] filter = null)
        {
            var primaryDriver = this.GetPrimaryDriver();
            return await primaryDriver.GetBlobNamesByPath(path, filter);
        }

        /// <summary>
        /// Gets the primary driver based on the configuration.
        /// </summary>
        /// <returns>ICosmosStorage.</returns>
        private ICosmosStorage GetPrimaryDriver()
        {
            if (this.isMultiTenant == true)
            {
                var connectionString = this.dynamicConfigurationProvider.GetStorageConnectionString();
                return new AzureStorage(connectionString, this.credential);
            }

            if (this.primaryDriver == null)
            {
                primaryDriver = new AzureStorage(this.config.Value.StorageConfig.AzureConfigs.FirstOrDefault(), this.credential);
            }

            return primaryDriver;
        }

        /// <summary>
        /// Gets the drivers.
        /// </summary>
        /// <returns>ICosmosStorage.</returns>
        private List<ICosmosStorage> GetDrivers()
        {
            if (this.isMultiTenant == true)
            {
                drivers.Clear();
                var connectionString = this.dynamicConfigurationProvider.GetStorageConnectionString();
                drivers.Add(new AzureStorage(connectionString, this.credential));
            }
            else
            {
                if (!drivers.Any())
                {
                    foreach (var storageConfigAzureConfig in this.config.Value.StorageConfig.AzureConfigs)
                    {
                        drivers.Add(new AzureStorage(storageConfigAzureConfig, this.credential));
                    }
                }
            }

            return drivers;
        }
    }
}
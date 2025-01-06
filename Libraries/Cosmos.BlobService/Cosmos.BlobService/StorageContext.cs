// <copyright file="StorageContext.cs" company="Moonrise Software, LLC">
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
    using Microsoft.Extensions.Caching.Memory;
    using Microsoft.Extensions.Options;

    /// <summary>
    ///     Multi cloud blob service context.
    /// </summary>
    public sealed class StorageContext
    {
        /// <summary>
        /// Cosmos storage configuration.
        /// </summary>
        private readonly IOptions<CosmosStorageConfig> config;

        /// <summary>
        /// Default Azure token credential.
        /// </summary>
        private readonly DefaultAzureCredential credential;

        /// <summary>
        /// Used to brefly store chuk data while uploading.
        /// </summary>
        private readonly IMemoryCache memoryCache;

        /// <summary>
        /// Gets or sets azure container being connected to.
        /// </summary>
        private string containerName;

        /// <summary>
        /// Initializes a new instance of the <see cref="StorageContext"/> class.
        /// </summary>
        /// <param name="cosmosConfig">Storage context configuration as a <see cref="CosmosStorageConfig"/>.</param>
        /// <param name="cache"><see cref="IMemoryCache"/> used by context.</param>
        /// <param name="defaultAzureCredential">Default Azure Credential.</param>
        public StorageContext(IOptions<CosmosStorageConfig> cosmosConfig, IMemoryCache cache, DefaultAzureCredential defaultAzureCredential)
        {
            containerName = cosmosConfig.Value.StorageConfig.AzureConfigs.FirstOrDefault().AzureBlobStorageContainerName;
            config = cosmosConfig;
            memoryCache = cache;
            credential = defaultAzureCredential;
        }

        /// <summary>
        ///     Determine if this service is configured.
        /// </summary>
        /// <returns>Returns a <see cref="bool"/> indicating context is configured.</returns>
        public bool IsConfigured()
        {
            // Are there configuration settings at all?
            if (config.Value == null || config.Value.StorageConfig == null)
            {
                return false;
            }

            // If both AWS and Azure blob storage settings are provided,
            // make sure the distributed cache is provided and there is a primary provider.
            if (config.Value.StorageConfig.AmazonConfigs.Any() && config.Value.StorageConfig.AzureConfigs.Any())
            {
                return memoryCache != null && string.IsNullOrEmpty(config.Value.PrimaryCloud) == false;
            }

            // If just AWS is configured, make sure distributed cache exists, as this is needed for uploading files.
            if (config.Value.StorageConfig.AmazonConfigs.Any())
            {
                return memoryCache != null;
            }

            // Finally, make sure at the very least, Azure blob storage is
            return config.Value.StorageConfig.AzureConfigs.Any();
        }

        /// <summary>
        ///     Determines if a blob exists.
        /// </summary>
        /// <param name="path">Path check for a blob.</param>
        /// <returns><see cref="bool"/> indicating existence.</returns>
        public async Task<bool> BlobExistsAsync(string path)
        {
            var driver = GetPrimaryDriver();
            return await driver.BlobExistsAsync(path);
        }

        /// <summary>
        /// Changes the default container name.
        /// </summary>
        /// <param name="name">The name of the container.</param>
        public void SetContainerName(string name)
        {
            containerName = name;
        }

        /// <summary>
        ///     Copies a file or folder.
        /// </summary>
        /// <param name="target">Path to the file or folder to copy.</param>
        /// <param name="destination">Path to where to make the copy.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public async Task CopyAsync(string target, string destination)
        {
            await CopyObjectsAsync(target, destination, false);
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

            var blobs = await GetBlobNamesByPath(path);

            foreach (var blob in blobs)
            {
                DeleteFile(blob);
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

            var drivers = GetDrivers();
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
            var drivers = GetDrivers();

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
            var drivers = GetDrivers();

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
            var drivers = GetDrivers();

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
            var drivers = GetDrivers();
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

            var driver = (AzureStorage)GetPrimaryDriver();
            var blob = await driver.GetBlobAsync(path);
            var isDirectory = blob.Name.EndsWith("folder.stubxx");
            var hasDirectories = false;
            var fileName = Path.GetFileName(blob.Name);
            var blobName = blob.Name;

            if (isDirectory)
            {
                var children = await driver.GetObjectsAsync(path);
                hasDirectories = children.Any(c => c.IsPrefix);
                fileName = ParseFirstFolder(blob.Name)[0];
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
            var driver = (AzureStorage)GetPrimaryDriver();
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
            await CopyObjectsAsync(path, destination, true);
        }

        /// <summary>
        ///     Append bytes to blob(s).
        /// </summary>
        /// <param name="stream"><see cref="MemoryStream"/> containing data being appended.</param>
        /// <param name="fileMetaData"><see cref="FileUploadMetaData"/> containing metadata about the data 'chunk' and blob.</param>
        public void AppendBlob(MemoryStream stream, FileUploadMetaData fileMetaData)
        {
            var mark = DateTimeOffset.UtcNow;

            var drivers = GetDrivers();
            var data = stream.ToArray();
            var uploadTasks = new List<Task>();
            foreach (var cosmosStorage in drivers)
            {
                var cloneArray = data.ToArray();
                uploadTasks.Add(cosmosStorage.AppendBlobAsync(cloneArray, fileMetaData, mark));
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
            var drivers = GetDrivers();

            var tasks = new List<Task>();

            var primary = GetPrimaryDriver();
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

            var results = GetFolderContents(path).Result;

            var folder = results.FirstOrDefault(f => f.Path == path);

            return folder;
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

            var azureDriver = (AzureStorage)GetPrimaryDriver();

            var azureResults = await azureDriver.GetObjectsAsync(path);

            foreach (var azureResult in azureResults)
            {
                if (azureResult.IsBlob)
                {
                    if (azureResult.Blob.Name.EndsWith("folder.stubxx"))
                    {
                        continue;
                    }

                    var fileName = Path.GetFileNameWithoutExtension(azureResult.Blob.Name);

                    var modified = azureResult.Blob.Properties.LastModified?.UtcDateTime ?? DateTime.UtcNow;

                    entries.Add(new FileManagerEntry
                    {
                        Created = DateTime.Now,
                        CreatedUtc = DateTime.UtcNow,
                        Extension = Path.GetExtension(azureResult.Blob.Name),
                        HasDirectories = false,
                        IsDirectory = false,
                        Modified = modified,
                        ModifiedUtc = modified,
                        Name = fileName,
                        Path = azureResult.Blob.Name,
                        Size = azureResult.Blob.Properties.ContentLength ?? 0
                    });
                }
                else
                {
                    var parse = azureResult.Prefix.TrimEnd('/').Split('/');

                    var subDirectory = await azureDriver.GetObjectsAsync(azureResult.Prefix);

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
                        Path = azureResult.Prefix.TrimEnd('/'),
                        Size = 0
                    });
                }
            }

            return entries;
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
            var azureDriver = (AzureStorage)GetPrimaryDriver();
            var azureResults = await azureDriver.GetBlobItemsAsync(path);
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

            return await GetObjectsAsync(path);
        }

        private async Task CopyObjectsAsync(string target, string destination, bool deleteSource)
        {
            // Make sure leading slashes are removed.
            target = target.TrimStart('/');
            destination = destination.TrimStart('/');

            if (string.IsNullOrEmpty(target))
            {
                throw new Exception("Cannot move the root folder.");
            }

            var drivers = GetDrivers();

            // Get a list of blobs to rename.
            var blobNames = await GetBlobNamesByPath(target);

            // Work through the list here.
            foreach (var srcBlobName in blobNames)
            {
                var tasks = new List<Task>();

                var destBlobName = srcBlobName.Replace(target, destination);

                // Check to see if the destination already exists
                foreach (var cosmosStorage in drivers)
                {
                    if (await cosmosStorage.BlobExistsAsync(destBlobName))
                    {
                        throw new Exception($"Could not copy {srcBlobName} as {destBlobName} already exists.");
                    }
                }

                // Copy the blob here
                foreach (var cosmosStorage in drivers)
                {
                    await cosmosStorage.CopyBlobAsync(srcBlobName, destBlobName);
                }

                // Now check to see if files were copied
                var success = true;

                foreach (var cosmosStorage in drivers)
                {
                    success = await cosmosStorage.BlobExistsAsync(destBlobName);
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

        private ICosmosStorage GetPrimaryDriver()
        {
            return new AzureStorage(config.Value.StorageConfig.AzureConfigs.FirstOrDefault(), credential, containerName);
        }

        private string[] ParseFirstFolder(string path)
        {
            var parts = path.Split('/');
            return parts;
        }

        private async Task<List<string>> GetBlobNamesByPath(string path, string[] filter = null)
        {
            var primaryDriver = GetPrimaryDriver();
            return await primaryDriver.GetBlobNamesByPath(path, filter);
        }

        private List<ICosmosStorage> GetDrivers()
        {
            var drivers = new List<ICosmosStorage>();

            foreach (var storageConfigAzureConfig in config.Value.StorageConfig.AzureConfigs)
            {
                drivers.Add(new AzureStorage(storageConfigAzureConfig, credential, containerName));
            }

            return drivers;
        }
    }
}
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
    using Cosmos.BlobService.Config;
    using Cosmos.BlobService.Drivers;
    using Cosmos.BlobService.Models;
    using Cosmos.DynamicConfig;
    using Microsoft.Extensions.Caching.Memory;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;

    /// <summary>
    ///     Multi cloud blob service context.
    /// </summary>
    public sealed class StorageContext
    {
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

        private ICosmosStorage primaryDriver;

        /// <summary>
        /// Initializes a new instance of the <see cref="StorageContext"/> class for multitenant instances.
        /// </summary>
        /// <param name="configuration">App configuration.</param>
        /// <param name="cache"><see cref="IMemoryCache"/>Memory cache.</param>
        /// <param name="serviceProvider">Services provider.</param>
        public StorageContext(
            IConfiguration configuration,
            IMemoryCache cache,
            IServiceProvider serviceProvider)
        {
            memoryCache = cache;
            isMultiTenant = configuration.GetValue<bool?>("MultiTenantEditor") ?? false;
            if (isMultiTenant)
            {
                dynamicConfigurationProvider = serviceProvider.GetRequiredService<IDynamicConfigurationProvider>();
            }
            else
            {
                var connectionString = configuration.GetConnectionString("StorageConnectionString");
                primaryDriver = GetDriverFromConnectionString(connectionString);
            }
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
            var driver = GetPrimaryDriver();
            await driver.DeleteFolderAsync(path);
        }

        /// <summary>
        ///     Deletes a file.
        /// </summary>
        /// <param name="path">Path to file.</param>
        public void DeleteFile(string path)
        {
            // Ensure leading slash is removed.
            path = path.TrimStart('/');

            this.GetPrimaryDriver().DeleteIfExistsAsync(path).Wait();
        }

        /// <summary>
        /// Enables the Azure BLOB storage static website.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public async Task EnableAzureStaticWebsite()
        {
            var driver = this.GetPrimaryDriver();
            if (driver.GetType() == typeof(AzureStorage))
            {
                var azureStorage = (AzureStorage)driver;
                await azureStorage.EnableStaticWebsite();
            }
        }

        /// <summary>
        /// Disables the static website (when login is required for example).
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public async Task DisableAzureStaticWebsite()
        {
            var driver = this.GetPrimaryDriver();
            if (driver.GetType() == typeof(AzureStorage))
            {
                var azureStorage = (AzureStorage)driver;
                await azureStorage.DisableStaticWebsite();
            }
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

            var driver = this.GetPrimaryDriver();
            var metadata = await driver.GetFileMetadataAsync(path);

            var isDirectory = metadata.FileName.EndsWith("folder.stubxx");
            var fileName = Path.GetFileName(metadata.FileName);
            var blobName = metadata.FileName;
            var hasDirectories = false;

            if (isDirectory)
            {
                var children = await driver.GetBlobNamesByPath(path);
                hasDirectories = children.Any(c => c.EndsWith("folder.stubxx"));
            }

            var fileManagerEntry = new FileManagerEntry
            {
                Created = metadata.Created.UtcDateTime,
                CreatedUtc = metadata.Created.UtcDateTime,
                Extension = isDirectory ? string.Empty : Path.GetExtension(metadata.FileName),
                HasDirectories = hasDirectories,
                IsDirectory = isDirectory,
                Modified = metadata.LastModified.DateTime,
                ModifiedUtc = metadata.LastModified.UtcDateTime,
                Name = fileName,
                Path = blobName,
                Size = metadata.ContentLength
            };

            return fileManagerEntry;
        }

        /// <summary>
        ///     Returns a response stream from the primary blob storage provider.
        /// </summary>
        /// <param name="path">Path to blob to open read stream from.</param>
        /// <returns>Data as a <see cref="Stream"/>.</returns>
        public async Task<Stream> GetStreamAsync(string path)
        {
            // Ensure leading slash is removed.
            path = path.TrimStart('/');

            // Get the primary driver based on the configuration.
            var driver = GetPrimaryDriver();
            return await driver.GetStreamAsync(path);
        }

        /// <summary>
        ///     Renames a file or folder.
        /// </summary>
        /// <param name="path">Path to file or folder.</param>
        /// <param name="destination">The new name or path.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public async Task RenameAsync(string path, string destination)
        {
            var driver = GetPrimaryDriver();
            await driver.RenameAsync(path, destination);
        }

        /// <summary>
        ///     Append bytes to blob(s).
        /// </summary>
        /// <param name="stream"><see cref="MemoryStream"/> containing data being appended.</param>
        /// <param name="fileMetaData"><see cref="FileUploadMetaData"/> containing metadata about the data 'chunk' and blob.</param>
        /// <param name="mode">Is either append or block.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public async Task AppendBlob(MemoryStream stream, FileUploadMetaData fileMetaData, string mode = "append")
        {
            var mark = DateTimeOffset.UtcNow;

            // Gets the primary driver based on the configuration.
            var driver = this.GetPrimaryDriver();

            await driver.AppendBlobAsync(stream.ToArray(), fileMetaData, mark, mode);
        }

        /// <summary>
        ///     Creates a folder in all the cloud storage accounts.
        /// </summary>
        /// <param name="path">Path to the folder to create.</param>
        /// <returns>Folder metadata as a <see cref="FileManagerEntry"/>.</returns>
        /// <remarks>Creates the folder if it does not already exist.</remarks>
        public async Task<FileManagerEntry> CreateFolder(string path)
        {
            var driver = this.GetPrimaryDriver();
            if (!driver.BlobExistsAsync(path + "/folder.stubxx").Result)
            {
                await driver.CreateFolderAsync(path);
            }

            // Gets the primary driver based on the configuration.
            var primary = this.GetPrimaryDriver();
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

        /// <summary>
        ///     Gets files and subfolders for a given path.
        /// </summary>
        /// <param name="path">Path to objects.</param>
        /// <returns>Returns metadata for the objects as a <see cref="FileManagerEntry"/> <see cref="List{T}"/>.</returns>
        public async Task<List<FileManagerEntry>> GetFilesAndDirectories(string path)
        {
            var driver = this.GetPrimaryDriver();

            path = path.TrimStart('/');

            var entries = await driver.GetFilesAndDirectories(path);

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
            var driver = this.GetPrimaryDriver();
            await driver.RenameAsync(target, destination);

            // Work through the list here.
            //foreach (var srcBlobName in blobNames)
            //{
            //    var tasks = new List<Task>();

            //    var destBlobName = srcBlobName.Replace(target, destination);

            //    if (await driver.BlobExistsAsync(destBlobName))
            //    {
            //        throw new Exception($"Could not copy {srcBlobName} as {destBlobName} already exists.");
            //    }

            //    await driver.CopyBlobAsync(srcBlobName, destBlobName);

            //    // Now check to see if files were copied
            //    var success = await driver.BlobExistsAsync(destBlobName);

            //    if (success)
            //    {
            //        // Deleting the source is in the case of RENAME.
            //        // Copying things does not delete the source
            //        if (deleteSource)
            //        {
            //            await driver.DeleteIfExistsAsync(srcBlobName);
            //        }
            //    }
            //    else
            //    {
            //        // The copy was NOT successfull, delete any copied files and halt, throw an error.
            //        await driver.DeleteIfExistsAsync(destBlobName);
            //        throw new Exception($"Could not copy: {srcBlobName} to {destBlobName}");
            //    }
            //}
        }

        /// <summary>
        ///    Gets the blob names by path.
        /// </summary>
        /// <param name="path">Path.</param>
        /// <returns>List of blob names.</returns>
        private async Task<List<string>> GetBlobNamesByPath(string path)
        {
            var primaryDriver = this.GetPrimaryDriver();
            return await primaryDriver.GetBlobNamesByPath(path);
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
                return GetDriverFromConnectionString(connectionString);
            }

            return primaryDriver;
        }

        /// <summary>
        /// Gets the driver from a connection string.
        /// </summary>
        /// <param name="connectionString">Connection string.</param>
        /// <returns>ICosmosStorage driver.</returns>
        private ICosmosStorage GetDriverFromConnectionString(string connectionString)
        {
            if (connectionString.StartsWith("DefaultEndpointsProtocol=", StringComparison.CurrentCultureIgnoreCase))
            {
                return new AzureStorage(connectionString, new DefaultAzureCredential());
            }
            else if (connectionString.Contains("bucket", StringComparison.CurrentCultureIgnoreCase))
            {
                // Example: Bucket=cosmoscms-001;Region=us-east-2;KeyId=AKIA;Key=MySecretKey;
                var parts = connectionString.Split(';', StringSplitOptions.RemoveEmptyEntries);
                var bucket = parts.FirstOrDefault(p => p.StartsWith("Bucket=", StringComparison.CurrentCultureIgnoreCase)).Split("=")[1];
                var region = parts.FirstOrDefault(p => p.StartsWith("Region=", StringComparison.CurrentCultureIgnoreCase)).Split("=")[1];
                var keyId = parts.FirstOrDefault(p => p.StartsWith("KeyId=", StringComparison.CurrentCultureIgnoreCase)).Split("=")[1];
                var key = parts.FirstOrDefault(p => p.StartsWith("Key=", StringComparison.CurrentCultureIgnoreCase)).Split("=")[1];

                return new AmazonStorage(
                    new AmazonStorageConfig()
                    {
                        AmazonRegion = region,
                        BucketName = bucket,
                        KeyId = keyId,
                        Key = key
                    },
                    memoryCache);
            }
            else
            {
                throw new Exception("No valid storage connection string found. Please check your configuration.");
            }
        }
    }
}
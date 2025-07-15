// <copyright file="StartupTaskService.cs" company="Moonrise Software, LLC">
// Copyright (c) Moonrise Software, LLC. All rights reserved.
// Licensed under the GNU Public License, Version 3.0 (https://www.gnu.org/licenses/gpl-3.0.html)
// See https://github.com/MoonriseSoftwareCalifornia/CosmosCMS
// for more information concerning the license and the contributors participating to this project.
// </copyright>

namespace Cosmos.Editor.Services
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Threading.Tasks;
    using Azure.Storage.Blobs;
    using Azure.Storage.Blobs.Models;
    using Azure.Storage.Blobs.Specialized;
    using Microsoft.AspNetCore.Hosting;

    /// <summary>
    /// Service implementation for running startup tasks asynchronously.
    /// </summary>
    public class StartupTaskService : IStartupTaskService
    {
        private readonly IWebHostEnvironment webHost;
        private readonly MultiDatabaseManagementUtilities managementUtilities;

        /// <summary>
        /// Initializes a new instance of the <see cref="StartupTaskService"/> class.
        /// </summary>
        /// <param name="webHost">Web host environment.</param>
        /// <param name="managementUtilities">Database management utilities.</param>
        public StartupTaskService(IWebHostEnvironment webHost, MultiDatabaseManagementUtilities managementUtilities)
        {
            this.webHost = webHost;
            this.managementUtilities = managementUtilities;
        }

        /// <summary>
        /// Runs the startup task asynchronously.
        /// </summary>
        /// <returns>Task.</returns>
        public async Task RunAsync()
        {
            // Get the list of files to upload.
            var fileList = GetFileList();

            // Loop through the file list and upload each file to the appropriate storage connection.
            foreach (var file in fileList)
            {
                using var fileStream = File.OpenRead(file.SourcePath);
                using var memoryStream = new MemoryStream();
                await fileStream.CopyToAsync(memoryStream);

                // Get the connections from the management utilities.
                var connections = await managementUtilities.GetConnections();

                // Loop through each connection and upload the file to each storage connection.
                foreach (var connection in connections)
                {
                    // Upload the file to the storage connection if it is not already in place.
                    await UploadFile(connection.StorageConn, file.BlobName, memoryStream);
                }
            }

            await managementUtilities.EnsureDatabasesAreConfigured();
        }

        /// <summary>
        /// Gets the list of files to upload to Azure Blob Storage.
        /// </summary>
        /// <returns>List of file specifications.</returns>
        private List<FileSpecification> GetFileList()
        {
            var list = new List<FileSpecification>
            {
                new FileSpecification(
                "/lib/ckeditor/ckeditor5-content.css",
                "text/css",
                webHost.WebRootPath + "/lib/ckeditor/ckeditor5-content.css",
                "text/css")
            };

            return list;
        }

        /// <summary>
        /// Uploads a file to the Azure Blob Storage.
        /// </summary>
        /// <param name="storageConnection">Storage connection.</param>
        /// <param name="blobName">Full blob name (including path).</param>
        /// <param name="memoryStream">Memory stream.</param>
        /// <returns>A Task.</returns>
        private async Task UploadFile(string storageConnection, string blobName, MemoryStream memoryStream)
        {
            memoryStream.Position = 0; // Reset the memory stream position to the beginning.
            var blobServiceClient = new BlobServiceClient(storageConnection);
            var blob = blobServiceClient.GetBlobContainerClient("$web").GetBlockBlobClient(blobName);

            // Check if the blob already exists before uploading.
            if (!await blob.ExistsAsync())
            {
                // Set the Content Type.
                var headers = new BlobHttpHeaders
                {
                    // Set the MIME ContentType every time the properties are updated or the field will be cleared.
                    ContentType = "text/css",
                };

                // Upload the file to the blob storage.
                await blob.UploadAsync(memoryStream, headers);

                // Create metadata for the blob.
                var dictionaryObject = new Dictionary<string, string>
                {
                    { "ccmsuploaduid", Guid.NewGuid().ToString() },
                    { "ccmssize", memoryStream.Length.ToString() },
                    { "ccmsdatetime", DateTimeOffset.UtcNow.Ticks.ToString() },
                    { "ccmsimagewidth", string.Empty },
                    { "ccmsimageheight", string.Empty }
                };

                // Set the metadata for the blob.
                _ = await blob.SetMetadataAsync(dictionaryObject);
            }
        }

        /// <summary>
        /// Represents a file specification for uploading to Azure Blob Storage.
        /// </summary>
        private class FileSpecification
        {
            internal FileSpecification(string blobName, string contentType, string sourcePath, string mimeType)
            {
                BlobName = blobName;
                ContentType = contentType;
                SourcePath = sourcePath;
                MimeType = mimeType;
            }

            /// <summary>
            /// Gets or sets the blob name, which includes the path within the container.
            /// </summary>
            internal string BlobName { get; set; }

            /// <summary>
            /// Gets or sets the content type of the file, used for setting the MIME type in Azure Blob Storage.
            /// </summary>
            internal string ContentType { get; set; }

            /// <summary>
            /// Gets or sets the source path of the file on the local filesystem.
            /// </summary>
            internal string SourcePath { get; set; }

            /// <summary>
            /// Gets or sets the MIME type of the file, used for setting the ContentType in Azure Blob Storage.
            /// </summary>
            internal string MimeType { get; set; }
        }
    }
}

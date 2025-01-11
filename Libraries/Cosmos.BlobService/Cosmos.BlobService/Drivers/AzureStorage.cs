// <copyright file="AzureStorage.cs" company="Moonrise Software, LLC">
// Copyright (c) Moonrise Software, LLC. All rights reserved.
// Licensed under the GNU Public License, Version 3.0 (https://www.gnu.org/licenses/gpl-3.0.html)
// See https://github.com/MoonriseSoftwareCalifornia/CosmosCMS
// for more information concerning the license and the contributors participating to this project.
// </copyright>

namespace Cosmos.BlobService.Drivers
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Runtime.CompilerServices;
    using System.Text;
    using System.Threading.Tasks;
    using Azure;
    using Azure.Identity;
    using Azure.Storage.Blobs;
    using Azure.Storage.Blobs.Models;
    using Azure.Storage.Blobs.Specialized;
    using Cosmos.BlobService.Config;
    using Cosmos.BlobService.Models;
    using static System.Net.Mime.MediaTypeNames;

    /// <summary>
    ///     Azure blob storage driver.
    /// </summary>
    public sealed class AzureStorage : ICosmosStorage
    {
        private readonly string containerName;
        private readonly BlobServiceClient blobServiceClient;
        private readonly bool usesAzureDefaultCredential;

        /// <summary>
        /// Initializes a new instance of the <see cref="AzureStorage"/> class.
        /// </summary>
        /// <param name="config">Storage configuration as a <see cref="AzureStorageConfig"/>.</param>
        /// <param name="containerName">Name of container (default is $web).</param>
        /// <param name="defaultAzureCredential">Default azure credential.</param>
        public AzureStorage(AzureStorageConfig config, DefaultAzureCredential defaultAzureCredential, string containerName = "$web")
        {
            this.containerName = containerName;
            var conparts = config.AzureBlobStorageConnectionString.Split(';');
            var conpartsDict = conparts.Where(w => !string.IsNullOrEmpty(w)).Select(part => part.Split('=')).ToDictionary(sp => sp[0], sp => sp[1], StringComparer.OrdinalIgnoreCase);

            if (!conpartsDict.ContainsKey("AccountKey"))
            {
                throw new ArgumentException("Azure Blob Storage connection string is missing AccountKey.");
            }

            if (conpartsDict["AccountKey"] == "AccessToken")
            {
                usesAzureDefaultCredential = true;
                var accountName = conpartsDict["AccountName"];
                blobServiceClient = new BlobServiceClient(new Uri($"https://{accountName}.blob.core.windows.net/"), defaultAzureCredential);
            }
            else
            {
                usesAzureDefaultCredential = false;
                blobServiceClient = new BlobServiceClient(config.AzureBlobStorageConnectionString);
            }
        }

        /// <summary>
        /// Gets the amount of bytes consumed in a storage account container.
        /// </summary>
        /// <returns>Returns the number of bytes consumed for the container as a <see cref="long"/>.</returns>
        public async Task<long> GetBytesConsumed()
        {
            var container = blobServiceClient.GetBlobContainerClient(containerName);
            long bytesConsumed = 0;
            var blobs = container.GetBlobsAsync().AsPages();

            await foreach (var blobPage in blobs)
            {
                bytesConsumed += blobPage.Values.Sum(b => b.Properties.ContentLength.GetValueOrDefault());
            }

            return bytesConsumed;
        }

        /// <summary>
        ///     Appends byte array to an existing blob.
        /// </summary>
        /// <param name="data">Bytes to append.</param>
        /// <param name="fileMetaData">'Chunk' metadata being appended as a <see cref="FileUploadMetaData"/>.</param>
        /// <param name="uploadDateTime">Date and time uploaded as a <see cref="DateTimeOffset"/>.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        /// <remarks>
        /// Existing blobs will be overwritten if they already exists, otherwise a new blob is created.
        /// </remarks>
        public async Task AppendBlobAsync(byte[] data, FileUploadMetaData fileMetaData, DateTimeOffset uploadDateTime)
        {
            var appendClient = GetAppendBlobClient(fileMetaData.RelativePath);

            if (fileMetaData.ChunkIndex == 0)
            {
                await appendClient.DeleteIfExistsAsync(DeleteSnapshotsOption.IncludeSnapshots);

                var headers = new BlobHttpHeaders
                {
                    // Set the MIME ContentType every time the properties are updated or the field will be cleared.
                    ContentType = Utilities.GetContentType(fileMetaData)
                };

                await appendClient.CreateIfNotExistsAsync(headers);

                var dictionaryObject = new Dictionary<string, string>
                {
                    { "ccmsuploaduid", fileMetaData.UploadUid },
                    { "ccmssize", fileMetaData.TotalFileSize.ToString() },
                    { "ccmsdatetime", uploadDateTime.UtcDateTime.Ticks.ToString() },
                    { "ccmsimagewidth", fileMetaData.ImageWidth },
                    { "ccmsimageheight", fileMetaData.ImageHeight }
                };

                _ = await appendClient.SetMetadataAsync(dictionaryObject);
            }

            // AWS Multi part upload requires parts or chunks to be 5MB, which
            // are too big for Azure append blobs, so buffer the upload size here.
            await using var loadMemoryStream = new MemoryStream(data);

            loadMemoryStream.Position = 0;
            int bytesRead;
            var buffer = new byte[2621440]; // 2.5 MB.
            while ((bytesRead = await loadMemoryStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
            {
                var newArray = new Span<byte>(buffer, 0, bytesRead).ToArray();
                Stream stream = new MemoryStream(newArray)
                {
                    Position = 0
                };
                await appendClient.AppendBlockAsync(stream);
            }

            if (fileMetaData.TotalChunks - 1 == fileMetaData.ChunkIndex)
            {
                // This is the last chunk, wrap things up here.
                await appendClient.SealAsync();
            }
        }

        /// <summary>
        ///     Determines if a blob exists.
        /// </summary>
        /// <param name="path">Path to blob.</param>
        /// <returns>Returns a <see cref="bool"/> indicating it exists or not.</returns>
        public async Task<bool> BlobExistsAsync(string path)
        {
            var blob = await GetBlobAsync(path);

            return await blob.ExistsAsync();
        }

        /// <summary>
        ///     Copies a single blob and returns it's <see cref="BlobClient" />.
        /// </summary>
        /// <param name="source">Path to the blob to copy.</param>
        /// <param name="destination">Path to where the blob should be copied.</param>
        /// <returns>The destination or new <see cref="BlobClient" />.</returns>
        /// <remarks>
        ///     Tip: After operation, check the returned blob object to see if it exists.
        /// </remarks>
        public async Task CopyBlobAsync(string source, string destination)
        {
            source = source.TrimStart('/');
            destination = destination.TrimStart('/');

            var containerClient =
                blobServiceClient.GetBlobContainerClient(containerName);
            var sourceBlob = containerClient.GetBlobClient(source);
            if (await sourceBlob.ExistsAsync())
            {
                var lease = sourceBlob.GetBlobLeaseClient();
                await lease.AcquireAsync(TimeSpan.FromSeconds(-1));

                // Get a BlobClient representing the destination blob with a unique name.
                var destBlob = containerClient.GetBlobClient(destination);

                // Start the copy operation.
                var c = await destBlob.StartCopyFromUriAsync(sourceBlob.Uri);
                await c.WaitForCompletionAsync();

                await lease.ReleaseAsync();
            }
        }

        /// <summary>
        ///     Creates a folder if it does not yet exists.
        /// </summary>
        /// <param name="path">Path to folder to create.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        /// <exception cref="Exception">Folder creation failure.</exception>
        public async Task CreateFolderAsync(string path)
        {
            // Blob storage does not have a folder object, just blobs with paths.
            // Therefore, to create an illusion of a folder, we have to create a blob
            // that will be in the folder.  For example:
            // 
            // To create folder /pictures
            //
            // You have to pub a blob here /pictures/folder.subxx
            //
            // To remove a folder, you simply remove all blobs below /pictures
            //
            // Make sure the path doesn't already exist.
            // Don't lead with "/" with S3
            var fullPath = Utilities.GetBlobName(path, "folder.stubxx").TrimStart('/');

            var blobClient = await GetBlobAsync(fullPath);

            if (!await blobClient.ExistsAsync())
            {
                var byteArray = Encoding.ASCII.GetBytes($"This is a folder stub file for {path}.");
                await using var stream = new MemoryStream(byteArray);
                await blobClient.UploadAsync(stream);
            }
        }

        /// <summary>
        ///     Gets a list of blobs by path.
        /// </summary>
        /// <param name="path">Path to get blob names from.</param>
        /// <param name="filter">Search filter (optional).</param>
        /// <returns>Returns the names as a <see cref="string"/> list.</returns>
        public async Task<List<string>> GetBlobNamesByPath(string path, string[] filter = null)
        {
            var containerClient =
                blobServiceClient.GetBlobContainerClient(containerName);

            var pageable = containerClient.GetBlobsAsync(prefix: path).AsPages();

            var results = new List<BlobItem>();
            await foreach (var page in pageable)
            {
                results.AddRange(page.Values);
            }

            return results.Select(s => s.Name).ToList();
        }

        /// <summary>
        ///     Deletes a folder and all its contents.
        /// </summary>
        /// <param name="path">Path to doomed folder.</param>
        /// <returns>Returns the number of deleted obhects as an <see cref="int"/>.</returns>
        public async Task<int> DeleteFolderAsync(string path)
        {
            var blobs = await GetBlobItemsByPath(path);
            var containerClient =
                blobServiceClient.GetBlobContainerClient(containerName);

            var responses = new List<Response<bool>>();

            foreach (var blob in blobs)
            {
                responses.Add(await containerClient.DeleteBlobIfExistsAsync(blob.Name, DeleteSnapshotsOption.IncludeSnapshots));
            }

            return responses.Count(r => r.Value);
        }

        /// <summary>
        ///     Deletes a file (blob).
        /// </summary>
        /// <param name="path">Path to doomed blob.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public async Task DeleteIfExistsAsync(string path)
        {
            var containerClient = blobServiceClient.GetBlobContainerClient(containerName);
            await containerClient.DeleteBlobIfExistsAsync(path, DeleteSnapshotsOption.IncludeSnapshots);
        }

        /// <summary>
        /// Enables the static website and sets default CORS rule.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public async Task EnableStaticWebsite()
        {
            if (usesAzureDefaultCredential)
            {
                return; // Not able to enable static website when using Azure Default Credential.
            }

            BlobServiceProperties properties = await blobServiceClient.GetPropertiesAsync();

            if (!properties.StaticWebsite.Enabled)
            {
                properties.StaticWebsite.Enabled = true;
                await blobServiceClient.SetPropertiesAsync(properties);
            }

            if (properties.Cors == null || properties.Cors.Count == 0)
            {
                var corsRule = new BlobCorsRule
                {
                    AllowedMethods = "GET,HEAD,OPTIONS",
                    AllowedOrigins = "*",
                    AllowedHeaders = "*",
                    ExposedHeaders = "*"
                };
                properties.Cors = new List<BlobCorsRule>() { corsRule };
                await blobServiceClient.SetPropertiesAsync(properties);
            }
        }

        /// <summary>
        /// Disables the static website.
        /// </summary>
        /// <returns>A <see cref="Task"/>.</returns>
        public async Task DisableStaticWebsite()
        {
            BlobServiceProperties properties = await blobServiceClient.GetPropertiesAsync();

            if (properties.StaticWebsite.Enabled)
            {
                properties.StaticWebsite.Enabled = false;
                await blobServiceClient.SetPropertiesAsync(properties);
            }
        }

        /// <summary>
        ///     Gets a <see cref="BlobClient"/> for a blob.
        /// </summary>
        /// <param name="path">Path to blob.</param>
        /// <returns>Returns a <see cref="BlobClient"/>.</returns>
        public async Task<BlobClient> GetBlobAsync(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return null;
            }

            path = path.TrimStart('/');
            var containerClient =
                blobServiceClient.GetBlobContainerClient(containerName);

            if (await containerClient.ExistsAsync())
            {
                return containerClient.GetBlobClient(path);
            }

            return null;
        }

        /// <summary>
        /// Get blob itmes by path.
        /// </summary>
        /// <param name="path">Path where to get items from.</param>
        /// <returns>Returns the items as <see cref="BlobItem"/> <see cref="List{T}"/>.</returns>
        public async Task<List<BlobItem>> GetBlobItemsByPath(string path)
        {
            var results = new List<BlobItem>();
            var containerClient =
                blobServiceClient.GetBlobContainerClient(containerName);
            var items = containerClient.GetBlobsAsync(prefix: path);

            await foreach (var item in items)
            {
                results.Add(item);
            }

            return results;
        }

        /// <summary>
        /// Gets metadata for a blob.
        /// </summary>
        /// <param name="path">Path to file from which to get metadata.</param>
        /// <returns>Returns metadata as a <see cref="FileMetadata"/>.</returns>
        public async Task<FileMetadata> GetFileMetadataAsync(string path)
        {
            var blobClient = await GetBlobAsync(path);
            var properties = await blobClient.GetPropertiesAsync();
            _ = long.TryParse(properties.Value.Metadata["ccmsuploaduid"], out var mark);
            return new FileMetadata()
            {
                ContentLength = properties.Value.ContentLength,
                ContentType = properties.Value.ContentType,
                ETag = properties.Value.ETag.ToString(),
                FileName = blobClient.Name,
                LastModified = properties.Value.LastModified.UtcDateTime,
                UploadDateTime = mark
            };
        }

        /// <inheritdoc/>
        public Task<List<FileMetadata>> GetInventory()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        ///     Gets files and subfolders for a given path.
        /// </summary>
        /// <param name="path">Path from which to get objects from.</param>
        /// <returns>Returns objects as a <see cref="BlobHierarchyItem"/> <see cref="List{T}"/>.</returns>
        public async Task<List<BlobHierarchyItem>> GetObjectsAsync(string path)
        {
            if (path == "/")
            {
                path = string.Empty;
            }

            if (!string.IsNullOrEmpty(path))
            {
                path = path.TrimStart('/');
            }

            var containerClient = blobServiceClient.GetBlobContainerClient(containerName);

            var resultSegment = containerClient.GetBlobsByHierarchyAsync(prefix: path, delimiter: "/").AsPages();
            var results = new List<BlobHierarchyItem>();

            await foreach (var blobPage in resultSegment)
            {
                results.AddRange(blobPage.Values);
            }

            return results;
        }

        /// <summary>
        ///    Gets a list of blobs for a given path.
        /// </summary>
        /// <param name="path">Path to search.</param>
        /// <returns>BlobItem list.</returns>
        public async Task<List<BlobItem>> GetBlobItemsAsync(string path)
        {
            var containerClient = blobServiceClient.GetBlobContainerClient(containerName);
            var resultSegment = containerClient.GetBlobsAsync(prefix: path).AsPages();
            var results = new List<BlobItem>();

            await foreach (var blobPage in resultSegment)
            {
                results.AddRange(blobPage.Values);
            }

            return results;
        }

        /// <summary>
        /// Opens a read stream to download the blob.
        /// </summary>
        /// <param name="path">Path to blob from which to open.</param>
        /// <returns>Returns data as a <see cref="Stream"/>.</returns>
        public async Task<Stream> GetStreamAsync(string path)
        {
            var containerClient =
                blobServiceClient.GetBlobContainerClient(containerName);

            var blobClient = containerClient.GetAppendBlobClient(path);

            return await blobClient.OpenReadAsync(new BlobOpenReadOptions(false));
        }

        /// <summary>
        /// Uploads file to a stream.
        /// </summary>
        /// <param name="readStream">Data stream to upload.</param>
        /// <param name="fileMetaData">File and chunk metadata.</param>
        /// <param name="uploadDateTime">Chunk upload date and time.</param>
        /// <returns>Indicates success as a <see cref="bool"/>.</returns>
        public async Task<bool> UploadStreamAsync(Stream readStream, FileUploadMetaData fileMetaData, DateTimeOffset uploadDateTime)
        {
            var appendClient = GetAppendBlobClient(fileMetaData.RelativePath);

            var headers = new BlobHttpHeaders
            {
                // Set the MIME ContentType every time the properties
                // are updated or the field will be cleared.,
                ContentType = Utilities.GetContentType(fileMetaData)
            };
            await appendClient.CreateIfNotExistsAsync(headers);

            var dictionaryObject = new Dictionary<string, string>
            {
                { "ccmsuploaduid", fileMetaData.UploadUid },
                { "ccmssize", fileMetaData.TotalFileSize.ToString() },
                { "ccmsdatetime", uploadDateTime.UtcDateTime.Ticks.ToString() }
            };

            _ = await appendClient.SetMetadataAsync(dictionaryObject);
            var writeStream = await appendClient.OpenWriteAsync(true);
            await readStream.CopyToAsync(writeStream);
            return true;
        }

        /// <summary>
        ///     Gets an append blob client, used for chunk uploads.
        /// </summary>
        /// <param name="path">Path to blob from which to open with client.</param>
        /// <returns>Returns the <see cref="AppendBlobClient"/>.</returns>
        public AppendBlobClient GetAppendBlobClient(string path)
        {
            var containerClient =
                blobServiceClient.GetBlobContainerClient(containerName);

            return containerClient.GetAppendBlobClient(path);
        }
    }
}
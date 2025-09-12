﻿// <copyright file="AmazonStorage.cs" company="Moonrise Software, LLC">
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
    using System.Net;
    using System.Text;
    using System.Threading.Tasks;
    using Amazon;
    using Amazon.S3;
    using Amazon.S3.Model;
    using Azure.Storage.Blobs;
    using Azure.Storage.Blobs.Models;
    using Cosmos.BlobService.Config;
    using Cosmos.BlobService.Models;
    using Microsoft.Extensions.Caching.Distributed;
    using Microsoft.Extensions.Caching.Memory;
    using Newtonsoft.Json;

    /// <summary>
    ///     AWS S3 storage driver
    /// </summary>
    public sealed class AmazonStorage : ICosmosStorage
    {
        private readonly AmazonStorageConfig config;
        private readonly IMemoryCache cache;

        /// <summary>
        /// Initializes a new instance of the <see cref="AmazonStorage"/> class.
        /// </summary>
        /// <param name="config">Storage configuration.</param>
        /// <param name="cache">Memory cache.</param>
        /// <remarks>
        ///     <para>
        ///         At present, AWS S3 doesn't have the ability to track byte chunks uploaded to a blob like Azure.
        ///         When all the chunks are uploaded, the developer needs to let AWS know all the chunks that need to be
        ///         combined. AWS is designed for the client to track the chunks.  The client needs to track this.
        ///         Problem is, the client in this case is a web browser, that uploads to the web server the
        ///         chunks.  The web browser using Telerik controls can't track this. So, we track the chunks
        ///         by storying the state in cache.  That is why <see cref="IMemoryCache" /> is needed.
        ///     </para>
        /// </remarks>
        public AmazonStorage(AmazonStorageConfig config, IMemoryCache cache)
        {
            this.config = config;
            this.cache = cache;
        }

        /// <summary>
        ///     Appends byte array to an existing blob.
        /// </summary>
        /// <param name="data">Bytes to append.</param>
        /// <param name="fileMetaData">'Chunk' metadata being appended as a <see cref="FileUploadMetaData"/>.</param>
        /// <param name="uploadDateTime">Date and time uploaded as a <see cref="DateTimeOffset"/>.</param>
        /// <param name="mode">Mode is either append or block.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        /// <remarks>
        /// Existing blobs will be overwritten if they already exists, otherwise a new blob is created.
        /// </remarks>
        public async Task AppendBlobAsync(byte[] data, FileUploadMetaData fileMetaData, DateTimeOffset uploadDateTime, string mode)
        {
            // ReSharper disable once PossibleNullReferenceException
            using var client = GetClient();

            if (fileMetaData.TotalChunks == 1 || mode.Equals("block", StringComparison.CurrentCultureIgnoreCase))
            {
                // This is NOT a multi part upload
                await DeleteIfExistsAsync(fileMetaData.RelativePath);

                await using var memoryStream = new MemoryStream(data);

                // 2. Put the object-set ContentType and add metadata.
                var putRequest = new PutObjectRequest
                {
                    BucketName = config.BucketName,
                    Key = fileMetaData.RelativePath,
                    InputStream = memoryStream,
                    ContentType = Utilities.GetContentType(fileMetaData)
                };

                putRequest.Metadata.Add("ccmsuploaduid", fileMetaData.UploadUid);
                putRequest.Metadata.Add("ccmssize", fileMetaData.TotalFileSize.ToString());
                putRequest.Metadata.Add("ccmsdatetime", uploadDateTime.UtcDateTime.Ticks.ToString());

                await client.PutObjectAsync(putRequest);

                // Dispose of the client.
                client.Dispose();
            }
            else
            {
                if (fileMetaData.ChunkIndex == 0)
                {
                    await DeleteIfExistsAsync(fileMetaData.RelativePath);

                    var initiateRequest = new InitiateMultipartUploadRequest
                    {
                        BucketName = config.BucketName,
                        Key = fileMetaData.RelativePath,
                        Headers = { ContentType = Utilities.GetContentType(fileMetaData) }
                    };

                    initiateRequest.Metadata.Add("ccmsuploaduid", fileMetaData.UploadUid);
                    initiateRequest.Metadata.Add("ccmssize", fileMetaData.TotalFileSize.ToString());
                    initiateRequest.Metadata.Add("ccmsdatetime", uploadDateTime.UtcDateTime.Ticks.ToString());

                    var initiateUpload =
                        await client.InitiateMultipartUploadAsync(initiateRequest);

                    cache.Set(
                        fileMetaData.UploadUid,
                        initiateUpload.UploadId,
                        new MemoryCacheEntryOptions
                        {
                            SlidingExpiration = TimeSpan.FromMinutes(15)
                        });

                    // Save the upload ID in the blob metadata
                    //await client.PutObjectAsync(request);
                }

                // This is the upload ID used to keep track of all the parts.
                var uploadId = (string)cache.Get(fileMetaData.UploadUid);

                try
                {
                    var bytes = (byte[])cache.Get(fileMetaData.UploadUid + "responses");

                    var responses = bytes == null
                        ? new List<UploadPartResponse>()
                        : JsonConvert.DeserializeObject<List<UploadPartResponse>>(Encoding.UTF32.GetString(bytes));


                    await using Stream stream = new MemoryStream(data);
                    stream.Position = 0;

                    // Upload 5 MB chunk here
                    var uploadRequest = new UploadPartRequest
                    {
                        BucketName = config.BucketName,
                        Key = fileMetaData.RelativePath,
                        UploadId = uploadId, // get this from initiation
                        PartNumber = Convert.ToInt32(fileMetaData.ChunkIndex) + 1, // not 0 index based
                        PartSize = stream.Length,
                        InputStream = stream
                    };
                    var uploadResult = await client.UploadPartAsync(uploadRequest);
                    responses.Add(uploadResult);

                    if (fileMetaData.TotalChunks - 1 == fileMetaData.ChunkIndex)
                    {
                        // This is the last chunk, wrap things up here.

                        // Setup to complete the upload.
                        var completeRequest = new CompleteMultipartUploadRequest
                        {
                            BucketName = config.BucketName,
                            Key = fileMetaData.RelativePath,
                            UploadId = uploadId
                        };

                        completeRequest.AddPartETags(responses);

                        // Complete the upload.
                        await client.CompleteMultipartUploadAsync(completeRequest);

                        cache.Remove(fileMetaData.UploadUid + "responses");

                        // Dispose of the client.
                        client.Dispose();
                    }
                    else
                    {
                        // Cache the list for the next chunk upload
                        cache.Set(fileMetaData.UploadUid + "responses",
                            Encoding.UTF32.GetBytes(JsonConvert.SerializeObject(responses)));
                    }
                }
                catch (Exception)
                {
                    // Something didn't go right, so cancel the multi part upload here.
                    var abortRequest = new AbortMultipartUploadRequest
                    {
                        BucketName = config.BucketName,
                        Key = fileMetaData.RelativePath,
                        UploadId = uploadId
                    };
                    await client.AbortMultipartUploadAsync(abortRequest);
                    throw;
                }
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

            return blob != null && blob.HttpStatusCode != HttpStatusCode.NotFound;
        }

        /// <summary>
        ///     Copies a single blob and returns it.
        /// </summary>
        /// <param name="source">Path to the blob to copy.</param>
        /// <param name="destination">Path to where the blob should be copied.</param>
        /// <returns>The destination or new.</returns>
        /// <remarks>
        ///     Tip: After operation, check the returned blob object to see if it exists.
        /// </remarks>
        public async Task CopyBlobAsync(string source, string destination)
        {
            var sourceObject = await GetBlobAsync(source);

            if (sourceObject != null)
            {
                using var client = GetClient();

                await client.CopyObjectAsync(
                    config.BucketName,
                    source,
                    config.BucketName, destination);
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
            //
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

            var blob = await GetBlobAsync(fullPath);

            // Create or return existing here.
            if (blob == null)
            {
                var request = new PutObjectRequest
                {
                    BucketName = config.BucketName,
                    Key = fullPath,
                    ContentBody = $"This is a folder stub file for {path}."
                };

                using var client = GetClient();

                await client.PutObjectAsync(request);
            }
        }

        /// <summary>
        ///     Gets a list of blobs by path.
        /// </summary>
        /// <param name="path">Path to get blob names from.</param>
        /// <param name="filter">Search filter (optional).</param>
        /// <returns>Returns the names as a <see cref="string"/> list.</returns>
        public async Task<List<string>> GetBlobNamesByPath(string path)
        {
            var blobList = await GetObjectsAsync(path);
            return blobList.Select(s => s.Key).ToList();
        }

        /// <summary>
        ///     Deletes a file (blob).
        /// </summary>
        /// <param name="path">Path to doomed blob.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public async Task DeleteIfExistsAsync(string path)
        {
            try
            {
                var deleteObjectRequest = new DeleteObjectRequest
                {
                    BucketName = config.BucketName,
                    Key = path
                };

                using var client = GetClient();

                await client.DeleteObjectAsync(deleteObjectRequest);
            }
            catch (AmazonS3Exception e)
            {
                if (e.StatusCode != HttpStatusCode.NotFound)
                {
                    throw;
                }
            }
        }

        /// <summary>
        ///     Deletes a folder and all its contents.
        /// </summary>
        /// <param name="path">Path to doomed folder.</param>
        /// <returns>Returns the number of deleted obhects as an <see cref="int"/>.</returns>
        public async Task<int> DeleteFolderAsync(string path)
        {
            var blobs = await GetObjectsAsync(path);

            if (blobs.Any())
            {
                using var client = GetClient();

                var deleteObjectsRequest = new DeleteObjectsRequest
                {
                    BucketName = config.BucketName
                };

                foreach (var blob in blobs)
                {
                    deleteObjectsRequest.AddKey(blob.Key);
                }

                var result = await client.DeleteObjectsAsync(deleteObjectsRequest);
                return result.DeletedObjects.Count;
            }

            return 0;
        }

        /// <summary>
        ///     Gets files and subfolders for a given path.
        /// </summary>
        /// <param name="path">Path from which to get objects from.</param>
        /// <returns>Returns objects as a <see cref="BlobHierarchyItem"/> <see cref="List{T}"/>.</returns>
        public async Task<List<S3Object>> GetObjectsAsync(string path)
        {
            path = path.TrimStart('/');

            var request = new ListObjectsV2Request
            {
                BucketName = config.BucketName
            };

            if (!string.IsNullOrEmpty(path))
            {
                request.Prefix = path;
            }

            ListObjectsV2Response response;
            var blobList = new List<S3Object>();

            using var client = GetClient();

            do
            {
                response = await client.ListObjectsV2Async(request);

                // Process the response.
                if (response.S3Objects != null)
                {
                    foreach (var blobItem in response.S3Objects)
                    {
                        blobList.Add(blobItem);
                    }
                }

                request.ContinuationToken = response.NextContinuationToken;
            } while (response.IsTruncated == true);

            return blobList;
        }

        /// <summary>
        /// Gets the files and directories for a given path.
        /// </summary>
        /// <param name="path">Path to search.</param>
        /// <returns>FileManagerEntry list.</returns>
        public async Task<List<FileManagerEntry>> GetFilesAndDirectories(string path)
        {
            var blobs = await GetObjectsAsync(path);
            var list = new List<FileManagerEntry>();

            foreach (var blob in blobs)
            {
                var isFolder = IsFolder(blob.Key);
                if (isFolder)
                {
                    var pathToFolder = blob.Key.TrimEnd('/').Replace("folder.stubxx", string.Empty);
                    var subs = await GetObjectsAsync(pathToFolder);

                    list.Add(new FileManagerEntry()
                    {
                        Created = DateTime.SpecifyKind(blob.LastModified ?? DateTime.UtcNow, DateTimeKind.Utc),
                        CreatedUtc = DateTime.SpecifyKind(blob.LastModified ?? DateTime.UtcNow, DateTimeKind.Utc),
                        Extension = System.IO.Path.GetExtension(blob.Key),
                        HasDirectories = subs.Any(a => IsFolder(a.Key)),
                        IsDirectory = true,
                        Modified = DateTime.SpecifyKind(blob.LastModified ?? DateTime.UtcNow, DateTimeKind.Utc),
                        ModifiedUtc = DateTime.SpecifyKind(blob.LastModified ?? DateTime.UtcNow, DateTimeKind.Utc),
                        Name = System.IO.Path.GetFileName(blob.Key.TrimEnd('/').Replace("folder.stubxx", string.Empty)),
                        Path = pathToFolder
                    });
                }
                else
                {
                    list.Add(new FileManagerEntry()
                    {
                        Created = DateTime.SpecifyKind(blob.LastModified ?? DateTime.UtcNow, DateTimeKind.Utc),
                        CreatedUtc = DateTime.SpecifyKind(blob.LastModified ?? DateTime.UtcNow, DateTimeKind.Utc),
                        Extension = System.IO.Path.GetExtension(blob.Key),
                        HasDirectories = false,
                        IsDirectory = false,
                        Modified = DateTime.SpecifyKind(blob.LastModified ?? DateTime.UtcNow, DateTimeKind.Utc),
                        ModifiedUtc = DateTime.SpecifyKind(blob.LastModified ?? DateTime.UtcNow, DateTimeKind.Utc),
                        Name = System.IO.Path.GetFileName(blob.Key),
                        Path = blob.Key
                    });
                }
            }

            return list;
        }

        private bool IsFolder(string key)
        {
            return key.EndsWith("folder.stubxx", StringComparison.CurrentCultureIgnoreCase) || key.EndsWith("/");
        }

        /// <summary>
        ///     Gets a <see cref="BlobClient"/> for a blob.
        /// </summary>
        /// <param name="path">Path to blob.</param>
        /// <returns>Returns a <see cref="BlobClient"/>.</returns>
        public async Task<GetObjectResponse> GetBlobAsync(string path)
        {
            try
            {
                if (string.IsNullOrEmpty(path))
                {
                    return null;
                }

                path = path.TrimStart('/');

                using var client = GetClient();

                var response = await client.GetObjectAsync(config.BucketName, path);
                return response;
            }
            catch (AmazonS3Exception e)
            {
                // object not found
                if (e.StatusCode == HttpStatusCode.NotFound)
                {
                    return null;
                }

                throw;
            }
        }

        /// <summary>
        /// Gets metadata for a blob.
        /// </summary>
        /// <param name="path">Path to file from which to get metadata.</param>
        /// <returns>Returns metadata as a <see cref="FileMetadata"/>.</returns>
        public async Task<FileMetadata> GetFileMetadataAsync(string target)
        {
            var blob = await GetBlobAsync(target);

            _ = long.TryParse(blob.Metadata["ccmsuploaduid"], out var mark);
            return new FileMetadata()
            {
                ContentLength = blob.ContentLength,
                ContentType = blob.Headers.ContentType,
                ETag = blob.ETag,
                LastModified = DateTime.SpecifyKind(blob.LastModified ?? DateTime.UtcNow, DateTimeKind.Utc),
                Created = DateTime.SpecifyKind(blob.LastModified ?? DateTime.UtcNow, DateTimeKind.Utc)
            };
        }

        /// <inheritdoc/>
        public Task<List<FileMetadata>> GetInventory()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Opens a read stream to download the blob.
        /// </summary>
        /// <param name="path">Path to blob from which to open.</param>
        /// <returns>Returns data as a <see cref="Stream"/>.</returns>
        public async Task<Stream> GetStreamAsync(string path)
        {
            var blob = await GetBlobAsync(path);
            return blob.ResponseStream;
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
            using var client = GetClient();

            // Get rid of old blob
            await DeleteIfExistsAsync(fileMetaData.RelativePath);

            var putRequest = new PutObjectRequest
            {
                BucketName = config.BucketName,
                Key = fileMetaData.RelativePath,
                InputStream = readStream,
                ContentType = Utilities.GetContentType(fileMetaData)
            };

            putRequest.Metadata.Add("ccmsuploaduid", fileMetaData.UploadUid);
            putRequest.Metadata.Add("ccmssize", fileMetaData.TotalFileSize.ToString());
            putRequest.Metadata.Add("ccmsdatetime", uploadDateTime.UtcDateTime.Ticks.ToString());

            var result = await client.PutObjectAsync(putRequest);

            return result.HttpStatusCode == HttpStatusCode.OK;
        }


        /// <summary>
        /// Gets the amount of bytes consumed in a storage account container.
        /// </summary>
        /// <returns>Returns the number of bytes consumed for the container as a <see cref="long"/>.</returns>
        public async Task<long> GetBytesConsumed()
        {
            using var client = GetClient();
            var list = await client.ListObjectsV2Async(new ListObjectsV2Request
            {
                BucketName = config.BucketName
            });

            return list.S3Objects.Sum(s => s.Size) ?? 0;
        }

        /// <summary>
        /// Gets the thumbnail stream for a given blob.
        /// </summary>
        /// <param name="target">Path to the blob.</param>
        /// <returns>Returns the thumbnail stream as a <see cref="Stream"/>.</returns>
        public Task<Stream> GetImageThumbnailStreamAsync(string target)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Gets the storage consumption for the container.
        /// </summary>
        /// <returns>Byte count.</returns>
        public async Task<long> GetStorageConsumption()
        {
            return await GetBytesConsumed();
        }

        private AmazonS3Client GetClient()
        {
            var regionIdentifier = RegionEndpoint.GetBySystemName(config.AmazonRegion);

            return new AmazonS3Client(config.KeyId, config.Key, new AmazonS3Config()
            {
                RegionEndpoint = regionIdentifier
            });
        }
    }
}
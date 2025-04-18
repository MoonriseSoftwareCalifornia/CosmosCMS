// <copyright file="ICosmosStorage.cs" company="Moonrise Software, LLC">
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
    using System.Threading.Tasks;
    using Cosmos.BlobService.Models;

    /// <summary>
    /// Cosmos storage interface.
    /// </summary>
    public interface ICosmosStorage
    {
        /// <summary>
        ///     Appends byte array to blob(s).
        /// </summary>
        /// <param name="data">Byte array to a blob.</param>
        /// <param name="fileMetaData">File metadata.</param>
        /// <param name="uploadDateTime">Upload date and time.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        Task AppendBlobAsync(byte[] data, FileUploadMetaData fileMetaData, DateTimeOffset uploadDateTime);

        /// <summary>
        ///     Checks to see if a blob exists.
        /// </summary>
        /// <param name="path">Path to blob.</param>
        /// <returns>A <see cref="bool"/> indicating blob exists or not.</returns>
        Task<bool> BlobExistsAsync(string path);

        /// <summary>
        ///     Copies a blob from the source to the destination.
        /// </summary>
        /// Copies a blob from the source to the destination.
        /// <param name="source">Path to source.</param>
        /// <param name="destination">Path to destination.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        Task CopyBlobAsync(string source, string destination);

        /// <summary>
        ///     Creates a folder at the given path.
        /// </summary>
        /// <param name="path">Path to the folder.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        Task CreateFolderAsync(string path);

        /// <summary>
        ///     Deletes a file (blob).
        /// </summary>
        /// <param name="target">Path to the object to delete.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        Task DeleteIfExistsAsync(string target);

        /// <summary>
        ///     Deletes a folder an all its contents.
        /// </summary>
        /// <param name="target">Path to the folder to delete.</param>
        /// <returns>Number of files deleted.</returns>
        Task<int> DeleteFolderAsync(string target);

        /// <summary>
        /// Gets the number of bytes consumed for a storage account.
        /// </summary>
        /// <returns>Returns the number of bytes consumed as a <see cref="long"/>.</returns>
        Task<long> GetBytesConsumed();

        /// <summary>
        ///     Gets a list of blob names for a given path (is recursive).
        /// </summary>
        /// <param name="path">That to search for blobs.</param>
        /// <param name="filter">Filter the use to search (optional).</param>
        /// <returns>A <see cref="List{T}"/> representing blobs found in the given path.</returns>
        Task<List<string>> GetBlobNamesByPath(string path, string[] filter = null);

        /// <summary>
        /// Opens a read stream from the blob in storage.
        /// </summary>
        /// <param name="target">Path to blob to read.</param>
        /// <returns>A <see cref="Stream"/> from the blob.</returns>
        Task<Stream> GetImageThumbnailStreamAsync(string target);

        /// <summary>
        /// Gets an inventory of all blobs in a storage account.
        /// </summary>
        /// <returns>A <see cref="Task"/> with a list of files as a <see cref="List{T}"/>.</returns>
        Task<List<FileMetadata>> GetInventory();

        /// <summary>
        /// Gets metadata for a blob.
        /// </summary>
        /// <param name="target">Path to blob.</param>
        /// <returns>A <see cref="Task"/> with <see cref="FileMetadata"/> representing blob metadata.</returns>
        Task<FileMetadata> GetFileMetadataAsync(string target);

        /// <summary>
        /// Opens a read stream from the blob in storage.
        /// </summary>
        /// <param name="target">Path to blob to read.</param>
        /// <returns>A <see cref="Stream"/> from the blob.</returns>
        Task<Stream> GetStreamAsync(string target);

        /// <summary>
        /// Uploads file as a <see cref="Stream"/> to a file.
        /// </summary>
        /// <param name="readStream">Stream being uploaded to a blob.</param>
        /// <param name="fileMetaData">Stream meta data being uploaded.</param>
        /// <param name="uploadDateTime">Upload date and time.</param>
        /// <returns>A <see cref="Task"/> indicating success as a <see cref="bool"/>.</returns>
        Task<bool> UploadStreamAsync(Stream readStream, FileUploadMetaData fileMetaData, DateTimeOffset uploadDateTime);
    }
}
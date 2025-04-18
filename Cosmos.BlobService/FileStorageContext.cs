// <copyright file="FileStorageContext.cs" company="Moonrise Software, LLC">
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
    using System.Threading.Tasks;
    using Cosmos.BlobService.Drivers;
    using Cosmos.BlobService.Models;

    /// <summary>
    ///     Azure Files Share Context.
    /// </summary>
    public sealed class FileStorageContext
    {
        /// <summary>
        /// Azure file share driver, this is not handled in the collection.
        /// </summary>
        private readonly AzureFileStorage driver;

        /// <summary>
        /// Initializes a new instance of the <see cref="FileStorageContext"/> class.
        /// </summary>
        /// <param name="connectionString">File storage connection string.</param>
        /// <param name="sharename">File storage share name.</param>
        public FileStorageContext(string connectionString, string sharename)
        {
            this.driver = new AzureFileStorage(connectionString, sharename);
        }

        /// <summary>
        ///     Determines if a blob exists.
        /// </summary>
        /// <param name="path">Path to blob.</param>
        /// <returns>Returns a <see cref="bool"/> indicating that an item exists or not. </returns>
        public async Task<bool> BlobExistsAsync(string path)
        {
            return await this.driver.BlobExistsAsync(path);
        }

        /// <summary>
        ///     Copies a file or folder.
        /// </summary>
        /// <param name="sourcePath">Path to source file or folder.</param>
        /// <param name="destFolderPath">Path to destination folder.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public async Task CopyAsync(string sourcePath, string destFolderPath)
        {
            await this.driver.CopyBlobAsync(sourcePath, destFolderPath);
        }

        /// <summary>
        ///     Delete a folder.
        /// </summary>
        /// <param name="folder">Path to folder.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public async Task DeleteFolderAsync(string folder)
        {
            // Ensure leading slash is removed.
            await this.driver.DeleteFolderAsync(folder);
        }

        /// <summary>
        ///     Deletes a file.
        /// </summary>
        /// <param name="target">A <see cref="Task"/> representing the asynchronous operation.</param>
        /// <returns>Returns a <see cref="Task"/> representing the asynchronous operation.</returns>
        public async Task DeleteFileAsync(string target)
        {
            // Ensure leading slash is removed.
            target = target.TrimStart('/');
            await this.driver.DeleteIfExistsAsync(target);
        }

        /// <summary>
        ///     Gets the metadata for a file.
        /// </summary>
        /// <param name="target">Path to the file.</param>
        /// <returns>Returns a <see cref="FileManagerEntry"/> representing metadata for a file.</returns>
        public async Task<FileManagerEntry> GetFileAsync(string target)
        {
            // Ensure leading slash is removed.
            target = target.TrimStart('/');

            var fileManagerEntry = await this.driver.GetBlobAsync(target);
            return fileManagerEntry;
        }

        /// <summary>
        ///     Moves a file or folder to a specified folder.
        /// </summary>
        /// <param name="sourcePath">Path to source file or folder.</param>
        /// <param name="destFolderPath">Path to destination folder.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public async Task MoveAsync(string sourcePath, string destFolderPath)
        {
            await this.driver.MoveAsync(sourcePath, destFolderPath);
        }

        /// <summary>
        ///     Returns a response stream from the primary blob storage provider.
        /// </summary>
        /// <param name="target">Path to the blob to open.</param>
        /// <returns>A <see cref="Stream"/> that reads bytes from a blob.</returns>
        public async Task<Stream> OpenBlobReadStreamAsync(string target)
        {
            // Ensure leading slash is removed.
            target = target.TrimStart('/');

            return await this.driver.GetStreamAsync(target);
        }

        /// <summary>
        ///     Renames (and can move) a file or folder.
        /// </summary>
        /// <param name="sourcePath">Full path to item to change.</param>
        /// <param name="destinationPath">Full path to the new name.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public async Task RenameAsync(string sourcePath, string destinationPath)
        {
            await this.driver.RenameAsync(sourcePath, destinationPath);
        }

        /// <summary>
        ///     Append bytes to blob(s).
        /// </summary>
        /// <param name="stream">Data <see cref="MemoryStream"/> to append to the blob.</param>
        /// <param name="fileMetaData">Data chuck upload metadata.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public async Task AppendBlob(MemoryStream stream, FileUploadMetaData fileMetaData)
        {
            await this.driver.AppendBlobAsync(stream.ToArray(), fileMetaData, DateTimeOffset.UtcNow);
        }

        /// <summary>
        ///     Creates a folder in all the cloud storage accounts.
        /// </summary>
        /// <param name="folderName">Name of folder to create.</param>
        /// <returns>Returns metadata for the folder as a <see cref="FileManagerEntry"/>.</returns>
        /// <remarks>Creates the folder if it does not already exist.</remarks>
        public async Task<FileManagerEntry> CreateFolder(string folderName)
        {
            await this.driver.CreateFolderAsync(folderName);
            var folder = await this.driver.GetBlobAsync(folderName);
            return folder;
        }

        /// <summary>
        /// Creates a file or fold object.
        /// </summary>
        /// <param name="path">Path to the object.</param>
        /// <returns>Returns object metadata as a <see cref="FileManagerEntry"/>.</returns>
        public async Task<FileManagerEntry> GetObjectAsync(string path)
        {
            var item = await this.driver.GetObjectAsync(path);
            return item;
        }

        /// <summary>
        ///     Gets files and subfolders for a given path.
        /// </summary>
        /// <param name="path">Path to get files and folders.</param>
        /// <returns>Returns the metadata of what is find as a <see cref="FileManagerEntry"/> list.</returns>
        public async Task<List<FileManagerEntry>> GetObjectsAsync(string path)
        {
            if (!string.IsNullOrEmpty(path))
            {
                path = path.TrimStart('/');
            }

            var entries = await this.driver.GetObjectsAsync(path);
            return entries;
        }

        /// <summary>
        ///     Gets the contents for a folder.
        /// </summary>
        /// <param name="path">Path to folder to retrieve contents.</param>
        /// <returns>Returns the metadata of what is find as a <see cref="FileManagerEntry"/> list.</returns>
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
    }
}
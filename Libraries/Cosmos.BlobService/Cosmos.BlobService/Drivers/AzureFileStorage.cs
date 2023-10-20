// <copyright file="AzureFileStorage.cs" company="Moonrise Software, LLC">
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
    using System.Threading.Tasks;
    using Azure.Storage.Files.Shares;
    using Azure.Storage.Files.Shares.Models;
    using Cosmos.BlobService.Models;

    /// <summary>
    /// Driver for Azure File Share service.
    /// </summary>
    public sealed class AzureFileStorage : ICosmosStorage
    {
        // See: https://github.com/Azure/azure-sdk-for-net/blob/main/sdk/storage/Azure.Storage.Files.Shares/samples/Sample01b_HelloWorldAsync.cs
        private readonly ShareClient shareClient;

        /// <summary>
        /// Initializes a new instance of the <see cref="AzureFileStorage"/> class.
        /// Constructor.
        /// </summary>
        /// <param name="connectionString">Azure file storage connection string.</param>
        /// <param name="fileShare">Azure storage fileshare name.</param>
        public AzureFileStorage(string connectionString, string fileShare)
        {
            shareClient = new ShareClient(connectionString, fileShare);
            _ = shareClient.CreateIfNotExistsAsync().Result;
        }

        /// <inheritdoc/>
        public async Task AppendBlobAsync(byte[] data, FileUploadMetaData fileMetaData, DateTimeOffset uploadDateTime)
        {
            // Name of the directory and file we'll create
            var dirName = Path.GetDirectoryName(fileMetaData.RelativePath);

            // Get a reference to a directory and create it
            var directory = shareClient.GetDirectoryClient(dirName);
            if (!await directory.ExistsAsync())
            {
                await CreateFolderAsync(dirName); // Make sure the folder exists.
            }

            // Get a reference to a file and upload it.
            ShareFileClient file = directory.GetFileClient(fileMetaData.FileName);
            using var memStream = new MemoryStream(data);

            if (!await file.ExistsAsync())
            {
                await file.CreateAsync(fileMetaData.TotalFileSize);
            }

            await file.UploadAsync(memStream);
        }

        /// <inheritdoc/>
        public async Task<bool> BlobExistsAsync(string path)
        {
            // Name of the directory and file we'll create
            var dirName = Path.GetDirectoryName(path);

            // Get a reference to a directory and create it
            var directory = shareClient.GetDirectoryClient(dirName);
            if (!await directory.ExistsAsync())
            {
                return false;
            }

            var fileName = Path.GetFileName(path);
            var file = directory.GetFileClient(fileName);

            return await file.ExistsAsync();
        }

        /// <summary>
        /// Rename a file.
        /// </summary>
        /// <param name="path">Path to the item being renamed.</param>
        /// <param name="destination">Where it is being moved to.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public async Task RenameAsync(string path, string destination)
        {
            var source = await GetObjectAsync(path);

            if (source.IsDirectory)
            {
                var directory = shareClient.GetDirectoryClient(path);
                await directory.RenameAsync(destination);
            }
            else
            {
                path = Path.GetDirectoryName(path).Replace("\\", "/");
                var fileName = Path.GetFileName(path);
                var directory = shareClient.GetDirectoryClient(path);
                var file = directory.GetFileClient(fileName);
                await file.RenameAsync(destination);
            }
        }

        /// <inheritdoc/>
        public async Task CopyBlobAsync(string sourcePath, string destDirectoryPath)
        {
            sourcePath = sourcePath.Trim('/');
            destDirectoryPath = destDirectoryPath.Trim('/');

            if (sourcePath.Equals(destDirectoryPath, StringComparison.InvariantCultureIgnoreCase))
            {
                throw new Exception("Source and destination cannot be the same.");
            }

            var sourceItem = await GetObjectAsync(sourcePath);
            var destinationItem = await GetObjectAsync(destDirectoryPath);
            var root = shareClient.GetRootDirectoryClient();

            if (destinationItem == null || destinationItem.IsDirectory == false)
            {
                throw new Exception($"Must be an existing directory: {destDirectoryPath}");
            }

            if (sourceItem.IsDirectory)
            {
                // Get everything that needs copying
                var items = await GetBlobNamesByPath(sourcePath);

                foreach (var item in items)
                {
                    var sourceFile = root.GetFileClient(item);
                    var info = await sourceFile.GetPropertiesAsync();

                    var destPath = destDirectoryPath.TrimEnd('/') + "/" + sourceFile.Path.Replace(sourcePath, string.Empty).TrimStart('/');

                    var dir = Path.GetDirectoryName(destPath).Replace("\\", "/");

                    if (!destDirectoryPath.Equals(dir, StringComparison.InvariantCultureIgnoreCase))
                    {
                        await CreateFolderAsync(dir);
                    }

                    var destFile = root.GetFileClient(destPath);
                    await destFile.CreateAsync(info.Value.ContentLength);
                    await destFile.StartCopyAsync(sourceFile.Uri);
                }
            }
            else
            {
                var sourceFile = root.GetFileClient(sourcePath);
                var info = await sourceFile.GetPropertiesAsync();

                var destPath = destDirectoryPath.TrimEnd('/') + "/" + sourceFile.Path.Replace(sourcePath, string.Empty).TrimStart('/');

                var dir = Path.GetDirectoryName(destPath).Replace("\\", "/");

                await CreateFolderAsync(dir);

                var destFile = root.GetFileClient(destPath.TrimEnd('/') + "/" + sourceFile.Name);

                await destFile.CreateAsync(info.Value.ContentLength);
                await destFile.StartCopyAsync(sourceFile.Uri);
            }
        }

        /// <inheritdoc/>
        public async Task CreateFolderAsync(string target)
        {
            // Name of the directory and file we'll create
            target = target.Replace("\\", "/");

            var pathParts = target.Trim('/').Split('/').ToList();

            var directory = shareClient.GetRootDirectoryClient();

            await CreateSubDirectory(directory, pathParts);
        }

        /// <inheritdoc/>
        public async Task<int> DeleteFolderAsync(string target)
        {
            target = target.Trim('/');

            ShareDirectoryClient rootDir;

            if (string.IsNullOrEmpty(target))
            {
                rootDir = shareClient.GetRootDirectoryClient();
            }
            else
            {
                rootDir = shareClient.GetDirectoryClient(target);
            }

            if (await rootDir.ExistsAsync())
            {
                // We are at root
                var items = rootDir.GetFilesAndDirectoriesAsync();
                await foreach (var item in items)
                {
                    if (item.IsDirectory)
                    {
                        await DeleteAllAsync(rootDir.GetSubdirectoryClient(item.Name));
                    }
                    else
                    {
                        await rootDir.DeleteFileAsync(item.Name);
                    }
                }

                if (!string.IsNullOrEmpty(rootDir.Path))
                {
                    await rootDir.DeleteIfExistsAsync();
                }
            }

            return 0;
        }

        /// <inheritdoc/>
        public async Task DeleteIfExistsAsync(string path)
        {
            if (path == "/" || string.IsNullOrEmpty(path))
            {
                return;
            }

            path = path.TrimStart('/');

            // Get a reference to a directory and create it
            var directory = shareClient.GetRootDirectoryClient();

            var file = directory.GetFileClient($"/{path}");

            // Get a reference to a directory and create it
            await file.DeleteIfExistsAsync();
        }

        /// <inheritdoc/>
        public async Task<List<string>> GetBlobNamesByPath(string path, string[] filter = null)
        {
            var list = new List<string>();

            // Get a reference to a directory and create it
            var directory = shareClient.GetDirectoryClient(path);
            var contents = directory.GetFilesAndDirectoriesAsync();

            await foreach (var item in contents)
            {
                if (item.IsDirectory)
                {
                    var subDirectory = directory.GetSubdirectoryClient(item.Name);
                    var results = await GetAllBlobsAsync(subDirectory);
                    list.AddRange(results);
                }
                else
                {
                    list.Add($"{path}/{item.Name}");
                }
            }

            return list;
        }

        /// <inheritdoc/>
        public async Task<FileMetadata> GetFileMetadataAsync(string target)
        {
            // Name of the directory and file we'll create
            var dirName = Path.GetDirectoryName(target);
            var fileName = Path.GetFileName(target);

            if (string.IsNullOrEmpty(fileName))
            {
                return null;  // This is a directory, do nothing.
            }

            // Get a reference to a directory and create it
            var directory = shareClient.GetDirectoryClient(dirName);
            var file = directory.GetFileClient(fileName);
            var props = await file.GetPropertiesAsync();

            return new FileMetadata()
            {
                ContentLength = props.Value.ContentLength,
                ContentType = props.Value.ContentType,
                ETag = props.Value.ETag.ToString(),
                FileName = file.Name,
                LastModified = props.Value.LastModified,
                UploadDateTime = props.Value.LastModified.UtcDateTime.Ticks
            };
        }

        /// <inheritdoc/>
        public Task<List<FileMetadata>> GetInventory()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Gets a blob object.
        /// </summary>
        /// <param name="target">Path to blob.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public async Task<FileManagerEntry> GetBlobAsync(string target)
        {
            if (string.IsNullOrEmpty(target))
            {
                return null;
            }

            target = target.TrimStart('/');

            var dirName = Path.GetDirectoryName(target).Replace("\\", "/");
            var fileName = Path.GetFileName(target);

            var directory = shareClient.GetDirectoryClient(dirName);

            if (!await directory.ExistsAsync())
            {
                return null;
            }

            var file = directory.GetFileClient(fileName);

            if (!await file.ExistsAsync())
            {
                return null;
            }

            var info = await file.GetPropertiesAsync();

            DateTime dateTime;
            if (info.Value.Metadata.TryGetValue("ccmsdatetime", out string uploaded))
            {
                dateTime = DateTimeOffset.Parse(uploaded).UtcDateTime;
            }
            else
            {
                dateTime = info.Value.LastModified.UtcDateTime;
            }

            return new FileManagerEntry()
            {
                Created = dateTime,
                CreatedUtc = dateTime.ToUniversalTime(),
                Extension = Path.GetExtension(fileName),
                HasDirectories = false,
                IsDirectory = false,
                Modified = info.Value.LastModified.DateTime,
                ModifiedUtc = info.Value.LastModified.UtcDateTime,
                Name = fileName,
                Path = dirName.TrimEnd('/') + "/" + fileName,
                Size = info.Value.ContentLength
            };
        }

        /// <summary>
        /// Gets a folder or file at the given path (if it exists).
        /// </summary>
        /// <param name="path">Path to blob.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public async Task<FileManagerEntry> GetObjectAsync(string path)
        {
            var item = shareClient.GetDirectoryClient(path);

            if (await item.ExistsAsync())
            {
                // This is a directory, get information and return
                var subItems = item.GetFilesAndDirectoriesAsync();

                var hasDirectories = false;

                await foreach (var subItem in subItems)
                {
                    hasDirectories = true;
                    break;
                }

                var directoryInfo = await item.GetPropertiesAsync();

                return new FileManagerEntry()
                {
                    Created = directoryInfo.Value.SmbProperties.FileCreatedOn.HasValue ? directoryInfo.Value.SmbProperties.FileCreatedOn.Value.DateTime : directoryInfo.Value.LastModified.DateTime,
                    CreatedUtc = directoryInfo.Value.SmbProperties.FileCreatedOn.HasValue ? directoryInfo.Value.SmbProperties.FileCreatedOn.Value.DateTime.ToUniversalTime() : directoryInfo.Value.LastModified.DateTime.ToUniversalTime(),
                    Extension = Path.GetExtension(path),
                    HasDirectories = hasDirectories,
                    IsDirectory = true,
                    Modified = directoryInfo.Value.LastModified.DateTime,
                    ModifiedUtc = directoryInfo.Value.LastModified.UtcDateTime,
                    Name = item.Name,
                    Path = item.Path
                };
            }

            // This is a file
            var folder = Path.GetDirectoryName(path).Replace("\\", "/");

            ShareDirectoryClient dir;
            if (string.IsNullOrEmpty(folder))
            {
                // This might be a root file
                dir = shareClient.GetRootDirectoryClient();
            }
            else
            {
                dir = shareClient.GetDirectoryClient(folder);
            }

            var file = dir.GetFileClient(Path.GetFileName(path));

            if (!await file.ExistsAsync())
            {
                return null;
            }

            var fileInfo = await file.GetPropertiesAsync();

            return new FileManagerEntry()
            {
                Created = fileInfo.Value.SmbProperties.FileCreatedOn.HasValue ? fileInfo.Value.SmbProperties.FileCreatedOn.Value.DateTime : fileInfo.Value.LastModified.DateTime,
                CreatedUtc = fileInfo.Value.SmbProperties.FileCreatedOn.HasValue ? fileInfo.Value.SmbProperties.FileCreatedOn.Value.DateTime.ToUniversalTime() : fileInfo.Value.LastModified.DateTime.ToUniversalTime(),
                Extension = Path.GetExtension(path),
                HasDirectories = false,
                IsDirectory = false,
                Modified = fileInfo.Value.LastModified.DateTime,
                ModifiedUtc = fileInfo.Value.LastModified.UtcDateTime,
                Name = file.Name,
                Path = file.Path
            };
        }

        /// <summary>
        ///     Gets files and subfolders for a given path.
        /// </summary>
        /// <param name="path">Path from which to retrieve objects.</param>
        /// <returns>Returns metadata as a <see cref="FileManagerEntry"/> <see cref="List{T}"/>.</returns>
        public async Task<List<FileManagerEntry>> GetObjectsAsync(string path)
        {
            path = path.Trim('/');

            if (string.IsNullOrEmpty(path))
            {
                path = "/";
            }

            var items = new List<FileManagerEntry>();

            // Get a reference to a directory and create it
            var directory = shareClient.GetDirectoryClient($"/{path}");
            if (await directory.ExistsAsync())
            {
                var contents = directory.GetFilesAndDirectoriesAsync();

                await foreach (var item in contents)
                {
                    if (item.IsDirectory)
                    {
                        var subClient = directory.GetSubdirectoryClient(item.Name);

                        var subList = subClient.GetFilesAndDirectoriesAsync();

                        var hasSubdirectories = false;

                        await foreach (var i in subList)
                        {
                            if (i.IsDirectory)
                            {
                                hasSubdirectories = true;
                                break;
                            }
                        }

                        var props = await subClient.GetPropertiesAsync();

                        var fileManagerEntry = new FileManagerEntry
                        {
                            Created = props.Value.LastModified.DateTime,
                            CreatedUtc = props.Value.LastModified.UtcDateTime,
                            Extension = string.Empty,
                            HasDirectories = hasSubdirectories,
                            IsDirectory = true,
                            Modified = props.Value.LastModified.DateTime,
                            ModifiedUtc = props.Value.LastModified.UtcDateTime,
                            Name = item.Name,
                            Path = $"{path}/{item.Name}",
                            Size = 0
                        };
                        items.Add(fileManagerEntry);
                    }
                    else
                    {
                        var file = directory.GetFileClient(item.Name);
                        var info = await file.GetPropertiesAsync();

                        DateTime dateTime;
                        if (info.Value.Metadata.TryGetValue("ccmsdatetime", out string uploaded))
                        {
                            dateTime = DateTimeOffset.Parse(uploaded).UtcDateTime;
                        }
                        else
                        {
                            dateTime = info.Value.LastModified.UtcDateTime;
                        }

                        var fileManagerEntry = new FileManagerEntry
                        {
                            Created = dateTime,
                            CreatedUtc = dateTime.ToUniversalTime(),
                            Extension = Path.GetExtension(item.Name),
                            HasDirectories = false,
                            IsDirectory = false,
                            Modified = info.Value.LastModified.DateTime,
                            ModifiedUtc = info.Value.LastModified.UtcDateTime,
                            Name = item.Name,
                            Path = $"{path}/{item.Name}",
                            Size = info.Value.ContentLength
                        };
                        items.Add(fileManagerEntry);
                    }
                }
            }

            return items;
        }

        /// <inheritdoc/>
        public async Task<Stream> GetStreamAsync(string target)
        {
            if (target == "/")
            {
                target = string.Empty;
            }

            if (!string.IsNullOrEmpty(target))
            {
                target = target.TrimStart('/');
            }

            var dirName = Path.GetDirectoryName($"/{target}");

            // Get a reference to a directory and create it
            var directory = shareClient.GetRootDirectoryClient();

            var file = directory.GetFileClient(target);

            if (await file.ExistsAsync())
            {
                return await file.OpenReadAsync();
            }

            throw new Exception($"Directory not found: {dirName}");
        }

        /// <inheritdoc/>
        public async Task<bool> UploadStreamAsync(Stream readStream, FileUploadMetaData fileMetaData, DateTimeOffset uploadDateTime)
        {
            // Get directory name
            var dirName = Path.GetDirectoryName(fileMetaData.RelativePath);

            // Get a reference to a directory and create it
            var directory = shareClient.GetDirectoryClient(dirName);

            // Create if not exists
            await directory.CreateIfNotExistsAsync();

            var fileName = Path.GetFileName(fileMetaData.FileName);

            var file = directory.GetFileClient(fileName);

            var dictionaryObject = new Dictionary<string, string>();
            dictionaryObject.Add("ccmsuploaduid", fileMetaData.UploadUid);
            dictionaryObject.Add("ccmssize", fileMetaData.TotalFileSize.ToString());
            dictionaryObject.Add("ccmsdatetime", uploadDateTime.UtcDateTime.Ticks.ToString());

            await file.SetMetadataAsync(dictionaryObject);

            await file.UploadAsync(readStream);

            return true;
        }

        /// <summary>
        /// Moves an item to the specified folder.
        /// </summary>
        /// <param name="sourcePath">Path to item(s) to move.</param>
        /// <param name="destinationFolderPath">Destination of where to movie item(s) to.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public async Task MoveAsync(string sourcePath, string destinationFolderPath)
        {
            sourcePath = sourcePath.Trim('/');
            destinationFolderPath = destinationFolderPath.Trim('/');

            if (sourcePath.Equals(destinationFolderPath, StringComparison.InvariantCultureIgnoreCase))
            {
                throw new Exception("The source and destination cannot be the same.");
            }

            var destObject = await GetObjectAsync(destinationFolderPath);

            if (!destObject.IsDirectory)
            {
                throw new Exception("Destination needs to be a folder");
            }

            var sourceObject = await GetObjectAsync(sourcePath);

            if (sourceObject.IsDirectory)
            {
                var sourceDirectory = shareClient.GetDirectoryClient(sourcePath);
                var destPath = destinationFolderPath + "/" + sourceDirectory.Name;
                await sourceDirectory.RenameAsync(destPath);
            }
            else
            {
                var root = shareClient.GetRootDirectoryClient();
                var sourceFile = root.GetFileClient(sourcePath);
                await sourceFile.RenameAsync(destinationFolderPath + "/" + sourceFile.Name);
            }
        }

        /// <inheritdoc/>
        public Task<long> GetBytesConsumed()
        {
            throw new NotImplementedException();
        }

        private async Task CreateSubDirectory(ShareDirectoryClient directory, List<string> path)
        {
            var client = directory.GetSubdirectoryClient(path[0]);
            await client.CreateIfNotExistsAsync();

            path.RemoveAt(0);

            if (path.Count > 0)
            {
                await CreateSubDirectory(client, path);
            }
        }

        private async Task DeleteAllAsync(ShareDirectoryClient dirClient)
        {
            await foreach (ShareFileItem item in dirClient.GetFilesAndDirectoriesAsync())
            {
                if (item.IsDirectory)
                {
                    var subDir = dirClient.GetSubdirectoryClient(item.Name);
                    await DeleteAllAsync(subDir);
                }
                else
                {
                    await dirClient.DeleteFileAsync(item.Name);
                }
            }

            await dirClient.DeleteIfExistsAsync();
        }

        /// <summary>
        /// Gets all the files in a folder (recursive).
        /// </summary>
        /// <param name="dirClient">Share directory client.</param>
        /// <returns>Array of blobs.</returns>
        private async Task<List<string>> GetAllBlobsAsync(ShareDirectoryClient dirClient)
        {
            var list = new List<string>();

            await foreach (ShareFileItem item in dirClient.GetFilesAndDirectoriesAsync())
            {
                if (item.IsDirectory)
                {
                    var subDir = dirClient.GetSubdirectoryClient(item.Name);
                    list.AddRange(await GetAllBlobsAsync(subDir));
                }
                else
                {
                    var path = dirClient.Path.Replace("\\", "/");
                    list.Add($"{path}/{item.Name}");
                }
            }

            return list;
        }
    }
}

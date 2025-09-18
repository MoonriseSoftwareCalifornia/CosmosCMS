// <copyright file="FileCacheObject.cs" company="Moonrise Software, LLC">
// Copyright (c) Moonrise Software, LLC. All rights reserved.
// Licensed under the GNU Public License, Version 3.0 (https://www.gnu.org/licenses/gpl-3.0.html)
// See https://github.com/MoonriseSoftwareCalifornia/CosmosCMS
// for more information concerning the license and the contributors participating to this project.
// </copyright>

namespace Cosmos.Publisher.Models
{
    using Cosmos.BlobService;

    /// <summary>
    /// File cache object.
    /// </summary>
    public class FileCacheObject : FileManagerEntry
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="FileCacheObject"/> class.
        /// </summary>
        public FileCacheObject()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="FileCacheObject"/> class.
        /// </summary>
        /// <param name="entry">FileManagerEntry object.</param>
        public FileCacheObject(FileManagerEntry entry)
        {
            this.Created = entry.Created;
            this.CreatedUtc = entry.CreatedUtc;
            this.Extension = entry.Extension;
            this.HasDirectories = entry.HasDirectories;
            this.IsDirectory = entry.IsDirectory;
            this.Modified = entry.Modified;
            this.ModifiedUtc = entry.ModifiedUtc;
            this.Name = entry.Name;
            this.Path = entry.Path;
            this.Size = entry.Size;
            this.ContentType = entry.ContentType;
        }

        /// <summary>
        /// Gets or sets data for the file.
        /// </summary>
        public byte[] FileData { get; set; }
    }
}

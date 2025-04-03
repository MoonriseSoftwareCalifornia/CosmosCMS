// <copyright file="FileManagerEntry.cs" company="Moonrise Software, LLC">
// Copyright (c) Moonrise Software, LLC. All rights reserved.
// Licensed under the GNU Public License, Version 3.0 (https://www.gnu.org/licenses/gpl-3.0.html)
// See https://github.com/MoonriseSoftwareCalifornia/CosmosCMS
// for more information concerning the license and the contributors participating to this project.
// </copyright>

namespace Cosmos.BlobService;

using System;

/// <summary>
/// File manager entry.
/// </summary>
public class FileManagerEntry
{
    /// <summary>
    /// Gets or sets date created.
    /// </summary>
    public DateTime Created { get; set; }

    /// <summary>
    /// Gets or sets date created in UTC.
    /// </summary>
    public DateTime CreatedUtc { get; set; }

    /// <summary>
    /// Gets or sets file extention.
    /// </summary>
    public string Extension { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether if folder, does it have child folders.
    /// </summary>
    public bool HasDirectories { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether is a directory.
    /// </summary>
    public bool IsDirectory { get; set; }

    /// <summary>
    /// Gets or sets date/time modified.
    /// </summary>
    public DateTime Modified { get; set; }

    /// <summary>
    /// Gets or sets date/time modified in UTC.
    /// </summary>
    public DateTime ModifiedUtc { get; set; }

    /// <summary>
    /// Gets or sets item name.
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// Gets or sets item path.
    /// </summary>
    public string Path { get; set; }

    /// <summary>
    /// Gets or sets size in bytes.
    /// </summary>
    public long Size { get; set; }

    /// <summary>
    /// Gets or sets the image width.
    /// </summary>
    public int? ImageX { get; set; }

    /// <summary>
    /// Gets or sets the image height.
    /// </summary>
    public int? ImageY { get; set; }

    /// <summary>
    /// Gets or sets the image DPI.
    /// </summary>
    public int? ImageDpi { get; set; }
}
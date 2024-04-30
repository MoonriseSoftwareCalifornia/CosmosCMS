// <copyright file="NewFolderViewModel.cs" company="Cosmos Website Solutions, Inc.">
// Copyright (c) Cosmos Website Solutions, Inc.. All rights reserved.
// Licensed under the GNU Public License, Version 3.0 (https://www.gnu.org/licenses/gpl-3.0.html)
// See https://github.com/MoonriseSoftwareCalifornia/CosmosCMS
// for more information concerning the license and the contributors participating to this project.
// </copyright>

namespace Cosmos.Cms.Models
{
    /// <summary>
    /// New folder view model.
    /// </summary>
    public class NewFolderViewModel
    {
        /// <summary>
        /// Gets or sets the parent folder where new folder is created as a child.
        /// </summary>
        public string ParentFolder { get; set; }

        /// <summary>
        /// Gets or sets new folder name.
        /// </summary>
        public string FolderName { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether directory only mode for file browser.
        /// </summary>
        public bool DirectoryOnly { get; set; }
    }
}

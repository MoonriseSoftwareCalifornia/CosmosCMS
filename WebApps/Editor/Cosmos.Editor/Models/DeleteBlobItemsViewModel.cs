// <copyright file="DeleteBlobItemsViewModel.cs" company="Moonrise Software, LLC">
// Copyright (c) Moonrise Software, LLC. All rights reserved.
// Licensed under the GNU Public License, Version 3.0 (https://www.gnu.org/licenses/gpl-3.0.html)
// See https://github.com/MoonriseSoftwareCalifornia/CosmosCMS
// for more information concerning the license and the contributors participating to this project.
// </copyright>

namespace Cosmos.Cms.Models
{
    using System.Collections.Generic;

    /// <summary>
    /// Delete blob items.
    /// </summary>
    public class DeleteBlobItemsViewModel
    {
        /// <summary>
        /// Gets or sets parent path when delete was executed.
        /// </summary>
        public string ParentPath { get; set; }

        /// <summary>
        /// Gets or sets full paths of items to delete.
        /// </summary>
        public List<string> Paths { get; set; }
    }
}

// <copyright file="FileManagerViewModel.cs" company="Moonrise Software, LLC">
// Copyright (c) Moonrise Software, LLC. All rights reserved.
// Licensed under the GNU Public License, Version 3.0 (https://www.gnu.org/licenses/gpl-3.0.html)
// See https://github.com/MoonriseSoftwareCalifornia/CosmosCMS
// for more information concerning the license and the contributors participating to this project.
// </copyright>

using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Cosmos.Cms.Models
{
    /// <summary>
    /// File manager view model.
    /// </summary>
    public class FileManagerViewModel
    {
        /// <summary>
        /// Gets or sets team ID.
        /// </summary>
        public int? TeamId { get; set; }

        /// <summary>
        /// Gets or sets team folders.
        /// </summary>
        public IEnumerable<SelectListItem> TeamFolders { get; set; }
    }
}
﻿// <copyright file="MoveFilesViewModel.cs" company="Moonrise Software, LLC">
// Copyright (c) Moonrise Software, LLC. All rights reserved.
// Licensed under the GNU Public License, Version 3.0 (https://www.gnu.org/licenses/gpl-3.0.html)
// See https://github.com/MoonriseSoftwareCalifornia/CosmosCMS
// for more information concerning the license and the contributors participating to this project.
// </copyright>

namespace Sky.Cms.Models
{
    using System.Collections.Generic;

    /// <summary>
    /// Files to move plus the path to move files to.
    /// </summary>
    public class MoveFilesViewModel
    {
        /// <summary>
        /// Gets or sets the folder where items will be placed.
        /// </summary>
        public string Destination { get; set; }

        /// <summary>
        /// Gets or sets items to be moved.
        /// </summary>
        public List<string> Items { get; set; }
    }
}

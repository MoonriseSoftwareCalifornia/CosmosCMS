// <copyright file="NewFileViewModel.cs" company="Moonrise Software, LLC">
// Copyright (c) Moonrise Software, LLC. All rights reserved.
// Licensed under the GNU Public License, Version 3.0 (https://www.gnu.org/licenses/gpl-3.0.html)
// See https://github.com/MoonriseSoftwareCalifornia/CosmosCMS
// for more information concerning the license and the contributors participating to this project.
// </copyright>

namespace Sky.Editor.Models
{
    using System.ComponentModel.DataAnnotations;

    /// <summary>
    /// New file name.
    /// </summary>
    public class NewFileViewModel
    {
        /// <summary>
        /// Gets or sets the parent folder where new folder is created as a child.
        /// </summary>
        [Required(AllowEmptyStrings = false, ErrorMessage = "Parent folder is required.")]
        public string ParentFolder { get; set; }

        /// <summary>
        /// Gets or sets new folder name.
        /// </summary>
        [Required(AllowEmptyStrings = false, ErrorMessage = "File name is required.")]
        public string FileName { get; set; }
    }
}

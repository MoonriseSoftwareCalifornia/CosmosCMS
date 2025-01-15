// <copyright file="LayoutFileUploadViewModel.cs" company="Moonrise Software, LLC">
// Copyright (c) Moonrise Software, LLC. All rights reserved.
// Licensed under the GNU Public License, Version 3.0 (https://www.gnu.org/licenses/gpl-3.0.html)
// See https://github.com/MoonriseSoftwareCalifornia/CosmosCMS
// for more information concerning the license and the contributors participating to this project.
// </copyright>

using System;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace Cosmos.Cms.Models
{
    /// <summary>
    /// Layout file upload view model.
    /// </summary>
    public class LayoutFileUploadViewModel
    {
        /// <summary>
        /// Gets or sets layout ID number (once saved).
        /// </summary>
        [Display(Name = "Choose layout to replace:")]
        public Guid? Id { get; set; }

        /// <summary>
        /// Gets or sets layout name.
        /// </summary>
        [Required(AllowEmptyStrings = false)]
        [Display(Name = "Layout name:")]
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets layout description.
        /// </summary>
        [Display(Name = "Description/Notes:")]
        public string Description { get; set; }

        /// <summary>
        /// Gets or sets layer file to upload.
        /// </summary>
        [Display(Name = "Select file to upload:")]
        public IFormFile File { get; set; }
    }
}

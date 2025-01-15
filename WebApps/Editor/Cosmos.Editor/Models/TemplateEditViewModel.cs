// <copyright file="TemplateEditViewModel.cs" company="Moonrise Software, LLC">
// Copyright (c) Moonrise Software, LLC. All rights reserved.
// Licensed under the GNU Public License, Version 3.0 (https://www.gnu.org/licenses/gpl-3.0.html)
// See https://github.com/MoonriseSoftwareCalifornia/CosmosCMS
// for more information concerning the license and the contributors participating to this project.
// </copyright>

using System;
using System.ComponentModel.DataAnnotations;

namespace Cosmos.Cms.Models
{
    /// <summary>
    /// Edit template title and description.
    /// </summary>
    public class TemplateEditViewModel
    {
        /// <summary>
        /// Gets or sets template ID.
        /// </summary>
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        /// <summary>
        /// Gets or sets template title.
        /// </summary>
        [Display(Name = "Template Title")]
        [StringLength(128)]
        [Required(AllowEmptyStrings = false)]
        public string Title { get; set; }

        /// <summary>
        /// Gets or sets template description.
        /// </summary>
        [Required(AllowEmptyStrings = false)]
        [Display(Name = "Description/Notes")]
        public string Description { get; set; }
    }
}

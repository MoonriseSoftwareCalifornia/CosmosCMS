// <copyright file="LayoutIndexViewModel.cs" company="Cosmos Website Solutions, Inc.">
// Copyright (c) Cosmos Website Solutions, Inc.. All rights reserved.
// Licensed under the GNU Public License, Version 3.0 (https://www.gnu.org/licenses/gpl-3.0.html)
// See https://github.com/MoonriseSoftwareCalifornia/CosmosCMS
// for more information concerning the license and the contributors participating to this project.
// </copyright>

namespace Cosmos.Cms.Models
{
    using System;
    using System.ComponentModel.DataAnnotations;

    /// <summary>
    /// Layout index view model.
    /// </summary>
    public class LayoutIndexViewModel
    {
        /// <summary>
        /// Gets or sets layout ID.
        /// </summary>
        [Key]
        public Guid Id { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether is the default layout.
        /// </summary>
        [Display(Name = "Default website layout?")]
        public bool IsDefault { get; set; } = false;

        /// <summary>
        /// Gets or sets layout name.
        /// </summary>
        [Display(Name = "Layout Name")]
        [StringLength(128)]
        public string LayoutName { get; set; }

        /// <summary>
        /// Gets or sets layout notes.
        /// </summary>
        [Display(Name = "Notes")]
        [DataType(DataType.Html)]
        public string Notes { get; set; }
    }
}
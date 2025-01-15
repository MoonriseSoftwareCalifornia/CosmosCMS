// <copyright file="ReservedPath.cs" company="Moonrise Software, LLC">
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
    /// Reserved path.
    /// </summary>
    /// <remarks>
    /// A reserved path prevents a page from being named that conflicts with a path.
    /// </remarks>
    public class ReservedPath
    {
        /// <summary>
        /// Gets or sets row ID.
        /// </summary>
        [Key]
        [Display(Name = "Id")]
        public Guid Id { get; set; } = Guid.NewGuid();

        /// <summary>
        /// Gets or sets reserved Path.
        /// </summary>
        [Required(AllowEmptyStrings = false)]
        [Display(Name = "Reserved Path")]
        public string Path { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether is required by Cosmos.
        /// </summary>
        [Display(Name = "Required by Cosmos?")]
        public bool CosmosRequired { get; set; } = false;

        /// <summary>
        /// Gets or sets reason by protected.
        /// </summary>
        [Required(AllowEmptyStrings = false)]
        [Display(Name = "Notes")]
        public string Notes { get; set; }
    }
}

// <copyright file="Setting.cs" company="Cosmos Website Solutions, Inc.">
// Copyright (c) Cosmos Website Solutions, Inc.. All rights reserved.
// Licensed under the GNU Public License, Version 3.0 (https://www.gnu.org/licenses/gpl-3.0.html)
// See https://github.com/MoonriseSoftwareCalifornia/CosmosCMS
// for more information concerning the license and the contributors participating to this project.
// </copyright>

namespace Cosmos.Common.Data
{
    using System;
    using System.ComponentModel.DataAnnotations;

    /// <summary>
    /// Setting.
    /// </summary>
    public class Setting
    {
        /// <summary>
        /// Gets or sets setting ID.
        /// </summary>
        [Key]
        [Display(Name = "Id")]
        public Guid Id { get; set; } = Guid.NewGuid();

        /// <summary>
        /// Gets or sets setting group.
        /// </summary>
        [Required]
        [MinLength(1)]
        [Display(Name = "Group")]
        public string Group { get; set; }

        /// <summary>
        /// Gets or sets setting Name.
        /// </summary>
        [Required]
        [MinLength(1)]
        [Display(Name = "Name")]
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets setting value.
        /// </summary>
        [Required]
        [MinLength(1)]
        [Display(Name = "Value")]
        public string Value { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether setting value is required.
        /// </summary>
        [Required]
        [Display(Name = "Is Required")]
        public bool IsRequired { get; set; } = true;

        /// <summary>
        /// Gets or sets description of setting.
        /// </summary>
        [Required]
        [MinLength(1)]
        [Display(Name = "Description")]
        public string Description { get; set; }
    }
}

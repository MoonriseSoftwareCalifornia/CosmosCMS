// <copyright file="RoleItemViewModel.cs" company="Cosmos Website Solutions, Inc.">
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
    /// Role item view model.
    /// </summary>
    [Serializable]
    public class RoleItemViewModel
    {
        /// <summary>
        ///     Gets or sets role ID.
        /// </summary>
        [Key]
        [Display(Name = "Role ID")]
        public string Id { get; set; }

        /// <summary>
        ///     Gets or sets friendly role name.
        /// </summary>
        [Required(AllowEmptyStrings = false)]
        [Display(Name = "Role Name")]
        public string RoleName { get; set; }

        /// <summary>
        ///     Gets or sets role used to search on.
        /// </summary>
        [Display(Name = "Role Normalized Name")]
        public string RoleNormalizedName { get; set; }
    }
}
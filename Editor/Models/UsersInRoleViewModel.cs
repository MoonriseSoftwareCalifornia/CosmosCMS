// <copyright file="UsersInRoleViewModel.cs" company="Moonrise Software, LLC">
// Copyright (c) Moonrise Software, LLC. All rights reserved.
// Licensed under the GNU Public License, Version 3.0 (https://www.gnu.org/licenses/gpl-3.0.html)
// See https://github.com/MoonriseSoftwareCalifornia/CosmosCMS
// for more information concerning the license and the contributors participating to this project.
// </copyright>

namespace Sky.Cms.Models
{
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;

    /// <summary>
    /// Users in a Role.
    /// </summary>
    public class UsersInRoleViewModel
    {
        /// <summary>
        /// Gets or sets role Id.
        /// </summary>
        public string RoleId { get; set; }

        /// <summary>
        /// Gets or sets role Name.
        /// </summary>
        public string RoleName { get; set; }

        /// <summary>
        /// Gets or sets new user Ids.
        /// </summary>
        [Required(ErrorMessage = "Please select at least one.")]
        [Display(Name = "New Users for Role")]
        public string[] UserIds { get; set; }

        /// <summary>
        /// Gets or sets user list.
        /// </summary>
        public List<SelectedUserViewModel> Users { get; set; } = new List<SelectedUserViewModel>();
    }

    /// <summary>
    /// Selected user view model.
    /// </summary>
    public class SelectedUserViewModel
    {
        /// <summary>
        /// Gets or sets user Id.
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// Gets or sets user email address.
        /// </summary>
        public string Email { get; set; }
    }
}

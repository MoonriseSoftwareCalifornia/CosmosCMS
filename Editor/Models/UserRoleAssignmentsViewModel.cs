// <copyright file="UserRoleAssignmentsViewModel.cs" company="Moonrise Software, LLC">
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
    /// User role assignments view model.
    /// </summary>
    public class UserRoleAssignmentsViewModel
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="UserRoleAssignmentsViewModel"/> class.
        /// </summary>
        public UserRoleAssignmentsViewModel()
        {
            this.RoleIds = new List<string>();
        }

        /// <summary>
        /// Gets or sets user Id.
        /// </summary>
        [Display(Name = "User Id")]
        public string Id { get; set; }

        /// <summary>
        /// Gets or sets user Email Address.
        /// </summary>
        [Display(Name = "User Email Address")]
        public string Email { get; set; }

        /// <summary>
        /// Gets or sets role assignments for this user.
        /// </summary>
        [Display(Name = "Role Assignments")]
        public List<string> RoleIds { get; set; }
    }
}

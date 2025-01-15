// <copyright file="UserIndexViewModel.cs" company="Moonrise Software, LLC">
// Copyright (c) Moonrise Software, LLC. All rights reserved.
// Licensed under the GNU Public License, Version 3.0 (https://www.gnu.org/licenses/gpl-3.0.html)
// See https://github.com/MoonriseSoftwareCalifornia/CosmosCMS
// for more information concerning the license and the contributors participating to this project.
// </copyright>

using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Cosmos.Cms.Models
{
    /// <summary>
    /// Users index view model.
    /// </summary>
    public class UserIndexViewModel
    {
        /// <summary>
        /// Gets or sets unique user ID.
        /// </summary>
        [Key]
        public string UserId { get; set; }

        /// <summary>
        /// Gets or sets user's email address.
        /// </summary>
        [Display(Name = "Email Address")]
        [EmailAddress]
        public string EmailAddress { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether user's email address is confirmed.
        /// </summary>
        [Display(Name = "Email Confirmed")]
        public bool EmailConfirmed { get; set; }

        /// <summary>
        /// Gets or sets user's phone number (can be SMS).
        /// </summary>
        [Display(Name = "Telephone #")]
        [Phone]
        public string PhoneNumber { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether user's phone number (can be SMS).
        /// </summary>
        [Display(Name = "Phone Confirmed")]
        public bool PhoneNumberConfirmed { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether user has two factor authentication enabled.
        /// </summary>
        [Display(Name = "2FA Enabled")]
        public bool TwoFactorEnabled { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether account is locked out.
        /// </summary>
        [Display(Name = "LockedOut?")]
        public bool IsLockedOut { get; set; }

        /// <summary>
        /// Gets or sets role memebership.
        /// </summary>
        public List<string> RoleMembership { get; set; } = new List<string>();
    }
}

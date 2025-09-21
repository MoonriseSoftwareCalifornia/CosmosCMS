// <copyright file="UserCreateViewModel.cs" company="Moonrise Software, LLC">
// Copyright (c) Moonrise Software, LLC. All rights reserved.
// Licensed under the GNU Public License, Version 3.0 (https://www.gnu.org/licenses/gpl-3.0.html)
// See https://github.com/MoonriseSoftwareCalifornia/CosmosCMS
// for more information concerning the license and the contributors participating to this project.
// </copyright>

namespace Sky.Cms.Models
{
    using System.ComponentModel.DataAnnotations;

    /// <summary>
    /// Create user view model.
    /// </summary>
    public class UserCreateViewModel
    {
        /// <summary>
        /// Gets or sets user's email address.
        /// </summary>
        [Required]
        [EmailAddress]
        [Display(Name = "Email Address")]
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
        [Phone()]
        public string PhoneNumber { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether user's phone number (can be SMS).
        /// </summary>
        [Display(Name = "Phone Confirmed")]
        public bool PhoneNumberConfirmed { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether optionally generates a random password.
        /// </summary>
        [Display(Name = "Generate random password")]
        public bool GenerateRandomPassword { get; set; } = true;

        /// <summary>
        /// Gets or sets user password.
        /// </summary>
        [Display(Name = "Password (recommended to use random instead)")]
        [DataType(DataType.Password)]
        public string Password { get; set; }
    }
}

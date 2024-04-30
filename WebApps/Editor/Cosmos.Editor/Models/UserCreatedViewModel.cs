// <copyright file="UserCreatedViewModel.cs" company="Cosmos Website Solutions, Inc.">
// Copyright (c) Cosmos Website Solutions, Inc.. All rights reserved.
// Licensed under the GNU Public License, Version 3.0 (https://www.gnu.org/licenses/gpl-3.0.html)
// See https://github.com/MoonriseSoftwareCalifornia/CosmosCMS
// for more information concerning the license and the contributors participating to this project.
// </copyright>

namespace Cosmos.Cms.Models
{
    using System.ComponentModel.DataAnnotations;
    using Cosmos.EmailServices;

    /// <summary>
    /// User created view model.
    /// </summary>
    public class UserCreatedViewModel
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="UserCreatedViewModel"/> class.
        /// </summary>
        /// <param name="model">Create user view model.</param>
        /// <param name="sendResult">Send result.</param>
        public UserCreatedViewModel(UserCreateViewModel model, SendResult sendResult)
        {
            EmailAddress = model.EmailAddress;
            EmailConfirmed = model.EmailConfirmed;
            PhoneNumber = model.PhoneNumber;
            PhoneNumberConfirmed = model.PhoneNumberConfirmed;
            GenerateRandomPassword = model.GenerateRandomPassword;
            RevealPassword = model.Password;
            SendResult = sendResult;
        }

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
        [Phone()]
        [Display(Name = "Telephone #")]
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
        /// Gets or sets reveal password value.
        /// </summary>
        [Display(Name = "Password (recommended to use random instead)")]
        public string RevealPassword { get; set; }

        /// <summary>
        /// Gets or sets sendGrid response.
        /// </summary>
        [Display(Name = "Email send result")]
        public SendResult SendResult { get; set; }
    }
}

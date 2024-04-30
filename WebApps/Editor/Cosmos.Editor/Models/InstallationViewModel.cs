// <copyright file="InstallationViewModel.cs" company="Cosmos Website Solutions, Inc.">
// Copyright (c) Cosmos Website Solutions, Inc.. All rights reserved.
// Licensed under the GNU Public License, Version 3.0 (https://www.gnu.org/licenses/gpl-3.0.html)
// See https://github.com/MoonriseSoftwareCalifornia/CosmosCMS
// for more information concerning the license and the contributors participating to this project.
// </copyright>

namespace Cosmos.Cms.Models
{
    using System.ComponentModel.DataAnnotations;

    /// <summary>
    /// Setup controller index view model.
    /// </summary>
    public class InstallationViewModel
    {
        /// <summary>
        /// Gets or sets setup state we are in.
        /// </summary>
        public SetupState SetupState { get; set; }

        /// <summary>
        /// Gets or sets administrator email address.
        /// </summary>
        [Display(Name = "Administrator Email Address")]
        [EmailAddress]
        [DataType(DataType.EmailAddress)]
        public string AdminEmail { get; set; }

        /// <summary>
        /// Gets or sets password.
        /// </summary>
        [Display(Name = "Password")]
        [DataType(DataType.Password)]
        [Required(AllowEmptyStrings = false)]
        public string Password { get; set; }

        /// <summary>
        /// Gets or sets password confirmation.
        /// </summary>
        [Display(Name = "Confirm Password")]
        [DataType(DataType.Password)]
        [Compare("Password")]
        public string ConfirmPassword { get; set; }
    }

    /// <summary>
    /// Set.
    /// </summary>
    public enum SetupState
    {
        /// <summary>
        /// Setup administrator account.
        /// </summary>
        SetupAdmin,

        /// <summary>
        /// Database is installed but needed to apply missing migrations.
        /// </summary>
        Upgrade,

        /// <summary>
        /// Database is up to date.
        /// </summary>
        UpToDate
    }
}

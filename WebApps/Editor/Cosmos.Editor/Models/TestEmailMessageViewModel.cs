// <copyright file="EmailMessageViewModel.cs" company="Moonrise Software, LLC">
// Copyright (c) Moonrise Software, LLC. All rights reserved.
// Licensed under the GNU Public License, Version 3.0 (https://www.gnu.org/licenses/gpl-3.0.html)
// See https://github.com/MoonriseSoftwareCalifornia/CosmosCMS
// for more information concerning the license and the contributors participating to this project.
// </copyright>

using System.ComponentModel.DataAnnotations;

namespace Cosmos.Editor.Models
{
    /// <summary>
    /// Email message view model.
    /// </summary>
    public class TestEmailMessageViewModel
    {
        /// <summary>
        /// Gets or sets the to email address.
        /// </summary>
        [EmailAddress]
        public string To { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the Subject Email Address.
        /// </summary>
        public string Subject { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the body of the email.
        /// </summary>
        public string Body { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets if the Email was a success.
        /// </summary>
        public bool? Success { get; set; } = null;

        /// <summary>
        /// Gets or sets the error message.
        /// </summary>
        public string ErrorMessage { get; set; } = string.Empty;
    }
}

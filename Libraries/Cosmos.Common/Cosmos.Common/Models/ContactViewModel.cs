// <copyright file="ContactViewModel.cs" company="Moonrise Software, LLC">
// Copyright (c) Moonrise Software, LLC. All rights reserved.
// Licensed under the GNU Public License, Version 3.0 (https://www.gnu.org/licenses/gpl-3.0.html)
// See https://github.com/MoonriseSoftwareCalifornia/CosmosCMS
// for more information concerning the license and the contributors participating to this project.
// </copyright>

namespace Cosmos.Common.Models
{
    using System;
    using System.ComponentModel.DataAnnotations;

    /// <summary>
    /// Contact view model for editing and reporting.
    /// </summary>
    public class ContactViewModel
    {
        /// <summary>
        ///     Gets or sets unique article entity primary key number (not to be confused with article number).
        /// </summary>
        public Guid? Id { get; set; }

        /// <summary>
        /// Gets or sets customer name.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets a customer's email address.
        /// </summary>
        [Required(AllowEmptyStrings = false)]
        [EmailAddress]
        public string Email { get; set; }

        /// <summary>
        /// Gets or sets the customer's phone number.
        /// </summary>
        [Phone]
        public string Phone { get; set; }

        /// <summary>
        /// Gets or sets when this record was created.
        /// </summary>
        public DateTimeOffset? Created { get; set; }

        /// <summary>
        /// Gets or sets the record update date and time.
        /// </summary>
        public DateTimeOffset? Updated { get; set; }
    }
}

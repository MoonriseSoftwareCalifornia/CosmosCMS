// <copyright file="ContactsExportViewModel.cs" company="Cosmos Website Solutions, Inc.">
// Copyright (c) Cosmos Website Solutions, Inc.. All rights reserved.
// Licensed under the GNU Public License, Version 3.0 (https://www.gnu.org/licenses/gpl-3.0.html)
// See https://github.com/MoonriseSoftwareCalifornia/CosmosCMS
// for more information concerning the license and the contributors participating to this project.
// </copyright>

namespace Cosmos.Editor.Models
{
    using System;
    using System.ComponentModel.DataAnnotations;

    /// <summary>
    /// Contacts export view model.
    /// </summary>
    public class ContactsExportViewModel
    {
        /// <summary>
        ///     Gets or sets unique article entity primary key number (not to be confused with article number).
        /// </summary>
        [Key]
        public int Id { get; set; }

        /// <summary>
        /// Gets or sets customer first name.
        /// </summary>
        public string FirstName { get; set; }

        /// <summary>
        /// Gets or sets customer last name.
        /// </summary>
        public string LastName { get; set; }

        /// <summary>
        /// Gets or sets a customer's email address.
        /// </summary>
        [Required(AllowEmptyStrings = false)]
        public string Email { get; set; }

        /// <summary>
        /// Gets or sets the customer's phone number.
        /// </summary>
        [Required(AllowEmptyStrings = true)]
        public string Phone { get; set; }

        /// <summary>
        /// Gets or sets when this record was created.
        /// </summary>
        public DateTimeOffset Created { get; set; } = DateTimeOffset.UtcNow;

        /// <summary>
        /// Gets or sets the record update date and time.
        /// </summary>
        public DateTimeOffset Updated { get; set; } = DateTimeOffset.UtcNow;
    }
}

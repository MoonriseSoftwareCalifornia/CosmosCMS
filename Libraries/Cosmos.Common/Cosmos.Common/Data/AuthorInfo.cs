// <copyright file="AuthorInfo.cs" company="Cosmos Website Solutions, Inc.">
// Copyright (c) Cosmos Website Solutions, Inc.. All rights reserved.
// Licensed under the GNU Public License, Version 3.0 (https://www.gnu.org/licenses/gpl-3.0.html)
// See https://github.com/MoonriseSoftwareCalifornia/CosmosCMS
// for more information concerning the license and the contributors participating to this project.
// </copyright>

namespace Cosmos.Common.Data
{
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;

    /// <summary>
    /// Author Information.
    /// </summary>
    public class AuthorInfo
    {
        /// <summary>
        /// Gets or sets the user ID.
        /// </summary>
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        [Display(Name = "User Id")]
        public string UserId { get; set; }

        /// <summary>
        /// Gets or sets author name.
        /// </summary>
        [Display(Name = "Author Name")]
        public string AuthorName { get; set; }

        /// <summary>
        /// Gets or sets the author description.
        /// </summary>
        [Display(Name = "About the author")]
        public string AuthorDescription { get; set; }

        /// <summary>
        /// Gets or sets the user's Twitter handle.
        /// </summary>
        [Display(Name = "Twitter Handle")]
        public string TwitterHandle { get; set; }

        /// <summary>
        /// Gets or sets the user's Instagram link.
        /// </summary>
        [DataType(DataType.Url)]
        [Display(Name = "Instagram Link")]
        public string InstagramUrl { get; set; }

        /// <summary>
        /// Gets or sets the user's website URL.
        /// </summary>
        [DataType(DataType.Url)]
        [Display(Name = "Website")]
        public string Website { get; set; }

        /// <summary>
        /// Gets or sets the users' public Email address.
        /// </summary>
        [DataType(DataType.EmailAddress)]
        [Display(Name = "Public Email")]
        public string EmailAddress { get; set; }
    }
}

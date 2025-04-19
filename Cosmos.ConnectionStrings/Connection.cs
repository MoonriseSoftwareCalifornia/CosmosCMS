// <copyright file="ConnectionStringProvider.cs" company="Moonrise Software, LLC">
// Copyright (c) Moonrise Software, LLC. All rights reserved.
// Licensed under the GNU Public License, Version 3.0 (https://www.gnu.org/licenses/gpl-3.0.html)
// See https://github.com/MoonriseSoftwareCalifornia/CosmosCMS
// for more information concerning the license and the contributors participating to this project.
// </copyright>

namespace Cosmos.DynamicConfig
{
    using System.ComponentModel.DataAnnotations;

    public class Connection
    {
        [Key]
        [Display(Name = "ID")]
        public Guid Id { get; set; } = Guid.NewGuid();

        /// <summary>
        /// Gets or sets the editor domain name of the connection.
        /// </summary>
        [Display(Name = "Editor Domain Name")]
        public string DomainName { get; set; } = null!;

        /// <summary>
        /// Gets or sets the database connection string.
        /// </summary>
        [Required(AllowEmptyStrings = false)]
        [Display(Name = "Database Connection String")]
        public string DbConn { get; set; } = null!;

        /// <summary>
        /// Gets or sets the database name.
        /// </summary>
        [Required(AllowEmptyStrings = false)]
        [Display(Name = "Database Name")]
        public string DbName { get; set; } = "cosmoscms";

        /// <summary>
        /// Gets or sets the storage connection string.
        /// </summary>
        [Required(AllowEmptyStrings = false)]
        [Display(Name = "Storage Connection String")]
        public string StorageConn { get; set; } = null!;

        /// <summary>
        /// Gets or sets the customer name.
        /// </summary>
        [Display(Name = "Website Owner Name")]
        public string? Customer { get; set; } = null;

        /// <summary>
        /// Gets or sets the resrouce group where the customer's resources are kept.
        /// </summary>
        [Required(AllowEmptyStrings = false)]
        [Display(Name = "Customer Resource Group")]
        public string? ResourceGroup { get; set; } = null;

        /// <summary>
        /// Gets or sets the publisher mode.
        /// </summary>
        [AllowedValues("Static", "Decoupled", "Headless", "Hybrid", "Static-dynamic", "")]
        public string PublisherMode { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the website URL.
        /// </summary>
        [Url]
        [Required(AllowEmptyStrings = false)]
        [Display(Name = "Website URL")]
        public string WebsiteUrl { get; set; } = null!;
    }
}

// <copyright file="DuplicateViewModel.cs" company="Moonrise Software, LLC">
// Copyright (c) Moonrise Software, LLC. All rights reserved.
// Licensed under the GNU Public License, Version 3.0 (https://www.gnu.org/licenses/gpl-3.0.html)
// See https://github.com/MoonriseSoftwareCalifornia/CosmosCMS
// for more information concerning the license and the contributors participating to this project.
// </copyright>

namespace Cosmos.Cms.Models
{
    using System;
    using System.ComponentModel.DataAnnotations;

    /// <summary>
    /// Article duplication model.
    /// </summary>
    public class DuplicateViewModel
    {
        /// <summary>
        /// Gets or sets entity ID.
        /// </summary>
        [Required]
        public Guid Id { get; set; }

        /// <summary>
        /// Gets or sets iD of the Article.
        /// </summary>
        public int ArticleId { get; set; }

        /// <summary>
        /// Gets or sets articleVersion.
        /// </summary>
        public int ArticleVersion { get; set; }

        /// <summary>
        /// Gets or sets parent page (optional).
        /// </summary>
        [Display(Name = "Parent page:")]
        public string ParentPageTitle { get; set; }

        /// <summary>
        /// Gets or sets new page title.
        /// </summary>
        [Display(Name = "New page title:")]
        [Required(AllowEmptyStrings = false)]
        public string Title { get; set; }

        /// <summary>
        /// Gets or sets optional date/time when published.
        /// </summary>
        [Display(Name = "Published date/time:")]
        public DateTimeOffset? Published { get; set; }
    }
}

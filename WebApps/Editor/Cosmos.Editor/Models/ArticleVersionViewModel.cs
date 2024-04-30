// <copyright file="ArticleVersionViewModel.cs" company="Cosmos Website Solutions, Inc.">
// Copyright (c) Cosmos Website Solutions, Inc.. All rights reserved.
// Licensed under the GNU Public License, Version 3.0 (https://www.gnu.org/licenses/gpl-3.0.html)
// See https://github.com/MoonriseSoftwareCalifornia/CosmosCMS
// for more information concerning the license and the contributors participating to this project.
// </copyright>

namespace Cosmos.Cms.Models
{
    using System;
    using System.ComponentModel.DataAnnotations;

    /// <summary>
    /// Article version view model.
    /// </summary>
    public class ArticleVersionViewModel
    {
        /// <summary>
        /// Gets or sets article Item ID.
        /// </summary>
        [Display(Name = "Id")]
        public Guid Id { get; set; }

        /// <summary>
        /// Gets or sets published date and time.
        /// </summary>
        [Display(Name = "Published")]
        public DateTimeOffset? Published { get; set; }

        /// <summary>
        /// Gets or sets title.
        /// </summary>
        [Display(Name = "Title")]
        public string Title { get; set; }

        /// <summary>
        /// Gets or sets last updated date and time.
        /// </summary>
        [Display(Name = "Updated")]
        public DateTimeOffset Updated { get; set; }

        /// <summary>
        /// Gets or sets version number.
        /// </summary>
        [Display(Name = "No.")]
        public int VersionNumber { get; set; }

        /// <summary>
        /// Gets or sets expires date and time.
        /// </summary>
        [Display(Name = "Expires")]
        public DateTimeOffset? Expires { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether uses Live editor.
        /// </summary>
        [Display(Name = "Uses Live editor")]
        public bool UsesHtmlEditor { get; set; }

        /// <summary>
        /// Gets or sets last person to edit this page.
        /// </summary>
        [Display(Name = "Edited by")]
        public string UserId { get; set; }
    }
}
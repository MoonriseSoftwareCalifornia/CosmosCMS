// <copyright file="NewHomeViewModel.cs" company="Moonrise Software, LLC">
// Copyright (c) Moonrise Software, LLC. All rights reserved.
// Licensed under the GNU Public License, Version 3.0 (https://www.gnu.org/licenses/gpl-3.0.html)
// See https://github.com/MoonriseSoftwareCalifornia/CosmosCMS
// for more information concerning the license and the contributors participating to this project.
// </copyright>

namespace Sky.Cms.Models
{
    using System;
    using System.ComponentModel.DataAnnotations;

    /// <summary>
    /// New home page view model.
    /// </summary>
    public class NewHomeViewModel
    {
        /// <summary>
        /// Gets or sets article ID.
        /// </summary>
        [Key]
        [Display(Name = "Article Key")]
        public Guid Id { get; set; }

        /// <summary>
        /// Gets or sets article number.
        /// </summary>
        [Display(Name = "Article Number")]
        public int ArticleNumber { get; set; }

        /// <summary>
        /// Gets or sets article title.
        /// </summary>
        [Display(Name = "Page Title")]
        public string Title { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether indicates to make this the new home page.
        /// </summary>
        [Display(Name = "Make this the new home page")]
        public bool IsNewHomePage { get; set; }

        /// <summary>
        /// Gets or sets article URL path.
        /// </summary>
        [Display(Name = "URL Path")]
        public string UrlPath { get; set; }
    }
}
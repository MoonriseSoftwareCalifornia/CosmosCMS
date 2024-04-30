// <copyright file="CreatePageViewModel.cs" company="Cosmos Website Solutions, Inc.">
// Copyright (c) Cosmos Website Solutions, Inc.. All rights reserved.
// Licensed under the GNU Public License, Version 3.0 (https://www.gnu.org/licenses/gpl-3.0.html)
// See https://github.com/MoonriseSoftwareCalifornia/CosmosCMS
// for more information concerning the license and the contributors participating to this project.
// </copyright>

namespace Cosmos.Cms.Models
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using Cosmos.Cms.Data;
    using Microsoft.AspNetCore.Mvc.Rendering;

    /// <summary>
    /// Create page view model.
    /// </summary>
    public class CreatePageViewModel
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CreatePageViewModel"/> class.
        /// </summary>
        public CreatePageViewModel()
        {
            Templates = new List<SelectListItem>();
        }

        /// <summary>
        /// Gets or sets article Number.
        /// </summary>
        public int ArticleNumber { get; set; } = 0;

        /// <summary>
        /// Gets or sets page ID.
        /// </summary>
        [Key]
        public Guid Id { get; set; }

        /// <summary>
        /// Gets or sets page title.
        /// </summary>
        [ArticleTitleValidation]
        [Display(Name = "Page Title")]
        [Required(AllowEmptyStrings = false, ErrorMessage = "Pages must have a title.")]
        public string Title { get; set; }

        /// <summary>
        /// Gets or sets page template used.
        /// </summary>
        [Display(Name = "Page template (optional)")]
        public Guid? TemplateId { get; set; }

        /// <summary>
        /// Gets or sets template list.
        /// </summary>
        public List<SelectListItem> Templates { get; set; }
    }
}
// <copyright file="TemplateIndexViewModel.cs" company="Moonrise Software, LLC">
// Copyright (c) Moonrise Software, LLC. All rights reserved.
// Licensed under the GNU Public License, Version 3.0 (https://www.gnu.org/licenses/gpl-3.0.html)
// See https://github.com/MoonriseSoftwareCalifornia/CosmosCMS
// for more information concerning the license and the contributors participating to this project.
// </copyright>

using System;
using System.ComponentModel.DataAnnotations;

namespace Cosmos.Cms.Models
{
    /// <summary>
    /// Template index view model.
    /// </summary>
    public class TemplateIndexViewModel
    {
        /// <summary>
        /// Gets or sets unique ID.
        /// </summary>
        [Key]
        public Guid Id { get; set; }

        /// <summary>
        /// Gets or sets name of the associated layout.
        /// </summary>
        [Display(Name = "Associated Layout")]
        public string LayoutName { get; set; }

        /// <summary>
        /// Gets or sets template title.
        /// </summary>
        [Display(Name = "Template Title")]
        [StringLength(128)]
        [Required(AllowEmptyStrings = false)]
        public string Title { get; set; }

        /// <summary>
        /// Gets or sets description/Notes regarding this template.
        /// </summary>
        [Display(Name = "Description/Notes")]
        public string Description { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether template can use live editor.
        /// </summary>
        public bool UsesHtmlEditor { get; set; }
    }
}
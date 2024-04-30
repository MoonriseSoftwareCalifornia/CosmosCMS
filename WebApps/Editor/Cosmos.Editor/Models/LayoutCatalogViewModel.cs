// <copyright file="LayoutCatalogViewModel.cs" company="Cosmos Website Solutions, Inc.">
// Copyright (c) Cosmos Website Solutions, Inc.. All rights reserved.
// Licensed under the GNU Public License, Version 3.0 (https://www.gnu.org/licenses/gpl-3.0.html)
// See https://github.com/MoonriseSoftwareCalifornia/CosmosCMS
// for more information concerning the license and the contributors participating to this project.
// </copyright>

namespace Cosmos.Cms.Models
{
    using System.ComponentModel.DataAnnotations;

    /// <summary>
    /// Layout catalog view model.
    /// </summary>
    public class LayoutCatalogViewModel
    {
        /// <summary>
        /// Gets or sets layout ID.
        /// </summary>
        [Key]
        public string Id { get; set; }

        /// <summary>
        /// Gets or sets layout name.
        /// </summary>
        [Display(Name = "Layout Name")]
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets layout description.
        /// </summary>
        [Display(Name = "Description/Notes")]
        public string Description { get; set; }

        /// <summary>
        /// Gets or sets layout license.
        /// </summary>
        [Display(Name = "License")]
        public string License { get; set; }
    }
}

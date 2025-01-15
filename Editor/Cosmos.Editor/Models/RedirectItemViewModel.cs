// <copyright file="RedirectItemViewModel.cs" company="Moonrise Software, LLC">
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
    /// Redirect from and to URL item.
    /// </summary>
    public class RedirectItemViewModel
    {
        /// <summary>
        /// Gets or sets redirect ID.
        /// </summary>
        [Key]
        public Guid Id { get; set; }

        /// <summary>
        /// Gets or sets redirect from this URL (local to this web server).
        /// </summary>
        [RedirectUrl]
        [Required(AllowEmptyStrings = false)]
        [Display(Name = "Redirect from URL")]
        public string FromUrl { get; set; }

        /// <summary>
        /// Gets or sets redirect to this URL.
        /// </summary>
        [RedirectUrl]
        [Required(AllowEmptyStrings = false)]
        [Display(Name = "Redirect to URL")]
        public string ToUrl { get; set; }
    }
}

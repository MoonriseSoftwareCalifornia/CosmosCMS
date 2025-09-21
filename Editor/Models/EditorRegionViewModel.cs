// <copyright file="EditorRegionViewModel.cs" company="Moonrise Software, LLC">
// Copyright (c) Moonrise Software, LLC. All rights reserved.
// Licensed under the GNU Public License, Version 3.0 (https://www.gnu.org/licenses/gpl-3.0.html)
// See https://github.com/MoonriseSoftwareCalifornia/CosmosCMS
// for more information concerning the license and the contributors participating to this project.
// </copyright>

namespace Sky.Editor.Models
{
    using System.ComponentModel.DataAnnotations;

    /// <summary>
    /// Editor region view model.
    /// </summary>
    public class EditorRegionViewModel
    {
        /// <summary>
        ///  Gets or sets the article ID (not article number).
        /// </summary>
        [Required]
        public int ArticleNumber { get; set; }

        /// <summary>
        /// Gets or sets the Editor ID.
        /// </summary>
        [Required(AllowEmptyStrings = false)]
        public string EditorId { get; set; }

        /// <summary>
        /// Gets or sets the edited data within the region.
        /// </summary>
        [Required(AllowEmptyStrings = true)]
        public string Data { get; set; }
    }
}
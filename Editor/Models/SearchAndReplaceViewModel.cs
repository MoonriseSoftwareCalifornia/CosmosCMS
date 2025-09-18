// <copyright file="SearchAndReplaceViewModel.cs" company="Moonrise Software, LLC">
// Copyright (c) Moonrise Software, LLC. All rights reserved.
// Licensed under the GNU Public License, Version 3.0 (https://www.gnu.org/licenses/gpl-3.0.html)
// See https://github.com/MoonriseSoftwareCalifornia/CosmosCMS
// for more information concerning the license and the contributors participating to this project.
// </copyright>

namespace Sky.Cms.Models
{
    using System.ComponentModel.DataAnnotations;

    /// <summary>
    /// Search and replace mode.
    /// </summary>
    public class SearchAndReplaceViewModel
    {
        /// <summary>
        /// Gets or sets a value indicating whether include content in search and replace?.
        /// </summary>
        [Display(Name = "Include content in search and replace?")]
        public bool IncludeContent { get; set; }

        /// <summary>
        /// 
        /// </summary>
        [Display(Name = "Include title in search and replace?")]
        public bool IncludeTitle { get; set; }

        /// <summary>
        /// Gets or sets limit to article.
        /// </summary>
        [Display(Name = "Limit to article:")]
        public int? ArticleNumber { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether limit to only published articles?.
        /// </summary>
        [Display(Name = "Limit to published articles?")]
        public bool LimitToPublished { get; set; } = true;

        /// <summary>
        /// Gets or sets find:.
        /// </summary>
        [Display(Name = "Find:")]
        public string FindValue { get; set; }

        /// <summary>
        /// Gets or sets replace with:.
        /// </summary>
        [Display(Name = "Replace:")]
        public string ReplaceValue { get; set; }
    }
}

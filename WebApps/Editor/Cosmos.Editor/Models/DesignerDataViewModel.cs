// <copyright file="DesignerDataViewModel.cs" company="Moonrise Software, LLC">
// Copyright (c) Moonrise Software, LLC. All rights reserved.
// Licensed under the GNU Public License, Version 3.0 (https://www.gnu.org/licenses/gpl-3.0.html)
// See https://github.com/MoonriseSoftwareCalifornia/CosmosCMS
// for more information concerning the license and the contributors participating to this project.
// </copyright>

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Cosmos.Cms.Data;
using Cosmos.Editor.Models.GrapesJs;
using MailChimp.Net.Models;

namespace Cosmos.Editor.Models
{
    /// <summary>
    /// GrapesJs designer post data view model.
    /// </summary>
    public class DesignerDataViewModel
    {
        /// <summary>
        /// Gets or sets the ID.
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// Gets or sets title. 
        /// </summary>
        [MaxLength(80)]
        [StringLength(80)]
        [ArticleTitleValidation]
        public string Title { get; set; }

        /// <summary>
        /// Gets or sets the HTML content.
        /// </summary>
        public string HtmlContent { get; set; }

        /// <summary>
        /// Gets or sets the CSS content.
        /// </summary>
        public string CssContent { get; set; }

    }
}

// <copyright file="EditorViewModel.cs" company="Cosmos Website Solutions, Inc.">
// Copyright (c) Cosmos Website Solutions, Inc.. All rights reserved.
// Licensed under the GNU Public License, Version 3.0 (https://www.gnu.org/licenses/gpl-3.0.html)
// See https://github.com/MoonriseSoftwareCalifornia/CosmosCMS
// for more information concerning the license and the contributors participating to this project.
// </copyright>

namespace Cosmos.Cms.Models
{
    /// <summary>
    /// Editor view model.
    /// </summary>
    public class EditorViewModel
    {
        /// <summary>
        /// Gets or sets file name.
        /// </summary>
        public string FieldName { get; set; }

        /// <summary>
        /// Gets or sets hTML content.
        /// </summary>
        public string Html { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether edit mode.
        /// </summary>
        public bool EditModeOn { get; set; }
    }
}
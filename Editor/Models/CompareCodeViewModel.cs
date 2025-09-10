// <copyright file="CompareCodeViewModel.cs" company="Moonrise Software, LLC">
// Copyright (c) Moonrise Software, LLC. All rights reserved.
// Licensed under the GNU Public License, Version 3.0 (https://www.gnu.org/licenses/gpl-3.0.html)
// See https://github.com/MoonriseSoftwareCalifornia/CosmosCMS
// for more information concerning the license and the contributors participating to this project.
// </copyright>

namespace Cosmos.Cms.Models
{
    using System.Collections.Generic;
    using Cosmos.Cms.Models.Interfaces;
    using Cosmos.Common.Models;

    /// <summary>
    /// Compare code between two pages.
    /// </summary>
    public class CompareCodeViewModel : ICodeEditorViewModel
    {
        /// <summary>
        /// Gets or sets editing field.
        /// </summary>
        public string EditingField { get; set; }

        /// <summary>
        /// Gets or sets editor title (different than Article title).
        /// </summary>
        public string EditorTitle { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether content is valid and OK to save.
        /// </summary>
        public bool IsValid { get; set; }

        /// <summary>
        /// Gets or sets array of editor fields.
        /// </summary>
        public IEnumerable<EditorField> EditorFields { get; set; }

        /// <summary>
        /// Gets or sets array of custom buttons for this editor.
        /// </summary>
        public IEnumerable<string> CustomButtons { get; set; }

        /// <summary>
        /// Gets or sets editor type.
        /// </summary>
        public string EditorType { get; set; } = nameof(CompareCodeViewModel);

        /// <summary>
        /// Gets or sets articles to compare.
        /// </summary>
        public Cosmos.Common.Data.Layout[] Layouts { get; set; }

        /// <summary>
        /// Gets or sets articles to compare.
        /// </summary>
        public ArticleViewModel[] Articles { get; set; }
    }
}

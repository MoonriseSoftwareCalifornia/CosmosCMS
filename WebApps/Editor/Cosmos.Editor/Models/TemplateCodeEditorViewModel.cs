// <copyright file="TemplateCodeEditorViewModel.cs" company="Moonrise Software, LLC">
// Copyright (c) Moonrise Software, LLC. All rights reserved.
// Licensed under the GNU Public License, Version 3.0 (https://www.gnu.org/licenses/gpl-3.0.html)
// See https://github.com/MoonriseSoftwareCalifornia/CosmosCMS
// for more information concerning the license and the contributors participating to this project.
// </copyright>

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Cosmos.Cms.Models.Interfaces;

namespace Cosmos.Cms.Models
{
    /// <summary>
    /// Template code editor view model.
    /// </summary>
    public class TemplateCodeEditorViewModel : ICodeEditorViewModel
    {
        /// <summary>
        /// Gets or sets content.
        /// </summary>
        [DataType(DataType.Html)]
        public string Content { get; set; }

        /// <summary>
        /// Gets or sets version.
        /// </summary>
        public int Version { get; set; }

        /// <summary>
        /// Gets or sets unique ID of template.
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// Gets or sets editing field name.
        /// </summary>
        public string EditingField { get; set; }

        /// <summary>
        /// Gets or sets editor title.
        /// </summary>
        public string EditorTitle { get; set; }

        /// <summary>
        /// Gets or sets template title.
        /// </summary>
        [Required]
        [MinLength(1)]
        public string Title { get; set; }

        /// <summary>
        /// Gets or sets list of editor fields.
        /// </summary>
        public IEnumerable<EditorField> EditorFields { get; set; }

        /// <summary>
        /// Gets or sets custom buttons used by editor.
        /// </summary>
        public IEnumerable<string> CustomButtons { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether content is valid.
        /// </summary>
        public bool IsValid { get; set; }

        /// <summary>
        /// Gets or sets editor Type.
        /// </summary>
        public string EditorType { get; set; }

        /// <summary>
        /// Gets or sets editor field.
        /// </summary>
        IEnumerable<EditorField> ICodeEditorViewModel.EditorFields { get; set; }
    }
}
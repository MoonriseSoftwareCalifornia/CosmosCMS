// <copyright file="EditScriptPostModel.cs" company="Moonrise Software, LLC">
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
    /// Edit script post back model.
    /// </summary>
    public class EditScriptPostModel : ICodeEditorViewModel
    {
        /// <summary>
        /// Gets or sets article ID.
        /// </summary>
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        /// <summary>
        /// Gets or sets endpoint Name.
        /// </summary>
        [Required]
        [MaxLength(60)]
        [MinLength(1)]
        [RegularExpression("^[\\w-_.]*$", ErrorMessage = "Letters and numbers only.")]
        public string EndPoint { get; set; }

        /// <summary>
        /// Gets or sets roles that can execute this script (if applicable).
        /// </summary>
        public string RoleList { get; set; }

        /// <summary>
        /// Gets or sets title of endpoint.
        /// </summary>
        public string Title { get; set; }

        /// <summary>
        /// Gets or sets version number.
        /// </summary>
        public int Version { get; set; }

        /// <summary>
        /// Gets or sets input variables.
        /// </summary>
        [RegularExpression("^[a-zA-Z0-9,.-:]*$", ErrorMessage = "Letters and numbers only.")]
        public string InputVars { get; set; }

        /// <summary>
        /// Gets or sets input configuration.
        /// </summary>
        public string Config { get; set; }

        /// <summary>
        /// Gets or sets node JavaScript code.
        /// </summary>
        public string Code { get; set; }

        /// <summary>
        /// Gets or sets published date and time.
        /// </summary>
        public DateTimeOffset? Published { get; set; }

        /// <summary>
        /// Gets or sets editing field.
        /// </summary>
        public string EditingField { get; set; }

        /// <summary>
        /// Gets or sets editor title.
        /// </summary>
        public string EditorTitle { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether code is valid.
        /// </summary>
        public bool IsValid { get; set; }

        /// <summary>
        /// Gets or sets editor fields.
        /// </summary>
        public IEnumerable<EditorField> EditorFields { get; set; }

        /// <summary>
        /// Gets or sets custom buttons.
        /// </summary>
        public IEnumerable<string> CustomButtons { get; set; }

        /// <summary>
        /// Gets or sets editor type.
        /// </summary>
        public string EditorType { get; set; } = nameof(EditScriptPostModel);

        /// <summary>
        /// Gets or sets description of what this script does.
        /// </summary>
        [Required]
        [MinLength(2)]
        public string Description { get; set; }

        /// <summary>
        /// Gets or sets indicates if this is to be saved as a new version.
        /// </summary>
        public bool? SaveAsNewVersion { get; set; }
    }
}

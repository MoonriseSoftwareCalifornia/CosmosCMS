// <copyright file="LayoutCodeViewModel.cs" company="Cosmos Website Solutions, Inc.">
// Copyright (c) Cosmos Website Solutions, Inc.. All rights reserved.
// Licensed under the GNU Public License, Version 3.0 (https://www.gnu.org/licenses/gpl-3.0.html)
// See https://github.com/MoonriseSoftwareCalifornia/CosmosCMS
// for more information concerning the license and the contributors participating to this project.
// </copyright>

namespace Cosmos.Cms.Models
{
    using System;
    using System.Collections.Generic;
    using Cosmos.Cms.Models.Interfaces;

    /// <summary>
    /// Layout code view model.
    /// </summary>
    public class LayoutCodeViewModel : ICodeEditorViewModel
    {
        /// <summary>
        /// Gets or sets layout ID number.
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// Gets or sets hEAD content of the layout.
        /// </summary>
        public string Head { get; set; }

        /// <summary>
        /// Gets or sets hTML Header content.
        /// </summary>
        /// <remarks>Often is either a NAV or HEADER tag content, or both.</remarks>
        public string HtmlHeader { get; set; }

        /// <summary>
        /// Gets or sets body tag HTML attributes.
        /// </summary>
        public string BodyHtmlAttributes { get; set; }

        /// <summary>
        /// Gets or sets layout footer.
        /// </summary>
        public string FooterHtmlContent { get; set; }

        /// <summary>
        /// Gets or sets current edit field.
        /// </summary>
        public string EditingField { get; set; }

        /// <summary>
        /// Gets or sets editor title.
        /// </summary>
        public string EditorTitle { get; set; }

        /// <summary>
        /// Gets or sets list of editor fields.
        /// </summary>
        public IEnumerable<EditorField> EditorFields { get; set; }

        /// <summary>
        /// Gets or sets custom button list.
        /// </summary>
        public IEnumerable<string> CustomButtons { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether model is valid.
        /// </summary>
        public bool IsValid { get; set; }

        /// <summary>
        /// Gets or sets editor type.
        /// </summary>
        public string EditorType { get; set; } = nameof(LayoutCodeViewModel);
    }
}
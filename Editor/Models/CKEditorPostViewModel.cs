// <copyright file="CKEditorPostViewModel.cs" company="Moonrise Software, LLC">
// Copyright (c) Moonrise Software, LLC. All rights reserved.
// Licensed under the GNU Public License, Version 3.0 (https://www.gnu.org/licenses/gpl-3.0.html)
// See https://github.com/MoonriseSoftwareCalifornia/CosmosCMS
// for more information concerning the license and the contributors participating to this project.
// </copyright>

namespace Cosmos.Editor.Models
{
    using System;

    /// <summary>
    /// CKEditor post view model.
    /// </summary>
    public class CKEditorPostViewModel
    {
        /// <summary>
        /// Gets or sets article record ID.
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// Gets or sets edit ID as defined by the data-ccms-ceid attribute.
        /// </summary>
        public string EditorId { get; set; }

        /// <summary>
        /// Gets or sets user Id (Email address).
        /// </summary>
        public string UserId { get; set; }

        /// <summary>
        /// Gets or sets hTML data being sent back.
        /// </summary>
        public string Content { get; set; } = string.Empty;
    }
}

// <copyright file="SaveResultJsonModel.cs" company="Moonrise Software, LLC">
// Copyright (c) Moonrise Software, LLC. All rights reserved.
// Licensed under the GNU Public License, Version 3.0 (https://www.gnu.org/licenses/gpl-3.0.html)
// See https://github.com/MoonriseSoftwareCalifornia/CosmosCMS
// for more information concerning the license and the contributors participating to this project.
// </copyright>

namespace Cosmos.Cms.Models
{
    /// <summary>
    ///     JSON model returned have Live editor saves content.
    /// </summary>
    public class SaveResultJsonModel : SaveCodeResultJsonModel
    {
        /// <summary>
        /// Gets or sets a value indicating whether server indicates the content was saved successfully in the database.
        /// </summary>
        public bool ServerSideSuccess { get; set; }

        /// <summary>
        ///     Gets or sets content model as saved.
        /// </summary>
        public new HtmlEditorViewModel Model { get; set; }
    }
}
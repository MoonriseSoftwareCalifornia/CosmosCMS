// <copyright file="SaveCodeResultJsonModel.cs" company="Moonrise Software, LLC">
// Copyright (c) Moonrise Software, LLC. All rights reserved.
// Licensed under the GNU Public License, Version 3.0 (https://www.gnu.org/licenses/gpl-3.0.html)
// See https://github.com/MoonriseSoftwareCalifornia/CosmosCMS
// for more information concerning the license and the contributors participating to this project.
// </copyright>

using System.Collections.Generic;
using Azure.ResourceManager;
using Cosmos.Editor.Controllers;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Cosmos.Cms.Models
{
    /// <summary>
    ///     Code editor return result model.
    /// </summary>
    public class SaveCodeResultJsonModel
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SaveCodeResultJsonModel"/> class.
        ///     Constructor.
        /// </summary>
        public SaveCodeResultJsonModel()
        {
            Errors = new List<ModelStateEntry>();
        }

        /// <summary>
        ///     Gets or sets a value indicating whether form post is valid.
        /// </summary>
        public bool IsValid { get; set; }

        /// <summary>
        ///     Gets or sets error count.
        /// </summary>
        public int ErrorCount { get; set; }

        /// <summary>
        ///     Gets or sets a value indicating whether has stopped counting errors because it has reached its maximum.
        /// </summary>
        public bool HasReachedMaxErrors { get; set; }

        /// <summary>
        ///     Gets or sets model valiation state.
        /// </summary>
        public ModelValidationState ValidationState { get; set; }

        /// <summary>
        ///     Gets or sets errors in model state.
        /// </summary>
        public List<ModelStateEntry> Errors { get; set; }

        /// <summary>
        ///   Gets or sets updated model.
        /// </summary>
        public EditCodePostModel Model { get; set; } = null;

        /// <summary>
        /// Gets aRM Operation (present if CDN purged).
        /// </summary>
        public List<CdnResult> CdnResults { get; internal set; }
    }
}
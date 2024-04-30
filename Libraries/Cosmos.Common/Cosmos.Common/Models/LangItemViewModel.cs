// <copyright file="LangItemViewModel.cs" company="Cosmos Website Solutions, Inc.">
// Copyright (c) Cosmos Website Solutions, Inc.. All rights reserved.
// Licensed under the GNU Public License, Version 3.0 (https://www.gnu.org/licenses/gpl-3.0.html)
// See https://github.com/MoonriseSoftwareCalifornia/CosmosCMS
// for more information concerning the license and the contributors participating to this project.
// </copyright>

namespace Cosmos.Common.Models
{
    /// <summary>
    ///     Language list item.
    /// </summary>
    public class LangItemViewModel
    {
        /// <summary>
        ///     Gets or sets iSO code for language.
        /// </summary>
        public string LanguageCode { get; set; }

        /// <summary>
        ///     Gets or sets friendly name of language.
        /// </summary>
        public string DisplayName { get; set; }
    }
}
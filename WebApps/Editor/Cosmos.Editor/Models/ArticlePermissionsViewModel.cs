// <copyright file="ArticlePermissionsViewModel.cs" company="Cosmos Website Solutions, Inc.">
// Copyright (c) Cosmos Website Solutions, Inc.. All rights reserved.
// Licensed under the GNU Public License, Version 3.0 (https://www.gnu.org/licenses/gpl-3.0.html)
// See https://github.com/MoonriseSoftwareCalifornia/CosmosCMS
// for more information concerning the license and the contributors participating to this project.
// </copyright>

namespace Cosmos.Editor.Models
{
    using System;
    using Cosmos.Common.Data;

    /// <summary>
    /// Article permissions view model.
    /// </summary>
    public class ArticlePermissionsViewModel
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ArticlePermissionsViewModel"/> class.
        /// </summary>
        public ArticlePermissionsViewModel()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ArticlePermissionsViewModel"/> class.
        /// </summary>
        /// <param name="entry">Catalog entry.</param>
        /// <param name="forRoles">Updating roles?</param>
        public ArticlePermissionsViewModel(CatalogEntry entry, bool forRoles = true)
        {
            Title = entry.Title;
            Published = entry.Published;
            ShowingRoles = forRoles;
        }

        /// <summary>
        /// Gets or sets article number.
        /// </summary>
        public int ArticleNumber { get; set; }

        /// <summary>
        /// Gets or sets article title.
        /// </summary>
        public string Title { get; set; }

        /// <summary>
        /// Gets or sets date and time published.
        /// </summary>
        public DateTimeOffset? Published { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether permission set is for roles, otherwise is for users.
        /// </summary>
        public bool ShowingRoles { get; set; }
    }
}

// <copyright file="ArticlePermissionsViewModel.cs" company="Moonrise Software, LLC">
// Copyright (c) Moonrise Software, LLC. All rights reserved.
// Licensed under the GNU Public License, Version 3.0 (https://www.gnu.org/licenses/gpl-3.0.html)
// See https://github.com/MoonriseSoftwareCalifornia/CosmosCMS
// for more information concerning the license and the contributors participating to this project.
// </copyright>

namespace Cosmos.Editor.Models
{
    using System;
    using Cosmos.Common.Models;

    /// <summary>
    /// Article permissions view model.
    /// </summary>
    public class ArticlePermissionsViewModel
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        public ArticlePermissionsViewModel()
        {
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="article"></param>
        /// <param name="forRoles"></param>
        public ArticlePermissionsViewModel(ArticleViewModel article, bool forRoles = true)
        {
            Id = article.Id;
            Title = article.Title;
            Published = article.Published;
            ShowingRoles = forRoles;
        }

        /// <summary>
        /// Gets or sets specific article version we are setting the permissions for.
        /// </summary>
        public Guid Id { get; set; }

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

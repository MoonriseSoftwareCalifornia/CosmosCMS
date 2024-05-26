// <copyright file="ArticlePermisionItem.cs" company="Moonrise Software, LLC">
// Copyright (c) Moonrise Software, LLC. All rights reserved.
// Licensed under the GNU Public License, Version 3.0 (https://www.gnu.org/licenses/gpl-3.0.html)
// See https://github.com/MoonriseSoftwareCalifornia/CosmosCMS
// for more information concerning the license and the contributors participating to this project.
// </copyright>

namespace Cosmos.Editor.Models
{
    /// <summary>
    /// Article permission item.
    /// </summary>
    public class ArticlePermisionItem
    {
        /// <summary>
        /// Gets or sets role or user ID.
        /// </summary>
        public string IdentityObjectId { get; set; }

        /// <summary>
        /// Gets or sets role name or user email.
        /// </summary>
        public string Name { get; set; }
    }
}

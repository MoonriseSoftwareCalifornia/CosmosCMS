// <copyright file="ArticlePermission.cs" company="Moonrise Software, LLC">
// Copyright (c) Moonrise Software, LLC. All rights reserved.
// Licensed under the GNU Public License, Version 3.0 (https://www.gnu.org/licenses/gpl-3.0.html)
// See https://github.com/MoonriseSoftwareCalifornia/CosmosCMS
// for more information concerning the license and the contributors participating to this project.
// </copyright>

namespace Cosmos.Common.Data
{
    /// <summary>
    /// Article permission for a role or user.
    /// </summary>
    public class ArticlePermission
    {
        /// <summary>
        /// Gets or sets role or user ID.
        /// </summary>
        public string IdentityObjectId { get; set; }

        /// <summary>
        /// Gets or sets permission (Read or Upload).
        /// </summary>
        public string Permission { get; set; } = "Read";

        /// <summary>
        /// Gets or sets a value indicating whether gets or sets if this is a role permission object.
        /// </summary>
        public bool IsRoleObject { get; set; } = true;
    }
}

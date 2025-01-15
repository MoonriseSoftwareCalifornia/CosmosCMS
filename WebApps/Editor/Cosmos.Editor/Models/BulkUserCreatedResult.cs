// <copyright file="BulkUserCreatedResult.cs" company="Moonrise Software, LLC">
// Copyright (c) Moonrise Software, LLC. All rights reserved.
// Licensed under the GNU Public License, Version 3.0 (https://www.gnu.org/licenses/gpl-3.0.html)
// See https://github.com/MoonriseSoftwareCalifornia/CosmosCMS
// for more information concerning the license and the contributors participating to this project.
// </copyright>

using Cosmos.EmailServices;
using Microsoft.AspNetCore.Identity;

namespace Cosmos.Cms.Models
{
    /// <summary>
    /// Returns the result of creating a user with the bulk-create method.
    /// </summary>
    public class BulkUserCreatedResult
    {
        /// <inheritdoc/>
        public IdentityResult IdentityResult { get; set; }

        /// <inheritdoc/>
        public SendResult SendResult { get; set; }

        /// <inheritdoc/>
        public UserCreateViewModel UserCreateViewModel { get; internal set; }
    }
}

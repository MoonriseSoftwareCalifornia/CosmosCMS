// <copyright file="StatusCodeEnum.cs" company="Moonrise Software, LLC">
// Copyright (c) Moonrise Software, LLC. All rights reserved.
// Licensed under the GNU Public License, Version 3.0 (https://www.gnu.org/licenses/gpl-3.0.html)
// See https://github.com/MoonriseSoftwareCalifornia/CosmosCMS
// for more information concerning the license and the contributors participating to this project.
// </copyright>

namespace Cosmos.Common.Data.Logic
{
    /// <summary>
    ///     Article status code.
    /// </summary>
    public enum StatusCodeEnum
    {
        /// <summary>
        ///     Active, able to display if publish data given.
        /// </summary>
        Active = 0,

        /// <summary>
        ///     In active, can be displayed by logged in users
        /// </summary>
        Inactive = 1,

        /// <summary>
        ///     Considered removed, no one can display until status changes.
        /// </summary>
        Deleted = 2,

        /// <summary>
        ///     The article is a redirect.
        /// </summary>
        Redirect = 3
    }
}
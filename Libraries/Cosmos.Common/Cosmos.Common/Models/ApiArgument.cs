// <copyright file="ApiArgument.cs" company="Moonrise Software, LLC">
// Copyright (c) Moonrise Software, LLC. All rights reserved.
// Licensed under the GNU Public License, Version 3.0 (https://www.gnu.org/licenses/gpl-3.0.html)
// See https://github.com/MoonriseSoftwareCalifornia/CosmosCMS
// for more information concerning the license and the contributors participating to this project.
// </copyright>

namespace Cosmos.Common.Models
{
    /// <summary>
    /// GET or POST argument.
    /// </summary>
    public class ApiArgument
    {
        /// <summary>
        /// Gets or sets argument key.
        /// </summary>
        public string Key { get; set; }

        /// <summary>
        /// Gets or sets argment value.
        /// </summary>
        public string Value { get; set; }
    }
}

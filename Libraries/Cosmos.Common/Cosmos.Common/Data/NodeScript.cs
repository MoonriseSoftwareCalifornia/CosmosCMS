// <copyright file="NodeScript.cs" company="Moonrise Software, LLC">
// Copyright (c) Moonrise Software, LLC. All rights reserved.
// Licensed under the GNU Public License, Version 3.0 (https://www.gnu.org/licenses/gpl-3.0.html)
// See https://github.com/MoonriseSoftwareCalifornia/CosmosCMS
// for more information concerning the license and the contributors participating to this project.
// </copyright>

using Cosmos.Common.Data.Logic;
using System;
using System.ComponentModel.DataAnnotations;

namespace Cosmos.Common.Data
{
    /// <summary>
    /// Node script.
    /// </summary>
    public class NodeScript
    {
        /// <summary>
        /// Gets or sets script Id.
        /// </summary>
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        /// <summary>
        /// Gets or sets endpoint Name.
        /// </summary>
        public string EndPoint { get; set; }

        /// <summary>
        /// Gets or sets version number.
        /// </summary>
        public int Version { get; set; }

        /// <summary>
        /// Gets or sets published date and time.
        /// </summary>
        public DateTimeOffset? Published { get; set; }

        /// <summary>
        /// Gets or sets date and time updated.
        /// </summary>
        public DateTimeOffset Updated { get; set; } = DateTimeOffset.UtcNow;

        /// <summary>
        /// Gets or sets script expiration date and time.
        /// </summary>
        public DateTimeOffset? Expires { get; set; }

        /// <summary>
        ///     Gets or sets status of this script.
        /// </summary>
        /// <remarks>See <see cref="StatusCodeEnum" /> enum for code numbers.</remarks>
        public int StatusCode { get; set; } = 0;

        /// <summary>
        /// Gets or sets input variables.
        /// </summary>
        public string[] InputVars { get; set; }

        /// <summary>
        /// Gets or sets input configuration.
        /// </summary>
        public string Config { get; set; }

        /// <summary>
        /// Gets or sets node JavaScript code.
        /// </summary>
        [Required]
        [MinLength(1)]
        public string Code { get; set; }

        /// <summary>
        /// Gets or sets description of what this script does.
        /// </summary>
        [Required]
        [MinLength(1)]
        public string Description { get; set; }

        /// <summary>
        /// Gets or sets roles allowed to access this endpoint.
        /// </summary>
        public string[] Roles { get; set; } = null;
    }
}

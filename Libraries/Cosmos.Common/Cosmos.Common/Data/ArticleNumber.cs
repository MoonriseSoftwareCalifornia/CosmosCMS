// <copyright file="ArticleNumber.cs" company="Cosmos Website Solutions, Inc.">
// Copyright (c) Cosmos Website Solutions, Inc.. All rights reserved.
// Licensed under the GNU Public License, Version 3.0 (https://www.gnu.org/licenses/gpl-3.0.html)
// See https://github.com/MoonriseSoftwareCalifornia/CosmosCMS
// for more information concerning the license and the contributors participating to this project.
// </copyright>

namespace Cosmos.Common.Data
{
    using System;

    /// <summary>
    /// Tracks article numbers.
    /// </summary>
    public class ArticleNumber
    {
        /// <summary>
        /// Gets or sets record ID.
        /// </summary>
        public string Id { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// Gets or sets date and time number set.
        /// </summary>
        public DateTimeOffset SetDateTime { get; set; } = DateTimeOffset.Now;

        /// <summary>
        /// Gets or sets last Article Number.
        /// </summary>
        public int LastNumber { get; set; }
    }
}

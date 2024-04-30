// <copyright file="ArticleLogJsonModel.cs" company="Cosmos Website Solutions, Inc.">
// Copyright (c) Cosmos Website Solutions, Inc.. All rights reserved.
// Licensed under the GNU Public License, Version 3.0 (https://www.gnu.org/licenses/gpl-3.0.html)
// See https://github.com/MoonriseSoftwareCalifornia/CosmosCMS
// for more information concerning the license and the contributors participating to this project.
// </copyright>

namespace Cosmos.Cms.Models
{
    using System;
    using System.ComponentModel.DataAnnotations;

    /// <summary>
    /// Article log json model.
    /// </summary>
    public class ArticleLogJsonModel
    {
        /// <summary>
        /// Gets or sets id.
        /// </summary>
        [Key]
        public Guid Id { get; set; }

        /// <summary>
        /// Gets or sets activity notes and description.
        /// </summary>
        public string ActivityNotes { get; set; }

        /// <summary>
        ///     Gets or sets date and Time (UTC by default).
        /// </summary>
        public DateTimeOffset DateTimeStamp { get; set; }

        /// <summary>
        /// Gets or sets identity User Id.
        /// </summary>
        public string IdentityUserId { get; set; }
    }
}
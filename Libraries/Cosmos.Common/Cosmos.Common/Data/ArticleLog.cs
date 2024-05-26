// <copyright file="ArticleLog.cs" company="Moonrise Software, LLC">
// Copyright (c) Moonrise Software, LLC. All rights reserved.
// Licensed under the GNU Public License, Version 3.0 (https://www.gnu.org/licenses/gpl-3.0.html)
// See https://github.com/MoonriseSoftwareCalifornia/CosmosCMS
// for more information concerning the license and the contributors participating to this project.
// </copyright>

namespace Cosmos.Common.Data
{
    using System;
    using System.ComponentModel.DataAnnotations;

    /// <summary>
    ///     Article activity log entry.
    /// </summary>
    public class ArticleLog
    {
        /// <summary>
        ///     Gets or sets identity key of the entity.
        /// </summary>
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        /// <summary>
        ///     Gets or sets user ID of the person who triggered the activity.
        /// </summary>
        public string IdentityUserId { get; set; }

        /// <summary>
        ///     Gets or sets iD of the Article associated with this event.
        /// </summary>
        public Guid ArticleId { get; set; }

        /// <summary>
        /// Gets or sets title of the article.
        /// </summary>
        public string ArticleTitle { get; set; }

        /// <summary>
        ///     Gets or sets notes regarding what happened.
        /// </summary>
        public string ActivityNotes { get; set; }

        /// <summary>
        ///     Gets or sets date and Time (UTC by default).
        /// </summary>
        public DateTimeOffset DateTimeStamp { get; set; } = DateTimeOffset.UtcNow;
    }
}
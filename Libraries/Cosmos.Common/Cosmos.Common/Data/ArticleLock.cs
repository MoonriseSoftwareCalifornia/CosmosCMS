// <copyright file="ArticleLock.cs" company="Moonrise Software, LLC">
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
    /// Represents a lock on an article because it is being edited.
    /// </summary>
    public class ArticleLock
    {
        /// <summary>
        /// Gets or sets unique ID for this record.
        /// </summary>
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        /// <summary>
        /// Gets or sets unique SignalR Connection Id.
        /// </summary>
        public string ConnectionId { get; set; }

        /// <summary>
        /// Gets or sets user ID for this lock.
        /// </summary>
        public string UserEmail { get; set; }

        /// <summary>
        /// Gets or sets article RECORD ID for this lock (Not the Article ID).
        /// </summary>
        public Guid ArticleId { get; set; }

        /// <summary>
        /// Gets or sets when the lock was set.
        /// </summary>
        public DateTimeOffset LockSetDateTime { get; set; }

        /// <summary>
        /// Gets or sets editor type for this lock.
        /// </summary>
        public string EditorType { get; set; }

        /// <summary>
        /// Gets or sets file path for this lock (if applicable).
        /// </summary>
        public string FilePath { get; set; }
    }
}

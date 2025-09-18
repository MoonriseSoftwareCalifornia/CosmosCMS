// <copyright file="ArticleLockViewModel.cs" company="Moonrise Software, LLC">
// Copyright (c) Moonrise Software, LLC. All rights reserved.
// Licensed under the GNU Public License, Version 3.0 (https://www.gnu.org/licenses/gpl-3.0.html)
// See https://github.com/MoonriseSoftwareCalifornia/CosmosCMS
// for more information concerning the license and the contributors participating to this project.
// </copyright>

namespace Sky.Cms.Models
{
    using System;
    using System.ComponentModel.DataAnnotations;

    /// <summary>
    /// Represents a lock on an article because it is being edited.
    /// </summary>
    public class ArticleLockViewModel
    {
        /// <summary>
        /// Gets or sets unique ID of the lock.
        /// </summary>
        [Key]
        public Guid Id { get; set; } = Guid.Empty;

        /// <summary>
        /// Gets or sets user email for this lock.
        /// </summary>
        public string UserEmail { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets article Record ID for this lock.
        /// </summary>
        public Guid ArticleRecordId { get; set; }

        /// <summary>
        /// Gets or sets iD of the Signal R Connection with lock.
        /// </summary>
        public string ConnectionId { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets when the lock was set.
        /// </summary>
        public DateTimeOffset LockSetDateTime { get; set; } = DateTimeOffset.Now;

        /// <summary>
        /// Gets or sets view path for this lock.
        /// </summary>
        public string ViewPath { get; set; }

        /// <summary>
        /// Gets or sets editor type name.
        /// </summary>
        public string EditorType { get; set; }
    }
}

﻿// <copyright file="ArticleLogJsonModel.cs" company="Moonrise Software, LLC">
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
// <copyright file="LiveEditorSignal.cs" company="Moonrise Software, LLC">
// Copyright (c) Moonrise Software, LLC. All rights reserved.
// Licensed under the GNU Public License, Version 3.0 (https://www.gnu.org/licenses/gpl-3.0.html)
// See https://github.com/MoonriseSoftwareCalifornia/CosmosCMS
// for more information concerning the license and the contributors participating to this project.
// </copyright>

namespace Cosmos.Cms.Models
{
    using System;

    /// <summary>
    /// Live editor SignalR model.
    /// </summary>
    public class LiveEditorSignal
    {
        /// <summary>
        /// Gets or sets article record ID.
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// Gets or sets id of the Article entity being worked on.
        /// </summary>
        public int ArticleNumber { get; set; }

        /// <summary>
        /// Gets or sets edit ID as defined by the data-ccms-ceid attribute.
        /// </summary>
        public string EditorId { get; set; }

        /// <summary>
        /// Gets or sets user Id (Email address).
        /// </summary>
        public string UserId { get; set; }

        /// <summary>
        /// Gets or sets user position in document.
        /// </summary>
        public int Offset { get; set; }

        /// <summary>
        /// Gets or sets command.
        /// </summary>
        public string Command { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether is Focused.
        /// </summary>
        public bool IsFocused { get; set; }

        /// <summary>
        /// Gets or sets page version number.
        /// </summary>
        public int VersionNumber { get; set; }

        /// <summary>
        /// Gets or sets hTML data being sent back.
        /// </summary>
        public string Data { get; set; }

        /// <summary>
        /// Gets or sets date/time published.
        /// </summary>
        public DateTimeOffset? Published { get; set; }

        /// <summary>
        /// Gets or sets date/time updated.
        /// </summary>
        public DateTimeOffset? Updated { get; set; }

        /// <summary>
        /// Gets or sets article title.
        /// </summary>
        public string Title { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets uRL path.
        /// </summary>
        public string UrlPath { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets banner Image URL.
        /// </summary>
        public string BannerImage { get; set; }

        /// <summary>
        /// Gets or sets role access list.
        /// </summary>
        public string RoleList { get; set; }
    }
}

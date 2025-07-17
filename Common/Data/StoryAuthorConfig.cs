// <copyright file="WebsiteAuthor.cs" company="Moonrise Software, LLC">
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
    /// Represents a website author in the StoryDesk system.
    /// </summary>
    public class StoryAuthorConfig
    {
        /// <summary>
        /// Gets or sets the unique identifier for the website author.
        /// </summary>
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        /// <summary>
        /// Gets or sets the unique identifier for the connection associated with the website author.
        /// </summary>
        public Guid ConnectionId { get; set; }

        /// <summary>
        /// Gets or sets the name of the website domain.
        /// </summary>
        public string WebsiteDomain { get; set; }

        /// <summary>
        /// Gets or sets the email address of the website author.
        /// </summary>
        public string EmailAddress { get; set; }

        /// <summary>
        /// Gets or sets the path to the website author's profile or content.
        /// </summary>
        public string Path { get; set; }

        /// <summary>
        /// Gets or sets the unique identifier for the template associated with the website author.
        /// </summary>
        public Guid? TemplateId { get; set; }

        /// <summary>
        /// Gets or sets the name of the template associated with the website author.
        /// </summary>
        public string TemplateName { get; set; }
    }
}
// <copyright file="StatusIndexViewModel.cs" company="Cosmos Website Solutions, Inc.">
// Copyright (c) Cosmos Website Solutions, Inc.. All rights reserved.
// Licensed under the GNU Public License, Version 3.0 (https://www.gnu.org/licenses/gpl-3.0.html)
// See https://github.com/MoonriseSoftwareCalifornia/CosmosCMS
// for more information concerning the license and the contributors participating to this project.
// </copyright>

namespace Cosmos.Cms.Models
{
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;

    /// <summary>
    /// Status Index View Model.
    /// </summary>
    public class StatusIndexViewModel
    {
        /// <summary>
        /// Gets or sets resource statuses.
        /// </summary>
        public List<StatusIndexItem> Items { get; set; }
    }

    /// <summary>
    /// Resouce Item Status.
    /// </summary>
    public class StatusIndexItem
    {
        /// <summary>
        /// Gets or sets resource type.
        /// </summary>
        [Display(Name = "Resource Type")]
        public ResourceType ResourceType { get; set; }

        /// <summary>
        /// Gets or sets identifier of the resource.
        /// </summary>
        [Display(Name = "Identifier")]
        public string Identifier { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether resource is operational.
        /// </summary>
        [Display(Name = "Status")]
        public bool IsValid { get; set; }

        /// <summary>
        /// Gets or sets any messages, informational, error or otherwise.
        /// </summary>
        [Display(Name = "Messages")]
        public string Messages { get; set; }
    }

    /// <summary>
    /// Cloud Resource Type.
    /// </summary>
    public enum ResourceType
    {
        /// <summary>
        /// SQL Database
        /// </summary>
        SqlDatabase = 0,

        /// <summary>
        /// BLOB Storage
        /// </summary>
        BlobStorage = 1,

        /// <summary>
        /// Redis Cache
        /// </summary>
        RedisCache = 2,

        /// <summary>
        /// Content Distribution Network
        /// </summary>
        Cdn = 3,

        /// <summary>
        /// Cosmos editor
        /// </summary>
        CosmosEditor = 4
    }
}

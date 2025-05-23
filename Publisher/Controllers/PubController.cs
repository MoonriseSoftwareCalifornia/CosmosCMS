﻿// <copyright file="PubController.cs" company="Moonrise Software, LLC">
// Copyright (c) Moonrise Software, LLC. All rights reserved.
// Licensed under the GNU Public License, Version 3.0 (https://www.gnu.org/licenses/gpl-3.0.html)
// See https://github.com/MoonriseSoftwareCalifornia/CosmosCMS
// for more information concerning the license and the contributors participating to this project.
// </copyright>

using Cosmos.BlobService;
using Cosmos.Cms.Common.Services.Configurations;
using Cosmos.Common.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;

namespace Cosmos.Publisher.Controllers
{
    /// <summary>
    /// Secure file access controller and proxy.
    /// </summary>
    [AllowAnonymous]
    public class PubController : PubControllerBase
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PubController"/> class.
        /// </summary>
        /// <param name="options">Cosmos options.</param>
        /// <param name="dbContext">Database context.</param>
        /// <param name="storageContext">Storage context.</param>
        public PubController(IOptions<CosmosConfig> options, ApplicationDbContext dbContext, StorageContext storageContext)
            : base(dbContext, storageContext, options.Value.SiteSettings.CosmosRequiresAuthentication)
        {
        }
    }
}
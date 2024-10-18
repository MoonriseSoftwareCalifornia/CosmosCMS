// <copyright file="PubController.cs" company="Moonrise Software, LLC">
// Copyright (c) Moonrise Software, LLC. All rights reserved.
// Licensed under the GNU Public License, Version 3.0 (https://www.gnu.org/licenses/gpl-3.0.html)
// See https://github.com/MoonriseSoftwareCalifornia/CosmosCMS
// for more information concerning the license and the contributors participating to this project.
// </copyright>

namespace Cosmos.Publisher.B2C.Controllers
{
    using Cosmos.BlobService;
    using Cosmos.Cms.Common.Services.Configurations;
    using Cosmos.Common.Data;
    using Cosmos.Common.Data.Logic;
    using Cosmos.Common.Services.PowerBI;
    using Cosmos.Publisher.Controllers;
    using Microsoft.Extensions.Options;

    /// <summary>
    /// Secure file access controller and proxy.
    /// </summary>
    public class PubController : B2CBaseController
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="HomeController"/> class.
        /// </summary>
        /// <param name="logger">Logger.</param>
        /// <param name="articleLogic">Article logic.</param>
        /// <param name="options">Cosmos options.</param>
        /// <param name="dbContext">Database Context.</param>
        /// <param name="storageContext">Storage context.</param>
        /// <param name="powerBiTokenService">Service used to get tokens from Power BI.</param>
        /// <param name="configuration">Application configuration.</param>
        public PubController(
            ILogger<HomeController> logger,
            ArticleLogic articleLogic,
            IOptions<CosmosConfig> options,
            ApplicationDbContext dbContext,
            StorageContext storageContext,
            PowerBiTokenService powerBiTokenService,
            IConfiguration configuration)
            : base(articleLogic, dbContext, storageContext, logger, powerBiTokenService, configuration)
        {
        }
    }
}
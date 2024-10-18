// <copyright file="B2CBaseController.cs" company="Moonrise Software, LLC">
// Copyright (c) Moonrise Software, LLC. All rights reserved.
// Licensed under the GNU Public License, Version 3.0 (https://www.gnu.org/licenses/gpl-3.0.html)
// See https://github.com/MoonriseSoftwareCalifornia/CosmosCMS
// for more information concerning the license and the contributors participating to this project.
// </copyright>

namespace Cosmos.Publisher.B2C.Controllers
{
    using System.Configuration;
    using System.Security.Claims;
    using Cosmos.BlobService;
    using Cosmos.Common;
    using Cosmos.Common.Data;
    using Cosmos.Common.Data.Logic;
    using Cosmos.Common.Services.PowerBI;

    /// <summary>
    /// Base controller for B2C controllers.
    /// </summary>
    public class B2CBaseController : HomeControllerBase
    {
        private readonly IConfiguration configuration;

        /// <summary>
        /// Initializes a new instance of the <see cref="B2CBaseController"/> class.
        /// </summary>
        /// <param name="articleLogic">Article Logic</param>
        /// <param name="dbContext">Database Context.</param>
        /// <param name="storageContext">Storage Context.</param>
        /// <param name="logger">Log service.</param>
        /// <param name="powerBiTokenService">Power BI service.</param>
        /// <param name="configuration">Application configuration.</param>
        public B2CBaseController(ArticleLogic articleLogic, ApplicationDbContext dbContext, StorageContext storageContext, ILogger logger, PowerBiTokenService powerBiTokenService, IConfiguration configuration)
            : base(articleLogic, dbContext, storageContext, logger, powerBiTokenService)
        {
            this.configuration = configuration;
        }

        /// <summary>
        /// Gets the default Customer IDs from the configuration file.
        /// </summary>
        public Guid[] CustomerIdsFromConfig
        {
            get
            {
                var customerId = configuration.GetValue<Guid?>("CustomerId") ?? configuration.GetValue<Guid?>("CUSTOMERID");

                if (customerId.HasValue)
                {
                    return new Guid[] { customerId.Value };
                }

                throw new ConfigurationErrorsException("CustomerId is not set in the configuration file.");
            }
        }

        /// <summary>
        /// Gets the current logged in user ID.
        /// </summary>
        public Guid? UserId
        {
            get
            {
                if (User.Identity != null && User.Identity.IsAuthenticated)
                {
                    if (Guid.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var guid))
                    {
                        return guid;
                    }

                    return null;
                }

                return null;
            }
        }

        /// <summary>
        /// Gets the group IDs that this user is a member of.
        /// </summary>
        /// <returns>List of group names.</returns>
        public List<string> GroupMembershipIds()
        {
            return User.Claims.Where(t => t.Type == "group").Select(s => s.Value).ToList();
        }

        /// <summary>
        /// Determins if a user is a member of a group.
        /// </summary>
        /// <param name="groupName">ID of group in question.</param>
        /// <returns>True or false.</returns>
        public bool IsMemberOfGroup(string groupName)
        {
            if (groupName.Equals("anonymous", StringComparison.CurrentCultureIgnoreCase))
            {
                return true;
            }

            return User.Claims.Any(t => t.Type == "group" && t.Value == groupName);
        }
    }
}

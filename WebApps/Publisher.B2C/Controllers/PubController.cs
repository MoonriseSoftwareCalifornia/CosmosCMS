// <copyright file="PubController.cs" company="Moonrise Software, LLC">
// Copyright (c) Moonrise Software, LLC. All rights reserved.
// Licensed under the GNU Public License, Version 3.0 (https://www.gnu.org/licenses/gpl-3.0.html)
// See https://github.com/MoonriseSoftwareCalifornia/CosmosCMS
// for more information concerning the license and the contributors participating to this project.
// </copyright>

namespace Cosmos.Publisher.B2C.Controllers
{
    using System.Security.Claims;
    using Cosmos.BlobService;
    using Cosmos.Cms.Common.Services.Configurations;
    using Cosmos.Common;
    using Cosmos.Common.Data;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Extensions.Options;
    using Microsoft.Net.Http.Headers;

    /// <summary>
    /// Secure file access controller and proxy.
    /// </summary>
    [AllowAnonymous]
    public class PubController : ControllerBase
    {
        private readonly IConfiguration configuration;
        private readonly IOptions<CosmosConfig> options;
        private readonly ApplicationDbContext dbContext;
        private readonly StorageContext storageContext;
        private readonly string userGroupName;

        /// <summary>
        /// Initializes a new instance of the <see cref="PubController"/> class.
        /// Constructor.
        /// </summary>
        /// <param name="options">Cosmos options.</param>
        /// <param name="dbContext">Database context.</param>
        /// <param name="storageContext">Storage context.</param>
        /// <param name="configuration">Application configuration.</param>
        public PubController(IOptions<CosmosConfig> options, ApplicationDbContext dbContext, StorageContext storageContext, IConfiguration configuration)
        {
            this.options = options;
            this.dbContext = dbContext;
            this.storageContext = storageContext;
            this.configuration = configuration;
            userGroupName = configuration.GetValue<string>("UserGroups:User") !;
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

                return new Guid[] { };
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
        /// Gets a file and validates user authentication.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public async Task<IActionResult> Index()
        {
            // If authentication is required, check for user authentication.
            if (options.Value.SiteSettings.CosmosRequiresAuthentication)
            {
                // If the user is not logged in, have them login first.
                if (User.Identity == null || User.Identity?.IsAuthenticated == false)
                {
                    return Unauthorized();
                }

                // Check if user is a member of the group, if not alert that the person needs permission.
                var isMember = IsMemberOfGroup(userGroupName);

                if (!isMember)
                {
                    return Unauthorized();
                }

                // Gets a path to file.
                var path = HttpContext.Request.Path.ToString().ToLower();

                Response.Headers.Expires = DateTimeOffset.UtcNow.ToString("ddd, dd MMM yyyy HH:mm:ss 'GMT'");
            }

            var client = storageContext.GetAppendBlobClient(HttpContext.Request.Path);
            var properties = await client.GetPropertiesAsync();

            return File(fileStream: await client.OpenReadAsync(), contentType: properties.Value.ContentType, lastModified: properties.Value.LastModified, entityTag: new EntityTagHeaderValue(properties.Value.ETag.ToString()));
        }

        /// <summary>
        /// Determins if a user is a member of a group.
        /// </summary>
        /// <param name="groupName">ID of group in question.</param>
        /// <returns>True or false.</returns>
        private bool IsMemberOfGroup(string groupName)
        {
            if (groupName.Equals("anonymous", StringComparison.CurrentCultureIgnoreCase))
            {
                return true;
            }

            return User.Claims.Any(t => t.Type == "group" && t.Value == groupName);
        }
    }
}
// <copyright file="Cosmos_Admin_CdnController.cs" company="Moonrise Software, LLC">
// Copyright (c) Moonrise Software, LLC. All rights reserved.
// Licensed under the GNU Public License, Version 3.0 (https://www.gnu.org/licenses/gpl-3.0.html)
// See https://github.com/MoonriseSoftwareCalifornia/CosmosCMS
// for more information concerning the license and the contributors participating to this project.
// </copyright>

namespace Cosmos.Cms.Controllers
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Azure.ResourceManager;
    using Azure.ResourceManager.Cdn;
    using Azure.ResourceManager.Resources;
    using Cosmos.Cms.Common.Services.Configurations;
    using Cosmos.Cms.Models;
    using Cosmos.Cms.Services;
    using Cosmos.Common.Data;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Options;
    using Newtonsoft.Json;

    /// <summary>
    /// Cosmos Systems Administrator Controller.
    /// </summary>
    // [ResponseCache(NoStore = true)]
    [Authorize(Roles = "Administrators, Editors")]
    public class Cosmos_Admin_CdnController : Controller
    {
        private readonly ILogger<Cosmos_Admin_CdnController> logger;
        private readonly ApplicationDbContext dbContext;
        private readonly IOptions<CosmosConfig> options;
        private readonly AzureSubscription azureSubscription;

        /// <summary>
        /// CDN Service Name.
        /// </summary>
        public static string CDNSERVICENAME = "CDN";

        /// <summary>
        /// Initializes a new instance of the <see cref="Cosmos_Admin_CdnController"/> class.
        /// Constructor.
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="dbContext"></param>
        /// <param name="options"></param>
        /// <param name="azureSubscription"></param>
        public Cosmos_Admin_CdnController(ILogger<Cosmos_Admin_CdnController> logger,
           ApplicationDbContext dbContext,
           IOptions<CosmosConfig> options,
           AzureSubscription azureSubscription
        )
        {
            this.logger = logger;
            this.dbContext = dbContext;
            this.options = options;
            this.azureSubscription = azureSubscription;
        }

        /// <summary>
        /// Gets the CDN Integration Status.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public async Task<IActionResult> Index()
        {
            var model = await dbContext.Settings.FirstOrDefaultAsync(f => f.Name == CDNSERVICENAME);

            if (model == null)
            {
                return View(null);
            }

            return View(JsonConvert.DeserializeObject<AzureCdnEndpoint>(model.Value));
        }

        /// <summary>
        /// Disable CDN integration.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public async Task<IActionResult> DisableCdn()
        {
            var oldSetting = await dbContext.Settings.FirstOrDefaultAsync(f => f.Name == CDNSERVICENAME);

            if (oldSetting != null)
            {
                dbContext.Settings.Remove(oldSetting);
                await dbContext.SaveChangesAsync();
            }

            return RedirectToAction("Index");
        }

        /// <summary>
        /// CDN Selection.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        [HttpGet]
        public async Task<IActionResult> EnableCdn()
        {
            return View(await GetEndPoints());
        }

        private async Task<List<AzureCdnEndpoint>> GetEndPoints()
        {
            var model = new List<AzureCdnEndpoint>();

            try
            {
                var url = new Uri(options.Value.SiteSettings.PublisherUrl);
                ViewData["Publisher"] = url;

                // var sub = client.GetDefaultSubscriptionAsync();
                SubscriptionResource subscription = azureSubscription.Subscription;
                ResourceGroupCollection resourceGroups = subscription.GetResourceGroups();

                var data = resourceGroups.GetAllAsync();

                await foreach (var group in data)
                {
                    var profiles = group.GetProfiles();
                    foreach (var profile in profiles)
                    {
                        var endpoints = profile.GetCdnEndpoints();
                        foreach (var end in endpoints)
                        {
                            foreach (var origin in end.Data.Origins)
                            {
                                model.Add(new AzureCdnEndpoint()
                                {
                                    EndPointId = end.Id,
                                    CdnProfileName = profile.Data.Name,
                                    EndPointHostName = origin.HostName,
                                    EndpointName = end.Data.Name,
                                    ResourceGroupName = group.Data.Name,
                                    SkuName = profile.Data.SkuName.Value.ToString(),
                                    SubscriptionId = subscription.Id
                                });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex.Message, ex);
            }

            return model;
        }

        /// <summary>
        /// Enables CDN Integration.
        /// </summary>
        /// <param name="EndPointId"></param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        [HttpPost]
        public async Task<IActionResult> EnableCdn([FromForm] string EndPointId)
        {
            var endpoints = await GetEndPoints();

            var endpoint = endpoints.FirstOrDefault(f => f.EndPointId == EndPointId);

            if (endpoint == null)
            {
                return NotFound();
            }

            var oldSetting = await dbContext.Settings.FirstOrDefaultAsync(f => f.Name == CDNSERVICENAME);

            if (oldSetting != null)
            {
                dbContext.Settings.Remove(oldSetting);
                await dbContext.SaveChangesAsync();
            }

            var cdnSetting = new Setting()
            {
                Name = CDNSERVICENAME,
                Description = "CDN Integration. Type: " + endpoint.SkuName + ".",
                Group = "Performance",
                IsRequired = false,
                Value = JsonConvert.SerializeObject(endpoint)
            };

            dbContext.Settings.Add(cdnSetting);
            await dbContext.SaveChangesAsync();

            return RedirectToAction("Index");
        }
    }
}

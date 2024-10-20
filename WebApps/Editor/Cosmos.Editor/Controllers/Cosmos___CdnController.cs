﻿// <copyright file="Cosmos___CdnController.cs" company="Moonrise Software, LLC">
// Copyright (c) Moonrise Software, LLC. All rights reserved.
// Licensed under the GNU Public License, Version 3.0 (https://www.gnu.org/licenses/gpl-3.0.html)
// See https://github.com/MoonriseSoftwareCalifornia/CosmosCMS
// for more information concerning the license and the contributors participating to this project.
// </copyright>

namespace Cosmos.Editor.Controllers
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Azure;
    using Azure.Identity;
    using Azure.ResourceManager;
    using Azure.ResourceManager.Cdn;
    using Azure.ResourceManager.Cdn.Models;
    using Cosmos.Common.Data;
    using Cosmos.Editor.Services;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.EntityFrameworkCore;

    /// <summary>
    /// The CDN controller.
    /// </summary>
    [Authorize(Roles = "Administrators,Editors")]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Minor Code Smell", "S101:Types should be named in PascalCase", Justification = "The URL must be unique and not have a changes of conflicting with user authored web page URLs.")]
    public class Cosmos___CdnController : Controller
    {
        /// <summary>
        /// CDN group name constant.
        /// </summary>
        public static readonly string GROUPNAME = "CDN";

        private readonly ApplicationDbContext dbContext;

        /// <summary>
        /// Initializes a new instance of the <see cref="Cosmos___CdnController"/> class.
        /// </summary>
        /// <param name="dbContext">Sets the database context.</param>
        public Cosmos___CdnController(ApplicationDbContext dbContext)
        {
            this.dbContext = dbContext;
        }

        /// <summary>
        /// Gets a CDN configuration.
        /// </summary>
        /// <param name="dbContext">Database context.</param>
        /// <returns>AzureCdnEndpoint.</returns>
        public static async Task<List<Setting>> GetCdnConfiguration(ApplicationDbContext dbContext)
        {
            var settings = await dbContext.Settings.Where(f => f.Group == GROUPNAME).ToListAsync();

            if (settings.Any())
            {
                return settings;
            }

            return new List<Setting>();
        }

        /// <summary>
        /// GEts the indext page.
        /// </summary>
        /// <returns>IActionResult.</returns>
        public async Task<IActionResult> Index()
        {
            ViewData["Operation"] = null;

            var settings = await GetCdnConfiguration(dbContext);

            var model = new CdnService(settings);

            return View(model);
        }

        /// <summary>
        /// Updates the CDN configuration.
        /// </summary>
        /// <param name="config">The CDN end point configuration.</param>
        /// <returns>A <see cref="Task{TResult}"/> representing the result of the asynchronous operation.</returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Index(CdnService config)
        {
            if (!ModelState.IsValid)
            {
                return View(config);
            }

            // If any of the Azure CDN fields are set, then all of them must be set.
            if (!string.IsNullOrEmpty(config.ResourceGroup)
                || !string.IsNullOrEmpty(config.ProfileName)
                || !string.IsNullOrEmpty(config.EndPointName)
                || config.SubscriptionId.HasValue)
            {
                if (string.IsNullOrEmpty(config.ResourceGroup))
                {
                    ModelState.AddModelError("ResourceGroup", "ERROR: Resource Group is required.");
                }

                if (string.IsNullOrEmpty(config.ProfileName))
                {
                    ModelState.AddModelError("ProfileName", "ERROR: Profile Name is required.");
                }

                if (string.IsNullOrEmpty(config.EndPointName))
                {
                    ModelState.AddModelError("EndPointName", "ERROR: End Point Name is required.");
                }

                if (config.SubscriptionId == null)
                {
                    ModelState.AddModelError("SubscriptionId", "ERROR: Subscription Id is required.");
                }

                if (!ModelState.IsValid)
                {
                    return View(config);
                }
            }

            if (!string.IsNullOrEmpty(config.SucuriApiKey) || !string.IsNullOrEmpty(config.SucuriApiSecret))
            {
                if (string.IsNullOrEmpty(config.SucuriApiKey))
                {
                    ModelState.AddModelError("SucuriApiKey", "ERROR Sucuri API Key is required.");
                }

                if (string.IsNullOrEmpty(config.SucuriApiSecret))
                {
                    ModelState.AddModelError("SucuriApiSecret", "ERROR Sucuri API Secret is required.");
                }

                if (!ModelState.IsValid)
                {
                    return View(config);
                }
            }

            var settings = await GetCdnConfiguration(dbContext);

            var profileName = settings.Find(f => f.Name == "ProfileName");
            if (profileName == null)
            {
                profileName = new Setting { Group = GROUPNAME, Name = "ProfileName", Value = config.ProfileName };
                dbContext.Settings.Add(profileName);
            }
            else
            {
                profileName.Value = config.ProfileName;
            }

            var resourceGroupName = settings.Find(f => f.Name == "ResourceGroupName");
            if (resourceGroupName == null)
            {
                resourceGroupName = new Setting { Group = GROUPNAME, Name = "ResourceGroupName", Value = config.ResourceGroup };
                dbContext.Settings.Add(resourceGroupName);
            }
            else
            {
                resourceGroupName.Value = config.ResourceGroup;
            }

            var isFrontDoor = settings.Find(f => f.Name == "IsFrontDoor");
            if (isFrontDoor == null)
            {
                isFrontDoor = new Setting { Group = GROUPNAME, Name = "IsFrontDoor", Value = config.IsFrontDoor.ToString() };
                dbContext.Settings.Add(isFrontDoor);
            }
            else
            {
                isFrontDoor.Value = config.IsFrontDoor.ToString();
            }

            var subscriptionId = settings.Find(f => f.Name == "SubscriptionId");
            if (subscriptionId == null)
            {
                subscriptionId = new Setting { Group = GROUPNAME, Name = "SubscriptionId", Value = config.SubscriptionId.ToString() };
                dbContext.Settings.Add(subscriptionId);
            }
            else
            {
                subscriptionId.Value = config.SubscriptionId.ToString();
            }

            var endPointName = settings.Find(f => f.Name == "EndPointName");
            if (endPointName == null)
            {
                endPointName = new Setting { Group = GROUPNAME, Name = "EndPointName", Value = config.EndPointName };
                dbContext.Settings.Add(endPointName);
            }
            else
            {
                endPointName.Value = config.EndPointName;
            }

            var sucuriApiKey = settings.Find(f => f.Name == "SucuriApiKey");
            if (sucuriApiKey == null)
            {
                sucuriApiKey = new Setting
                {
                    Group = GROUPNAME,
                    Name = "SucuriApiKey",
                    Value = config.SucuriApiKey,
                };
                dbContext.Settings.Add(sucuriApiKey);
            }
            else
            {
                sucuriApiKey.Value = config.SucuriApiKey;
            }

            var sucuriApiSecret = settings.Find(f => f.Name == "SucuriApiSecret");
            if (sucuriApiSecret == null)
            {
                sucuriApiSecret = new Setting
                {
                    Group = GROUPNAME,
                    Name = "SucuriApiSecret",
                    Value = config.SucuriApiSecret,
                };
                dbContext.Settings.Add(sucuriApiSecret);
            }
            else
            {
                sucuriApiSecret.Value = config.SucuriApiSecret;
            }

            // Try making a connection to the CDN to validate the configuration.
            try
            {
                var operation = await TestConnection(config);
                ViewData["Operation"] = operation;

                // Save the changes to the database.
                await dbContext.SaveChangesAsync();
                return View(config);
            }
            catch (RequestFailedException ex)
            {
                ModelState.AddModelError(string.Empty, $"ERROR: {ex.ErrorCode}");
                return View(config);
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, $"ERROR: {ex.Message}");
                return View(config);
            }
        }

        /// <summary>
        /// Removes the CDN configuration.
        /// </summary>
        /// <returns>A <see cref="Task{TResult}"/> representing the result of the asynchronous operation.</returns>
        public async Task<IActionResult> Remove()
        {
            var cdnConfiguration = await dbContext.Settings.Where(f => f.Group == GROUPNAME).ToListAsync();

            if (cdnConfiguration.Any())
            {
                dbContext.Settings.RemoveRange(cdnConfiguration);
                await dbContext.SaveChangesAsync();
            }

            return RedirectToAction("Index");
        }

        private async Task<CdnResult> TestConnection(CdnService azureCdnConfig)
        {
            if (!azureCdnConfig.IsConfigured())
            {
                throw new InvalidOperationException("CDN configuration is not complete.");
            }

            if (azureCdnConfig.ConfiguredCdnTypes().Contains(CdnType.AzureFrontDoor))
            {
                ArmClient client = new ArmClient(new DefaultAzureCredential());

                var frontendEndpointResourceId = FrontDoorEndpointResource.CreateResourceIdentifier(
                    azureCdnConfig.SubscriptionId.ToString(),
                    azureCdnConfig.ResourceGroup,
                    azureCdnConfig.ProfileName,
                    azureCdnConfig.EndPointName);

                var frontDoor = client.GetFrontDoorEndpointResource(frontendEndpointResourceId);

                var t = await frontDoor.GetAsync();

                var purgeContent = new FrontDoorPurgeContent(new[] { "/" });
                purgeContent.Domains.Add(t.Value.Data.HostName);

                var operation = await frontDoor.PurgeContentAsync(WaitUntil.Started, purgeContent);

                return new CdnResult()
                {
                    Id = operation.Id,
                    IsSuccessStatusCode = operation.HasCompleted
                };
            }
            else if (azureCdnConfig.ConfiguredCdnTypes().Contains(CdnType.AzureCdn))
            {
                var cdnResource = CdnEndpointResource.CreateResourceIdentifier(
                           azureCdnConfig.SubscriptionId.ToString(),
                           azureCdnConfig.ResourceGroup,
                           azureCdnConfig.ProfileName,
                           azureCdnConfig.EndPointName);
                ArmClient client = new ArmClient(new DefaultAzureCredential());
                var cdnEndpoint = client.GetCdnEndpointResource(cdnResource);
                var purgeContent = new PurgeContent(new[] { "/" });
                var operation = await cdnEndpoint.PurgeContentAsync(WaitUntil.Started, purgeContent);
                return new CdnResult()
                {
                    Id = operation.Id,
                    IsSuccessStatusCode = operation.HasCompleted
                };
            }

            if (azureCdnConfig.ConfiguredCdnTypes().Contains(CdnType.Sucuri))
            {
                var sucuri = new SucuriCdnService(azureCdnConfig.SucuriApiKey, azureCdnConfig.SucuriApiSecret);

                _ = await sucuri.PurgeContentAsync(new[] { "/" });

                return new CdnResult()
                {
                    Id = Guid.NewGuid().ToString(),
                    IsSuccessStatusCode = true
                };
            }

            return null;
        }
    }
}

// <copyright file="Cosmos___SettingsController.cs" company="Moonrise Software, LLC">
// Copyright (c) Moonrise Software, LLC. All rights reserved.
// Licensed under the GNU Public License, Version 3.0 (https://www.gnu.org/licenses/gpl-3.0.html)
// See https://github.com/MoonriseSoftwareCalifornia/CosmosCMS
// for more information concerning the license and the contributors participating to this project.
// </copyright>

namespace Cosmos.Editor.Controllers
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;
    using Cosmos.Common.Data;
    using Cosmos.Editor.Data.Logic;
    using Cosmos.Editor.Models;
    using Cosmos.Editor.Services;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.Logging;

    /// <summary>
    /// The settings controller.
    /// </summary>
    [Authorize(Roles = "Administrators")]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Minor Code Smell", "S101:Types should be named in PascalCase", Justification = "The URL must be unique and not have a changes of conflicting with user authored web page URLs.")]
    public class Cosmos___SettingsController : Controller
    {
        /// <summary>
        /// CDN group name constant.
        /// </summary>
        public static readonly string CDNGROUPNAME = "CDN";
        /// <summary>
        /// Editor settings group name.
        /// </summary>
        public static readonly string EDITORSETGROUPNAME = "EDITORSETTINGS";

        private readonly ApplicationDbContext dbContext;
        private readonly ILogger<Cosmos___SettingsController> logger;
        private readonly IEditorSettings settings;

        /// <summary>
        /// Initializes a new instance of the <see cref="Cosmos___SettingsController"/> class.
        /// </summary>
        /// <param name="dbContext">Sets the database context.</param>
        /// <param name="logger">Log service.</param>
        /// <param name="settings">Editor settings.</param>
        public Cosmos___SettingsController(
            ApplicationDbContext dbContext,
            ILogger<Cosmos___SettingsController> logger,
            IEditorSettings settings)
        {
            this.dbContext = dbContext;
            this.logger = logger;
            this.settings = settings;
        }

        /// <summary>
        /// Gets a CDN configuration.
        /// </summary>
        /// <param name="dbContext">Database context.</param>
        /// <returns>AzureCdnEndpoint.</returns>
        public static async Task<List<Setting>> GetCdnConfiguration(ApplicationDbContext dbContext)
        {
            var settings = await dbContext.Settings.Where(f => f.Group == CDNGROUPNAME).ToListAsync();

            if (settings.Any())
            {
                return settings;
            }

            return new List<Setting>();
        }

        /// <summary>
        /// Gets the index page.
        /// </summary>
        /// <returns>IActionResult.</returns>
        public IActionResult Index()
        {
            if (this.settings.IsMultiTenantEditor)
            {
                var model = ((EditorSettings)this.settings).GetEditorConfig();
                model.IsMultiTenantEditor = true; // This is set by environment variables and cannot be changed.
                return View(model);
            }
            else
            {
                var model = new EditorConfig((EditorSettings)settings);
                model.IsMultiTenantEditor = false; // This is set by environment variables and cannot be changed.
                return View(model);
            }
        }

        /// <summary>
        /// Updates the index page.
        /// </summary>
        /// <param name="model">Editor config model.</param>
        /// <returns>IActionResult.</returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Index(EditorConfig model)
        {
            model.IsMultiTenantEditor = true; // This is set by environment variables and cannot be changed.
            if (!ModelState.IsValid || !this.settings.IsMultiTenantEditor)
            {
                return View(model);
            }

            // Check if mode is static website, and if so, set the blob URL.
            if (model.StaticWebPages)
            {
                model.BlobPublicUrl = "/";
            }

            var settings = await dbContext.Settings.FirstOrDefaultAsync(f => f.Group == EDITORSETGROUPNAME);
            if (settings == null)
            {
                settings = new Setting
                {
                    Group = EDITORSETGROUPNAME,
                    Name = "EditorSettings",
                    Value = Newtonsoft.Json.JsonConvert.SerializeObject(model),
                    Description = "Settings used by the Cosmos Editor",
                };
                dbContext.Settings.Add(settings);
            }
            else
            {
                settings.Value = Newtonsoft.Json.JsonConvert.SerializeObject(model);
            }

            // Save the changes to the database.
            await dbContext.SaveChangesAsync();

            return View(model);
        }

        /// <summary>
        /// Gets the CDN configuration page.
        /// </summary>
        /// <returns>IActionResult.</returns>
        public async Task<IActionResult> CDN()
        {
            ViewData["Operation"] = null;

            var settings = await GetCdnConfiguration(dbContext);

            var model = new CdnService(settings, logger, HttpContext);

            return View(model);
        }

        /// <summary>
        /// Updates the CDN configuration.
        /// </summary>
        /// <param name="config">The CDN end point configuration.</param>
        /// <returns>A <see cref="Task{TResult}"/> representing the result of the asynchronous operation.</returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CDN(CdnService config)
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
                profileName = new Setting { Group = CDNGROUPNAME, Name = "ProfileName", Value = config.ProfileName };
                dbContext.Settings.Add(profileName);
            }
            else
            {
                profileName.Value = config.ProfileName;
            }

            var resourceGroupName = settings.Find(f => f.Name == "ResourceGroupName");
            if (resourceGroupName == null)
            {
                resourceGroupName = new Setting { Group = CDNGROUPNAME, Name = "ResourceGroupName", Value = config.ResourceGroup };
                dbContext.Settings.Add(resourceGroupName);
            }
            else
            {
                resourceGroupName.Value = config.ResourceGroup;
            }

            var isFrontDoor = settings.Find(f => f.Name == "IsFrontDoor");
            if (isFrontDoor == null)
            {
                isFrontDoor = new Setting { Group = CDNGROUPNAME, Name = "IsFrontDoor", Value = config.IsFrontDoor.ToString() };
                dbContext.Settings.Add(isFrontDoor);
            }
            else
            {
                isFrontDoor.Value = config.IsFrontDoor.ToString();
            }

            var subscriptionId = settings.Find(f => f.Name == "SubscriptionId");
            if (subscriptionId == null)
            {
                subscriptionId = new Setting { Group = CDNGROUPNAME, Name = "SubscriptionId", Value = config.SubscriptionId.ToString() };
                dbContext.Settings.Add(subscriptionId);
            }
            else
            {
                subscriptionId.Value = config.SubscriptionId.ToString();
            }

            var endPointName = settings.Find(f => f.Name == "EndPointName");
            if (endPointName == null)
            {
                endPointName = new Setting { Group = CDNGROUPNAME, Name = "EndPointName", Value = config.EndPointName };
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
                    Group = CDNGROUPNAME,
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
                    Group = CDNGROUPNAME,
                    Name = "SucuriApiSecret",
                    Value = config.SucuriApiSecret,
                };
                dbContext.Settings.Add(sucuriApiSecret);
            }
            else
            {
                sucuriApiSecret.Value = config.SucuriApiSecret;
            }

            // Save the changes to the database.
            await dbContext.SaveChangesAsync();

            // Try making a connection to the CDN to validate the configuration.
            var message = string.Empty;

            var operation = await TestConnection();
            ViewData["Operation"] = operation;

            if (operation != null && operation.Response != null && operation.Response.ContentStream != null)
            {
                using var streamReader = new StreamReader(operation.Response.ContentStream);
                message = await streamReader.ReadToEndAsync();
            }

            logger.LogInformation(message);

            return View(config);
        }

        /// <summary>
        /// Removes the CDN configuration.
        /// </summary>
        /// <returns>A <see cref="Task{TResult}"/> representing the result of the asynchronous operation.</returns>
        public async Task<IActionResult> Remove()
        {
            var cdnConfiguration = await dbContext.Settings.Where(f => f.Group == CDNGROUPNAME).ToListAsync();

            if (cdnConfiguration.Any())
            {
                dbContext.Settings.RemoveRange(cdnConfiguration);
                await dbContext.SaveChangesAsync();
            }

            return RedirectToAction("Index");
        }

        private async Task<CdnResult> TestConnection()
        {
            var settings = await GetCdnConfiguration(dbContext);
            var cdnService = new CdnService(settings, logger, HttpContext);

            if (!cdnService.IsConfigured())
            {
                throw new InvalidOperationException("CDN configuration is not complete.");
            }

            var result = await cdnService.PurgeCdn(new List<string> { "/" });

            return result.FirstOrDefault();
        }
    }
}

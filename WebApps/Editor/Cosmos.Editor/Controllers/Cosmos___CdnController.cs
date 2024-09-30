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
    public class Cosmos___CdnController : Controller
    {
        private const string GROUPNAME = "AZURECDN";

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
        /// GEts the indext page.
        /// </summary>
        /// <returns>IActionResult</returns>
        public async Task<IActionResult> Index()
        {
            ViewData["Operation"] = null;

            var settings = await GetCdnConfiguration();

            var model = new AzureCdnConfig
            {
                ProfileName = settings.FirstOrDefault(f => f.Name == "ProfileName")?.Value,
                ResourceGroup = settings.FirstOrDefault(f => f.Name == "ResourceGroupName")?.Value,
                IsFrontDoor = bool.Parse(settings.FirstOrDefault(f => f.Name == "IsFrontDoor")?.Value ?? "false"),
                SubscriptionId = Guid.Parse(settings.FirstOrDefault(f => f.Name == "SubscriptionId")?.Value ?? Guid.Empty.ToString()),
                EndPointName = settings.FirstOrDefault(f => f.Name == "EndPointName")?.Value,
            };

            return View(model);
        }

        /// <summary>
        /// Updates the CDN configuration.
        /// </summary>
        /// <param name="config">The CDN end point configuration.</param>
        /// <returns>A <see cref="Task{TResult}"/> representing the result of the asynchronous operation.</returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Index(AzureCdnConfig config)
        {
            if (!ModelState.IsValid)
            {
                return View(config);
            }

            var settings = await GetCdnConfiguration();

            var profileName = settings.FirstOrDefault(f => f.Name == "ProfileName");
            if (profileName == null)
            {
                profileName = new Setting { Group = GROUPNAME, Name = "ProfileName", Value = config.ProfileName };
                dbContext.Settings.Add(profileName);
            }
            else
            {
                profileName.Value = config.ProfileName;
            }

            var resourceGroupName = settings.FirstOrDefault(f => f.Name == "ResourceGroupName");
            if (resourceGroupName == null)
            {
                resourceGroupName = new Setting { Group = GROUPNAME, Name = "ResourceGroupName", Value = config.ResourceGroup };
                dbContext.Settings.Add(resourceGroupName);
            }
            else
            {
                resourceGroupName.Value = config.ResourceGroup;
            }

            var isFrontDoor = settings.FirstOrDefault(f => f.Name == "IsFrontDoor");
            if (isFrontDoor == null)
            {
                isFrontDoor = new Setting { Group = GROUPNAME, Name = "IsFrontDoor", Value = config.IsFrontDoor.ToString() };
                dbContext.Settings.Add(isFrontDoor);
            }
            else
            {
                isFrontDoor.Value = config.IsFrontDoor.ToString();
            }

            var subscriptionId = settings.FirstOrDefault(f => f.Name == "SubscriptionId");
            if (subscriptionId == null)
            {
                subscriptionId = new Setting { Group = GROUPNAME, Name = "SubscriptionId", Value = config.SubscriptionId.ToString() };
                dbContext.Settings.Add(subscriptionId);
            }
            else
            {
                subscriptionId.Value = config.SubscriptionId.ToString();
            }

            var endPointName = settings.FirstOrDefault(f => f.Name == "EndPointName");
            if (endPointName == null)
            {
                endPointName = new Setting { Group = GROUPNAME, Name = "EndPointName", Value = config.EndPointName };
                dbContext.Settings.Add(endPointName);
            }
            else
            {
                endPointName.Value = config.EndPointName;
            }

            // Save the changes to the database.
            await dbContext.SaveChangesAsync();

            // Try making a connection to the CDN to validate the configuration.
            try
            {
                var operation = await TestConnection(config);
                ViewData["Operation"] = operation;
                return View(config);
            }
            catch (RequestFailedException ex)
            {
                ModelState.AddModelError("ProfileName", $"ERROR: {ex.ErrorCode}");
                return View(config);
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("ProfileName", $"ERROR: {ex.Message}");
                return View(config);
            }

            await dbContext.SaveChangesAsync();

            return View("Index", config);
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

        private async Task<ArmOperation> TestConnection(AzureCdnConfig azureCdnConfig)
        {
            var settings = await GetCdnConfiguration();



            if (!azureCdnConfig.IsConfigured())
            {
                throw new InvalidOperationException("CDN configuration is not complete.");
            }

            ArmClient client = new ArmClient(new DefaultAzureCredential());

            if (azureCdnConfig.IsFrontDoor)
            {
                var frontendEndpointResourceId = FrontDoorEndpointResource.CreateResourceIdentifier(
                    azureCdnConfig.SubscriptionId.ToString(),
                    azureCdnConfig.ResourceGroup,
                    azureCdnConfig.ProfileName,
                    azureCdnConfig.EndPointName);

                var frontDoor = client.GetFrontDoorEndpointResource(frontendEndpointResourceId);
                var purgeContent = new FrontDoorPurgeContent(new[] { "/" });

                var operation = await frontDoor.PurgeContentAsync(WaitUntil.Started, purgeContent);

                return operation;
            }
            else
            {
                var cdnResource = CdnEndpointResource.CreateResourceIdentifier(
                       azureCdnConfig.SubscriptionId.ToString(),
                       azureCdnConfig.ResourceGroup,
                       azureCdnConfig.ProfileName,
                       azureCdnConfig.EndPointName);

                var cdnEndpoint = client.GetCdnEndpointResource(cdnResource);

                var purgeContent = new PurgeContent(new[] { "/" });

                var operation = await cdnEndpoint.PurgeContentAsync(WaitUntil.Started, purgeContent);

                return operation;
            }
        }

        /// <summary>
        /// Gets a CDN configuration.
        /// </summary>
        /// <returns>AzureCdnEndpoint.</returns>
        private async Task<List<Setting>> GetCdnConfiguration()
        {
            var settings = await dbContext.Settings.Where(f => f.Group == GROUPNAME).ToListAsync();

            if (settings.Any())
            {
                return settings;
            }

            return new List<Setting>();
        }
    }
}

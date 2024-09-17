namespace Cosmos.Editor.Controllers
{
    using System;
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
            return View(await GetCdnConfiguration());
        }

        /// <summary>
        /// Updates the CDN configuration.
        /// </summary>
        /// <param name="cdnEndpoint">The CDN end point configuration.</param>
        /// <returns>A <see cref="Task{TResult}"/> representing the result of the asynchronous operation.</returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Index(AzureCdnConfig cdnEndpoint)
        {
            if (!ModelState.IsValid)
            {
                return View(cdnEndpoint);
            }

            var cdnConfiguration = await GetCdnConfiguration();

            if (cdnConfiguration == null)
            {
                return NotFound();
            }

            cdnConfiguration.ProfileName = cdnEndpoint.ProfileName;
            cdnConfiguration.EndPointName = cdnEndpoint.EndPointName;
            cdnConfiguration.ResourceGroup = cdnEndpoint.ResourceGroup;
            cdnConfiguration.IsFrontDoor = cdnEndpoint.IsFrontDoor;
            cdnConfiguration.SubscriptionId = cdnEndpoint.SubscriptionId;
            cdnConfiguration.EndPointName = cdnEndpoint.EndPointName;

            // Try making a connection to the CDN to validate the configuration.
            var operation = await TestConnection(cdnConfiguration);

            var result = await operation.WaitForCompletionResponseAsync();

            if (result.IsError)
            {
                ModelState.AddModelError("EndPointName", result.ReasonPhrase);
                return View(cdnEndpoint);
            }

            await dbContext.SaveChangesAsync();

            ViewData["Operation"] = operation;

            return View("Index", cdnEndpoint);
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

        /// <summary>
        /// Tests the CDN connection.
        /// </summary>
        /// <returns>A <see cref="Task{Response}"/> representing the result of the asynchronous operation.</returns>
        public async Task<IActionResult> Test()
        {
            var cdnConfiguration = await GetCdnConfiguration();

            if (cdnConfiguration == null)
            {
                return NotFound();
            }

            var operation = await TestConnection(cdnConfiguration);

            var result = await operation.WaitForCompletionResponseAsync();

            return Json(result);
        }

        private async Task<ArmOperation> TestConnection(AzureCdnConfig azureCdnConfig)
        {
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
        private async Task<AzureCdnConfig> GetCdnConfiguration()
        {
            var cdnConfiguration = await dbContext.Settings.Where(f => f.Group == GROUPNAME).ToListAsync();

            if (!cdnConfiguration.Any())
            {
                return new Cosmos.Editor.Services.AzureCdnConfig();
            }

            return new Cosmos.Editor.Services.AzureCdnConfig()
            {
                ProfileName = cdnConfiguration.FirstOrDefault(f => f.Name == "ProfileName").Value,
                ResourceGroup = cdnConfiguration.FirstOrDefault(f => f.Name == "ResourceGroupName").Value,
                IsFrontDoor = bool.Parse(cdnConfiguration.FirstOrDefault(f => f.Name == "IsFrontDoor").Value),
                SubscriptionId = Guid.Parse(cdnConfiguration.FirstOrDefault(f => f.Name == "SubscriptionId").Value),
                EndPointName = cdnConfiguration.FirstOrDefault(f => f.Name == "EndPointName").Value
            };
        }
    }
}

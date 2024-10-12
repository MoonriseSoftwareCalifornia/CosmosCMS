// <copyright file="AzureCdnConfig.cs" company="Moonrise Software, LLC">
// Copyright (c) Moonrise Software, LLC. All rights reserved.
// Licensed under the GNU Public License, Version 3.0 (https://www.gnu.org/licenses/gpl-3.0.html)
// See https://github.com/MoonriseSoftwareCalifornia/CosmosCMS
// for more information concerning the license and the contributors participating to this project.
// </copyright>

namespace Cosmos.Editor.Services
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.Linq;
    using System.Net;
    using System.Threading.Tasks;
    using Azure;
    using Azure.Identity;
    using Azure.ResourceManager;
    using Azure.ResourceManager.Cdn;
    using Azure.ResourceManager.Cdn.Models;
    using Cosmos.Common.Data;
    using Cosmos.Editor.Controllers;

    /// <summary>
    ///     Configuration for Azure Front Door, Edgio or Microsoft CDN.
    /// </summary>
    public class CdnService
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CdnService"/> class.
        /// </summary>
        public CdnService() 
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CdnService"/> class.
        /// </summary>
        /// <param name="settings">CDN Settings.</param>
        public CdnService(List<Setting> settings)
        {
            var guidValue = settings.Find(f => f.Name == "SubscriptionId");

            Guid? subscriptionId = null;
            if (guidValue == null)
            {
                subscriptionId = null;
            }
            else
            {
                if (Guid.TryParse(guidValue.Value, out var test))
                {
                    subscriptionId = test;
                }
            }

            ProfileName = settings.Find(f => f.Name == "ProfileName")?.Value;
            ResourceGroup = settings.Find(f => f.Name == "ResourceGroupName")?.Value;
            IsFrontDoor = bool.Parse(settings.Find(f => f.Name == "IsFrontDoor")?.Value ?? "false");
            SubscriptionId = subscriptionId;
            EndPointName = settings.Find(f => f.Name == "EndPointName")?.Value;
            SucuriApiKey = settings.Find(f => f.Name == "SucuriApiKey")?.Value;
            SucuriApiSecret = settings.Find(f => f.Name == "SucuriApiSecret")?.Value;
        }

        /// <summary>
        ///     Gets or sets end Point Name.
        /// </summary>
        [Display(Name = "End Point Name")]
        public string EndPointName { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets a value indicating whether this is Azure frontdoor.
        /// </summary>
        [Display(Name = "Is Azure Front Door?")]
        public bool IsFrontDoor { get; set; } = false;

        /// <summary>
        /// Gets or sets the CDN profile name.
        /// </summary>
        [Display(Name = "Profile Name")]
        public string ProfileName { get; set; } = string.Empty;

        /// <summary>
        ///     Gets or sets azure Resource Group.
        /// </summary>
        [Display(Name = "Resource Group Name")]
        public string ResourceGroup { get; set; } = string.Empty;

        /// <summary>
        ///     Gets or sets subscription Id.
        /// </summary>
        [Display(Name = "Subscription Id")]
        public Guid? SubscriptionId { get; set; } = null;

        /// <summary>
        /// Gets or sets the Sucuri firewall/CDN key.
        /// </summary>
        [Display(Name = "Sucuri API key")]
        public string SucuriApiKey { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the Sucuri firewall/CDN secret.
        /// </summary>
        [Display(Name = "Sucuri API secret")]
        public string SucuriApiSecret { get; set; } = string.Empty;

        /// <summary>
        /// Indicates if CDN integration is configured.
        /// </summary>
        /// <returns>If true then a CDN or Front Door integration is configured.</returns>
        public bool IsConfigured()
        {
            return (ProfileName != string.Empty
                && SubscriptionId != null
                && ResourceGroup != string.Empty
                && EndPointName != string.Empty) || (SucuriApiKey != string.Empty && SucuriApiSecret != string.Empty);
        }

        /// <summary>
        /// Checks to see if a particular CDN type is configured.
        /// </summary>
        /// <param name="type">CDN type to check for.</param>
        /// <returns>True of false.</returns>
        public bool IsConfigured(CdnType type)
        {
            return ConfiguredCdnTypes().Contains(type);
        }

        /// <summary>
        /// Gets the configured CDN types.
        /// </summary>
        /// <returns>Type list.</returns>
        public List<CdnType> ConfiguredCdnTypes()
        {
            var cdnTypes = new List<CdnType>();

            if (IsFrontDoor)
            {
                cdnTypes.Add(CdnType.AzureFrontDoor);
            }
            else if (ProfileName != string.Empty
                && SubscriptionId != null
                && ResourceGroup != string.Empty
                && EndPointName != string.Empty)
            {
                cdnTypes.Add(CdnType.AzureCdn);
            }
            else if (SucuriApiKey != string.Empty && SucuriApiSecret != string.Empty)
            {
                cdnTypes.Add(CdnType.Sucuri);
            }
            else
            {
                cdnTypes.Add(CdnType.AzureCdn);
            }

            return cdnTypes;
        }

        /// <summary>
        /// Purges the CDN (or Front Door) if either is configured.
        /// </summary>
        /// <param name="purgeUrls">Purge URL Paths.</param>
        /// <returns>ArmOperation results.</returns>
        public async Task<List<CdnResult>> PurgeCdn(List<string> purgeUrls)
        {
            var results = new List<CdnResult>();

            purgeUrls = purgeUrls.Distinct().Select(s => s.Trim('/')).Select(s => s.Equals("root") ? "/" : "/" + s).ToList();

            // Check for Azure Frontdoor, if available use that.
            if (IsConfigured(CdnType.AzureFrontDoor))
            {
                ArmClient client = new ArmClient(new DefaultAzureCredential());

                var frontendEndpointResourceId = FrontDoorEndpointResource.CreateResourceIdentifier(
                    SubscriptionId.ToString(),
                    ResourceGroup,
                    ProfileName,
                    EndPointName);

                var frontDoor = client.GetFrontDoorEndpointResource(frontendEndpointResourceId);

                var purgeContent = new FrontDoorPurgeContent(purgeUrls);

                var result = await frontDoor.PurgeContentAsync(WaitUntil.Started, purgeContent);

                var response = result.GetRawResponse();
                results.Add(new CdnResult
                {
                    Status = (HttpStatusCode)response.Status,
                    ReasonPhrase = response.ReasonPhrase,
                    IsSuccessStatusCode = !response.IsError,
                    ClientRequestId = response.ClientRequestId,
                    Id = Guid.NewGuid().ToString(),
                });
            }
            else if (IsConfigured(CdnType.AzureCdn))
            {
                ArmClient client = new ArmClient(new DefaultAzureCredential());

                var cdnResource = CdnEndpointResource.CreateResourceIdentifier(
                    SubscriptionId.ToString(),
                    ResourceGroup,
                    ProfileName,
                    EndPointName);

                var cdnEndpoint = client.GetCdnEndpointResource(cdnResource);

                ArmOperation operation = null;

                if (purgeUrls.Count > 100 || purgeUrls.Any(p => p.Equals("/") || p.Equals("/*")))
                {
                    operation = await cdnEndpoint.PurgeContentAsync(Azure.WaitUntil.Started, new PurgeContent(new string[] { "/*" }));
                }
                else
                {
                    // 100 paths or less, no need to page or use wildcard
                    var purgeContent = new PurgeContent(purgeUrls);
                    operation = await cdnEndpoint.PurgeContentAsync(WaitUntil.Started, purgeContent);
                }

                var response = operation.GetRawResponse();
                results.Add(new CdnResult
                {
                    Status = (HttpStatusCode)response.Status,
                    ReasonPhrase = response.ReasonPhrase,
                    IsSuccessStatusCode = !response.IsError,
                    ClientRequestId = response.ClientRequestId,
                    Id = Guid.NewGuid().ToString(),
                });
            }

            if (IsConfigured(CdnType.Sucuri))
            {
                var sucuriService = new SucuriCdnService(SucuriApiKey, SucuriApiSecret);
                results.AddRange(await sucuriService.PurgeContentAsync(purgeUrls.ToArray()));
            }

            return results;
        }
    }

    /// <summary>
    /// Type pof CDN to use.
    /// </summary>
    public enum CdnType
    {
        /// <summary>
        /// Azure Front Door.
        /// </summary>
        AzureFrontDoor,

        /// <summary>
        /// Azure CDN (Edgio or Microsoft).
        /// </summary>
        AzureCdn,

        /// <summary>
        /// Sucuri firewall/CDN.
        /// </summary>
        Sucuri
    }
}
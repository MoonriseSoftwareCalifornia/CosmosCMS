// <copyright file="PowerBiTokenService.cs" company="Moonrise Software, LLC">
// Copyright (c) Moonrise Software, LLC. All rights reserved.
// Licensed under the GNU Public License, Version 3.0 (https://www.gnu.org/licenses/gpl-3.0.html)
// See https://github.com/MoonriseSoftwareCalifornia/CosmosCMS
// for more information concerning the license and the contributors participating to this project.
// </copyright>

namespace Cosmos.Common.Services.PowerBI
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Runtime.InteropServices;
    using System.Threading.Tasks;
    using Microsoft.Extensions.Options;
    using Microsoft.Identity.Client;
    using Microsoft.PowerBI.Api;
    using Microsoft.PowerBI.Api.Models;
    using Microsoft.Rest;

    /// <summary>
    /// Power BI Token service.
    /// </summary>
    public class PowerBiTokenService
    {
        private readonly IConfidentialClientApplication clientApp;
        private readonly string[] scopeBase;
        private readonly string powerBiApiUrl = "https://api.powerbi.com";

        /// <summary>
        /// Initializes a new instance of the <see cref="PowerBiTokenService"/> class.
        /// </summary>
        /// <param name="options">Token service options.</param>
        public PowerBiTokenService(IOptions<PowerBiAuth> options)
        {
            IsConfigured = !string.IsNullOrEmpty(options.Value.TenantId)
                && !string.IsNullOrEmpty(options.Value.ClientId)
                && !string.IsNullOrEmpty(options.Value.ClientSecret)
                && options.Value.ScopeBase.Any();

            if (IsConfigured)
            {
                // Create a confidential client to authorize the app with the AAD app
                clientApp = ConfidentialClientApplicationBuilder.Create(options.Value.ClientId)
                                                                .WithClientSecret(options.Value.ClientSecret)
                                                                .WithAuthority($"https://login.microsoftonline.com/{options.Value.TenantId}/")
                                                                .Build();
                scopeBase = options.Value.ScopeBase;
            }
        }

        /// <summary>
        /// Gets a value indicating whether the Power BI Token Service is configured.
        /// </summary>
        public bool IsConfigured
        {
            get;
            private set;
        }

        /// <summary>
        /// Get embed parameters.
        /// </summary>
        /// <param name="workspaceId">Workspace ID.</param>
        /// <param name="reportId">Report ID.</param>
        /// <param name="additionalDatasetId">Additional dataset id (optional).</param>
        /// <returns><see cref="EmbedParams"/>.</returns>
        public async Task<EmbedParams> GetEmbedParams(Guid workspaceId, Guid reportId, [Optional] Guid additionalDatasetId)
        {
            PowerBIClient pbiClient = await GetPowerBIClient();

            // Get report info
            var pbiReport = pbiClient.Reports.GetReportInGroup(workspaceId, reportId);

            // Check if dataset is present for the corresponding report.
            // If isRDLReport is true then it is a RDL Report.
            var isRDLReport = string.IsNullOrEmpty(pbiReport.DatasetId);

            EmbedToken embedToken;

            // Generate embed token for RDL report if dataset is not present
            if (isRDLReport)
            {
                // Get Embed token for RDL Report
                embedToken = await GetEmbedTokenForRDLReport(workspaceId, reportId);
            }
            else
            {
                // Create list of datasets
                var datasetIds = new List<Guid>();

                // Add dataset associated to the report
                datasetIds.Add(Guid.Parse(pbiReport.DatasetId));

                // Append additional dataset to the list to achieve dynamic binding later
                if (additionalDatasetId != Guid.Empty)
                {
                    datasetIds.Add(additionalDatasetId);
                }

                // Get Embed token multiple resources
                embedToken = await GetEmbedToken(reportId, datasetIds, workspaceId);
            }

            // Add report data for embedding
            var embedReports = new List<EmbedReport>()
            {
                new ()
                {
                    ReportId = pbiReport.Id, ReportName = pbiReport.Name, EmbedUrl = pbiReport.EmbedUrl
                }
            };

            // Capture embed params
            var embedParams = new EmbedParams
            {
                EmbedReport = embedReports,
                Type = "Report",
                EmbedToken = embedToken
            };

            return embedParams;
        }

        /// <summary>
        /// Get Embed token for single report, multiple datasets, and an optional target workspace.
        /// </summary>
        /// <param name="reportId">Report ID.</param>
        /// <param name="datasetIds">List of dataset IDs.</param>
        /// <param name="targetWorkspaceId">Target Power BI workspace.</param>
        /// <returns>Embed token.</returns>
        /// <remarks>This function is not supported for RDL Report.</remarks>
        public async Task<EmbedToken> GetEmbedToken(Guid reportId, IList<Guid> datasetIds = null, Guid? targetWorkspaceId = null)
        {
            PowerBIClient pbiClient = await GetPowerBIClient();

            // Create a request for getting Embed token.
            // This method works only with new Power BI V2 workspace experience.
            var tokenRequest = new GenerateTokenRequestV2(
                reports: new List<GenerateTokenRequestV2Report>() { new (reportId) },
                datasets: datasetIds != null ? datasetIds.Select(datasetId => new GenerateTokenRequestV2Dataset(datasetId.ToString())).ToList() : null,
                targetWorkspaces: targetWorkspaceId != null && targetWorkspaceId != Guid.Empty ? new List<GenerateTokenRequestV2TargetWorkspace>() { new (targetWorkspaceId.Value) } : null);

            // Generate Embed token.
            var embedToken = pbiClient.EmbedToken.GenerateToken(tokenRequest);

            return new EmbedToken()
            {
                Expiration = embedToken.Expiration,
                Token = embedToken.Token,
                TokenId = embedToken.TokenId,
            };
        }

        /// <summary>
        /// Get Embed token for multiple reports, datasets, and an optional target workspace.
        /// </summary>
        /// <param name="reportIds">Report ID list for which to get a token.</param>
        /// <param name="datasetIds">List of dataset IDs.</param>
        /// <param name="targetWorkspaceId">Target Power BI workspace.</param>
        /// <returns>Embed token.</returns>
        /// <remarks>This function is not supported for RDL Report.</remarks>
        public async Task<EmbedToken> GetEmbedToken(IList<Guid> reportIds, IList<Guid> datasetIds, [Optional] Guid targetWorkspaceId)
        {
            PowerBIClient pbiClient = await GetPowerBIClient();

            // Convert report Ids to required types
            var reports = reportIds.Select(reportId => new GenerateTokenRequestV2Report(reportId)).ToList();

            // Convert dataset Ids to required types
            var datasets = datasetIds.Select(datasetId => new GenerateTokenRequestV2Dataset(datasetId.ToString())).ToList();

            // Create a request for getting Embed token.
            // This method works only with new Power BI V2 workspace experience.
            var tokenRequest = new GenerateTokenRequestV2(datasets: datasets, reports: reports, targetWorkspaces: targetWorkspaceId != Guid.Empty ? new List<GenerateTokenRequestV2TargetWorkspace>() { new (targetWorkspaceId) } : null);

            // Generate Embed token
            var embedToken = pbiClient.EmbedToken.GenerateToken(tokenRequest);

            return new EmbedToken()
            {
                Expiration = embedToken.Expiration,
                Token = embedToken.Token,
                TokenId = embedToken.TokenId,
            };
        }

        /// <summary>
        /// Get Embed token for multiple reports, datasets, and optional target workspaces.
        /// </summary>
        /// <param name="reportIds">Report ID list for which to get a token.</param>
        /// <param name="datasetIds">List of dataset IDs.</param>
        /// <param name="targetWorkspaceIds">A list of Power BI workspaces.</param>
        /// <returns>Embed token.</returns>
        /// <remarks>This function is not supported for RDL Report.</remarks>
        public async Task<EmbedToken> GetEmbedToken(IList<Guid> reportIds, IList<Guid> datasetIds, [Optional] IList<Guid> targetWorkspaceIds)
        {
            // Note: This method is an example and is not consumed in this sample app
            PowerBIClient pbiClient = await GetPowerBIClient();

            // Convert report Ids to required types
            var reports = reportIds.Select(reportId => new GenerateTokenRequestV2Report(reportId)).ToList();

            // Convert dataset Ids to required types
            var datasets = datasetIds.Select(datasetId => new GenerateTokenRequestV2Dataset(datasetId.ToString())).ToList();

            // Convert target workspace Ids to required types
            IList<GenerateTokenRequestV2TargetWorkspace> targetWorkspaces = null;
            if (targetWorkspaceIds != null)
            {
                targetWorkspaces = targetWorkspaceIds.Select(targetWorkspaceId => new GenerateTokenRequestV2TargetWorkspace(targetWorkspaceId)).ToList();
            }

            // Create a request for getting Embed token.
            // This method works only with new Power BI V2 workspace experience.
            var tokenRequest = new GenerateTokenRequestV2(datasets, reports, targetWorkspaceIds != null ? targetWorkspaces : null);

            // Generate Embed token
            var embedToken = pbiClient.EmbedToken.GenerateToken(tokenRequest);

            return new EmbedToken()
            {
                Expiration = embedToken.Expiration,
                Token = embedToken.Token,
                TokenId = embedToken.TokenId,
            };
        }

        /// <summary>
        /// Get Embed token for RDL Report.
        /// </summary>
        /// <param name="targetWorkspaceId">Power BI Workspace ID.</param>
        /// <param name="reportId">Power BI Report ID.</param>
        /// <param name="accessLevel">Report access level ('view' is default).</param>
        /// <returns>Embed token.</returns>
        public async Task<EmbedToken> GetEmbedTokenForRDLReport(Guid targetWorkspaceId, Guid reportId, string accessLevel = "view")
        {
            PowerBIClient pbiClient = await GetPowerBIClient();

            // Generate token request for RDL Report
            var generateTokenRequestParameters = new GenerateTokenRequest(accessLevel: accessLevel);

            // Generate Embed token
            var embedToken = pbiClient.Reports.GenerateTokenInGroup(targetWorkspaceId, reportId, generateTokenRequestParameters);

            return new EmbedToken()
            {
                Expiration = embedToken.Expiration,
                Token = embedToken.Token,
                TokenId = embedToken.TokenId,
            };
        }

        /// <summary>
        /// Generates and returns Entra ID application access token.
        /// </summary>
        /// <returns>Entra ID application access token.</returns>
        private async Task<string> GetAppAccessToken()
        {
            if (IsConfigured)
            {
                // Service Principal auth is the recommended by Microsoft to achieve App Owns Data Power BI embedding.
                // Make a client call if Access token is not available in cache.
                var authenticationResult = await clientApp.AcquireTokenForClient(scopeBase).ExecuteAsync();

                return authenticationResult.AccessToken;
            }

            return string.Empty;
        }

        /// <summary>
        /// Get Power BI client.
        /// </summary>
        /// <returns>Power BI client object.</returns>
        private async Task<PowerBIClient> GetPowerBIClient()
        {
            var tokenCredentials = new TokenCredentials(await GetAppAccessToken(), "Bearer");
            return new PowerBIClient(new Uri(powerBiApiUrl), tokenCredentials);
        }
    }
}

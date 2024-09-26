// <copyright file="MsGraphService.cs" company="Moonrise Software, LLC">
// Copyright (c) Moonrise Software, LLC. All rights reserved.
// Licensed under the GNU Public License, Version 3.0 (https://www.gnu.org/licenses/gpl-3.0.html)
// See https://github.com/MoonriseSoftwareCalifornia/CosmosCMS
// for more information concerning the license and the contributors participating to this project.
// </copyright>

namespace Cosmos.MicrosoftGraph
{
    using Azure.Identity;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Graph.Beta;
    using Microsoft.Graph.Beta.Models;

    // SEE: https://damienbod.com/2021/09/06/using-azure-security-groups-in-asp-net-core-with-an-azure-b2c-identity-provider/
    public class MsGraphService
    {
        private readonly GraphServiceClient _graphServiceClient;
        private readonly string? _tenantId;

        public MsGraphService(GraphServiceClient graphServiceClient)
        {
            _graphServiceClient = graphServiceClient;
        }

        public MsGraphService(IConfiguration configuration)
        {
            string[]? scopes = configuration.GetValue<string>("User.Read.All")?.Split(' ');
            _tenantId = configuration.GetValue<string>("AzureAd:TenantId");

            // Values from app registration
            var clientId = configuration.GetValue<string>("AzureAd:ClientId");
            var clientSecret = configuration.GetValue<string>("AzureAd:ClientSecret");

            var options = new TokenCredentialOptions
            {
                AuthorityHost = AzureAuthorityHosts.AzurePublicCloud
            };

            // https://docs.microsoft.com/dotnet/api/azure.identity.clientsecretcredential
            var clientSecretCredential = new ClientSecretCredential(
                _tenantId, clientId, clientSecret, options);

            _graphServiceClient = new GraphServiceClient(clientSecretCredential, scopes);
        }

        public async Task<User?> GetGraphApiUser(string userId)
        {
            return await _graphServiceClient.Users[userId]
                    .GetAsync(c => c.QueryParameters.Select = new[] { "Identities", "displayName" });
        }

        public async Task<List<User>> GetUsersAsync()
        {
            var users = new List<User>();
            var userCollectionResponse = await _graphServiceClient.Users.GetAsync(c => c.QueryParameters.Select = new[] { "Identities", "displayName" });

            if (userCollectionResponse != null && userCollectionResponse.Value != null)
            {
                users.AddRange(userCollectionResponse.Value);
            }

            return users;
        }

        public async Task<AppRoleAssignmentCollectionResponse?> GetGraphApiUserAppRoles(string userId)
        {
            return await _graphServiceClient.Users[userId]
                    .AppRoleAssignments
                    .GetAsync();
        }

        public async Task<Microsoft.Graph.Beta.Users.Item.GetMemberGroups.GetMemberGroupsPostResponse?> GetGraphApiUserMemberGroups(string userId)
        {
            var requestBody = new Microsoft.Graph.Beta.AdministrativeUnits.Item.GetMemberGroups.GetMemberGroupsPostRequestBody
            {
                SecurityEnabledOnly = true
            };

            var result = await _graphServiceClient.Users[userId]
                .GetMemberGroups
                .PostAsGetMemberGroupsPostResponseAsync(new Microsoft.Graph.Beta.Users.Item.GetMemberGroups.GetMemberGroupsPostRequestBody()
                {
                    SecurityEnabledOnly = true
                });

            return result;
        }

        public async Task<Profile?> GetUserProfile(string userId)
        {
            var result = await _graphServiceClient.Users[userId].Profile.GetAsync();
            return result;
        }

        public async Task<string?> GetGroupNameAsync(string groupId)
        {
            var group = await _graphServiceClient.Groups[groupId].GetAsync();
            return group?.DisplayName;
        }
    }
}

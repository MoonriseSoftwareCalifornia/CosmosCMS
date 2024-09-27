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

    /// <summary>
    /// This class is used to interact with the Microsoft Graph API. It is used to get the user's profile, the user's app roles, the user's member groups, and the user's groups.
    /// </summary>
    // SEE: https://damienbod.com/2021/09/06/using-azure-security-groups-in-asp-net-core-with-an-azure-b2c-identity-provider/
    public class MsGraphService
    {
        private readonly GraphServiceClient graphServiceClient;
        private readonly string? tenantId;

        /// <summary>
        /// Initializes a new instance of the <see cref="MsGraphService"/> class.
        /// </summary>
        /// <param name="graphServiceClient">Graph service client.</param>
        public MsGraphService(GraphServiceClient graphServiceClient)
        {
            this.graphServiceClient = graphServiceClient;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MsGraphService"/> class.
        /// </summary>
        /// <param name="configuration">App configuration.</param>
        public MsGraphService(IConfiguration configuration)
        {
            string[]? scopes = configuration.GetValue<string>("User.Read.All Group.Read.All GroupMember.Read.All Directory.Read.All")?.Split(' ');
            tenantId = configuration.GetValue<string>("AzureAd:TenantId");

            // Values from app registration
            var clientId = configuration.GetValue<string>("AzureAd:ClientId");
            var clientSecret = configuration.GetValue<string>("AzureAd:ClientSecret");

            var options = new TokenCredentialOptions
            {
                AuthorityHost = AzureAuthorityHosts.AzurePublicCloud
            };

            // https://docs.microsoft.com/dotnet/api/azure.identity.clientsecretcredential
            var clientSecretCredential = new ClientSecretCredential(
                tenantId, clientId, clientSecret, options);

            graphServiceClient = new GraphServiceClient(clientSecretCredential, scopes);
        }

        public async Task<User?> GetGraphApiUser(string userId)
        {
            return await graphServiceClient.Users[userId]
                    .GetAsync(c => c.QueryParameters.Select = new[] { "Identities", "displayName" });
        }

        public async Task<List<User>> GetUsersAsync()
        {
            var users = new List<User>();
            var userCollectionResponse = await graphServiceClient.Users.GetAsync(c => c.QueryParameters.Select = new[] { "Identities", "displayName" });

            if (userCollectionResponse != null && userCollectionResponse.Value != null)
            {
                users.AddRange(userCollectionResponse.Value);
            }

            return users;
        }

        public async Task<AppRoleAssignmentCollectionResponse?> GetGraphApiUserAppRoles(string userId)
        {
            return await graphServiceClient.Users[userId]
                    .AppRoleAssignments
                    .GetAsync();
        }

        public async Task<List<Group>?> GetGraphApiUserMemberGroups(string userId)
        {
            var groups = await graphServiceClient.Users[userId].MemberOf.GraphGroup.GetAsync();
            return groups.Value;
        }

        public async Task<List<Group>?> GetGroupsAsync()
        {
            var groups = await graphServiceClient.Groups.GetAsync();
            return groups.Value;
        }

        public async Task<Profile?> GetUserProfile(string userId)
        {
            var result = await graphServiceClient.Users[userId].Profile.GetAsync();
            return result;
        }

        public async Task<string?> GetGroupNameAsync(string groupId)
        {
            var group = await graphServiceClient.Groups[groupId].GetAsync();
            return group?.DisplayName;
        }
    }
}

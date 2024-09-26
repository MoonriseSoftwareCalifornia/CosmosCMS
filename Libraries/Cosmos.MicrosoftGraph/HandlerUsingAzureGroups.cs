// <copyright file="HandlerUsingAzureGroups.cs" company="Moonrise Software, LLC">
// Copyright (c) Moonrise Software, LLC. All rights reserved.
// Licensed under the GNU Public License, Version 3.0 (https://www.gnu.org/licenses/gpl-3.0.html)
// See https://github.com/MoonriseSoftwareCalifornia/CosmosCMS
// for more information concerning the license and the contributors participating to this project.
// </copyright>

namespace Cosmos.MicrosoftGraph
{
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.Extensions.Configuration;

    /// <summary>
    /// Handles the authorization requirement using Azure Entra ID groups.
    /// </summary>
    // SEE: https://damienbod.com/2021/09/06/using-azure-security-groups-in-asp-net-core-with-an-azure-b2c-identity-provider/
    public class HandlerUsingAzureGroups : AuthorizationHandler<GroupAuthorizationRequirement>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="HandlerUsingAzureGroups"/> class.
        /// </summary>
        /// <param name="configuration">Application configuration.</param>
        public HandlerUsingAzureGroups(IConfiguration configuration)
        {
        }

        /// <inheritdoc/>
        protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, GroupAuthorizationRequirement requirement)
        {
            ArgumentNullException.ThrowIfNull(context);
            ArgumentNullException.ThrowIfNull(requirement);

            var claimIdentityprovider = context.User.Claims.FirstOrDefault(t => t.Type == "group"
                && t.Value == requirement.GroupId);

            if (claimIdentityprovider != null)
            {
                context.Succeed(requirement);
            }

            return Task.CompletedTask;
        }
    }
}

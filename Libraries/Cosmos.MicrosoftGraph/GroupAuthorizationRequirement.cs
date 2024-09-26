// <copyright file="GroupAuthorizationRequirement.cs" company="Moonrise Software, LLC">
// Copyright (c) Moonrise Software, LLC. All rights reserved.
// Licensed under the GNU Public License, Version 3.0 (https://www.gnu.org/licenses/gpl-3.0.html)
// See https://github.com/MoonriseSoftwareCalifornia/CosmosCMS
// for more information concerning the license and the contributors participating to this project.
// </copyright>

namespace Cosmos.MicrosoftGraph
{
    using Microsoft.AspNetCore.Authorization;

    /// <summary>
    /// Group authorization requirement based on the Entra ID Group ID.
    /// </summary>
    // SEE: https://damienbod.com/2021/09/06/using-azure-security-groups-in-asp-net-core-with-an-azure-b2c-identity-provider/
    public class GroupAuthorizationRequirement : IAuthorizationRequirement
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="GroupAuthorizationRequirement"/> class.
        /// </summary>
        /// <param name="groupId">Group ID.</param>
        public GroupAuthorizationRequirement(string groupId)
        {
            GroupId = groupId;
        }

        /// <summary>
        /// Gets the Entra ID Group ID.
        /// </summary>
        public string GroupId { get; }

    }
}

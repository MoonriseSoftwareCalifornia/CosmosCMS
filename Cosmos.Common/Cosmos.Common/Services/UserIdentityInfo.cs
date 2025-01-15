// <copyright file="UserIdentityInfo.cs" company="Moonrise Software, LLC">
// Copyright (c) Moonrise Software, LLC. All rights reserved.
// Licensed under the GNU Public License, Version 3.0 (https://www.gnu.org/licenses/gpl-3.0.html)
// See https://github.com/MoonriseSoftwareCalifornia/CosmosCMS
// for more information concerning the license and the contributors participating to this project.
// </copyright>

namespace Cosmos.Cms.Common.Services
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Security.Claims;

    /// <summary>
    /// User identity information.
    /// </summary>
    public class UserIdentityInfo
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="UserIdentityInfo"/> class.
        /// </summary>
        /// <param name="user">User claims principle.</param>
        public UserIdentityInfo(ClaimsPrincipal user)
        {
            if (user.Identity != null && user.Identity.IsAuthenticated)
            {
                IsAuthenticated = user.Identity.IsAuthenticated;
                RoleMembership = ((ClaimsIdentity)user.Identity).Claims
                    .Where(c => c.Type == ClaimTypes.Role)
                    .Select(c => c.Value).ToList();
                UserName = user.Identity.Name;
            }
            else
            {
                IsAuthenticated = false;
                RoleMembership = new List<string>();
                UserName = string.Empty;
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether user is authenticated.
        /// </summary>
        public bool IsAuthenticated { get; set; }

        /// <summary>
        /// Gets or sets user email address.
        /// </summary>
        public string UserName { get; set; }

        /// <summary>
        /// Gets or sets role membership.
        /// </summary>
        public List<string> RoleMembership { get; set; }

        /// <summary>
        /// Checks to see if a user is in a role.
        /// </summary>
        /// <param name="name">User name.</param>
        /// <returns>Indicates that the user has that role assignment.</returns>
        public bool IsInRole(string name)
        {
            return RoleMembership.Contains(name, StringComparer.CurrentCultureIgnoreCase);
        }
    }
}

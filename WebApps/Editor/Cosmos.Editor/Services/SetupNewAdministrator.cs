// <copyright file="SetupNewAdministrator.cs" company="Moonrise Software, LLC">
// Copyright (c) Moonrise Software, LLC. All rights reserved.
// Licensed under the GNU Public License, Version 3.0 (https://www.gnu.org/licenses/gpl-3.0.html)
// See https://github.com/MoonriseSoftwareCalifornia/CosmosCMS
// for more information concerning the license and the contributors participating to this project.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Cosmos.Cms.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.Azure.Cosmos.Linq;

namespace Cosmos.Editor.Services
{
    /// <summary>
    /// Creates a new administrator.
    /// </summary>
    public static class SetupNewAdministrator
    {
        /// <summary>
        /// Ensures the required roles exist, and, add the first user as an administrator.
        /// </summary>
        /// <param name="roleManager">Role manager.</param>
        /// <param name="userManager">User manager.</param>
        /// <param name="user">Logged in identity user.</param>
        /// <returns>True if a new administrator was created.</returns>
        public static async Task<bool> Ensure_RolesAndAdmin_Exists(RoleManager<IdentityRole> roleManager, UserManager<IdentityUser> userManager, IdentityUser user)
        {
            if (!await Ensure_Roles_Exists(roleManager))
            {
                throw new InvalidOperationException("Required roles do not exist or could not be created.");
            }

            var userCount = await userManager.Users.CountAsync();

            // If there is only one registered user (the person who just registered for instance),
            // and that person is not in the Administrators role, then add that person now.
            // There must be at least one administrator.
            if (userCount == 1 && !await userManager.IsInRoleAsync(user, RequiredIdentityRoles.Administrators))
            {
                var result = await userManager.AddToRoleAsync(user, RequiredIdentityRoles.Administrators);

                if (result.Succeeded)
                {
                    var code = await userManager.GenerateEmailConfirmationTokenAsync(user);

                    var confirmResult = await userManager.ConfirmEmailAsync(user, code);

                    if (!confirmResult.Succeeded)
                    {
                        var error = result.Errors.FirstOrDefault();
                        var exception = new InvalidOperationException($"Code: {error?.Code} - {error?.Description}");
                        throw exception;
                    }
                }
                else
                {
                    var error = result.Errors.FirstOrDefault();
                    var exception = new InvalidOperationException($"Code: {error?.Code} - {error?.Description}");
                    throw exception;
                }

                return true;
            }

            return false;
        }

        /// <summary>
        /// Ensures the required roles exist.
        /// </summary>
        /// <param name="roleManager">Role manager service.</param>
        /// <returns>True if roles are now present.</returns>
        public static async Task<bool> Ensure_Roles_Exists(RoleManager<IdentityRole> roleManager)
        {
            var rolesCreated = new List<string>();

            foreach (var role in RequiredIdentityRoles.Roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                {
                    var identityRole = new IdentityRole(role);
                    var result = await roleManager.CreateAsync(identityRole);
                    if (!result.Succeeded)
                    {
                        var error = result.Errors.FirstOrDefault();
                        var exception = new InvalidOperationException($"Code: {error?.Code} - {error?.Description}");
                        throw exception;
                    }

                    rolesCreated.Add(role);
                }
            }

            return AreArraysEqual(rolesCreated, RequiredIdentityRoles.Roles);
        }

        private static bool AreArraysEqual(List<string> array1, List<string> array2)
        {
            if (array1.Count != array2.Count)
            { 
                return false;
            }

            foreach (var item in array1)
            {
                if (!array2.Contains(item))
                {
                    return false;
                }
            }

            return true;
        }
    }
}

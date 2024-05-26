// <copyright file="RequiredIdentityRoles.cs" company="Moonrise Software, LLC">
// Copyright (c) Moonrise Software, LLC. All rights reserved.
// Licensed under the GNU Public License, Version 3.0 (https://www.gnu.org/licenses/gpl-3.0.html)
// See https://github.com/MoonriseSoftwareCalifornia/CosmosCMS
// for more information concerning the license and the contributors participating to this project.
// </copyright>

namespace Cosmos.Cms.Data
{
    using System.Collections.Generic;

    /// <summary>
    /// Required roles for Cosmos.
    /// </summary>
    public static class RequiredIdentityRoles
    {
        /// <summary>
        /// Gets list of roles required for Cosmos to work.
        /// </summary>
        public static List<string> Roles
        {
            // Reviewers,Authors,Editors,Administrators
            get
            {
                var roles = new List<string>();
                roles.Add(Administrators);
                roles.Add(Authors);
                roles.Add(Editors);
                roles.Add(Reviewers);

                return roles;
            }
        }

        /// <summary>
        /// Administrators role.
        /// </summary>
        public const string Administrators = "Administrators";

        /// <summary>
        /// Authors.
        /// </summary>
        public const string Authors = "Authors";

        /// <summary>
        /// Editors.
        /// </summary>
        public const string Editors = "Editors";

        /// <summary>
        /// Reviewers.
        /// </summary>
        public const string Reviewers = "Reviewers";
    }
}

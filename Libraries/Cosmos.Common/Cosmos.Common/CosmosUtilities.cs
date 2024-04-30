// <copyright file="CosmosUtilities.cs" company="Cosmos Website Solutions, Inc.">
// Copyright (c) Cosmos Website Solutions, Inc.. All rights reserved.
// Licensed under the GNU Public License, Version 3.0 (https://www.gnu.org/licenses/gpl-3.0.html)
// See https://github.com/MoonriseSoftwareCalifornia/CosmosCMS
// for more information concerning the license and the contributors participating to this project.
// </copyright>

namespace Cosmos.Common
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Security.Claims;
    using System.Threading.Tasks;
    using Cosmos.BlobService;
    using Cosmos.Common.Data;
    using Cosmos.Common.Models;
    using Microsoft.EntityFrameworkCore;

    /// <summary>
    /// Static utilities class.
    /// </summary>
    public static class CosmosUtilities
    {
        /// <summary>
        /// Authenticates a user using article permissions.
        /// </summary>
        /// <param name="dbContext">Database context.</param>
        /// <param name="user">Claims identity principle.</param>
        /// <param name="articleNumber">Article number.</param>
        /// <returns>Indicates a user is authenticated as a <see cref="bool"/>.</returns>
        public static async Task<bool> AuthUser(
            ApplicationDbContext dbContext,
            ClaimsPrincipal user,
            int articleNumber)
        {
            List<ArticlePermission> permissions = null;
            try
            {
                permissions = dbContext.ArticleCatalog.FirstOrDefault(l => l.ArticleNumber == articleNumber).ArticlePermissions;
                if (permissions == null || permissions.Count == 0)
                {
                    return false; // No one can access this page.
                }
            }
            catch (Exception ex)
            {
                var message = ex.Message; // Debugging
                return false;
            }

            var roleIds = permissions.Where(w => w.IsRoleObject).Select(s => s.IdentityObjectId).ToArray();

            // Check for anonymous user access.
            if (await dbContext.Roles.Where(w => roleIds.Contains(w.Id) && w.NormalizedName == "ANONYMOUS").CosmosAnyAsync())
            {
                return true; // Anonymous users can view, so that means everyone.
            }

            // At this point, only authenticated users have access.
            if (!user.Identity.IsAuthenticated)
            {
                return false;
            }

            // Get the current user ID and see if this person has user-specific access.
            var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
            if (permissions.Any(a => a.IdentityObjectId.Equals(userId, StringComparison.OrdinalIgnoreCase)))
            {
                return true; // Current user has access.
            }

            // Finally, if a user has role permissions, grant access here.
            return (await dbContext.UserRoles.CountAsync(a => a.UserId == userId && roleIds.Contains(a.RoleId))) > 0;
        }

        /// <summary>
        /// Gets the folder contents for an article.
        /// </summary>
        /// <param name="storageContext">File storage context.</param>
        /// <param name="articleNumber">Article number (not ID).</param>
        /// <param name="path">Path to article.</param>
        /// <returns>Returns file and folder metadata as a <see cref="FileManagerEntry"/> <see cref="List{T}"/>.</returns>
        /// <remarks>Does NOT authenticate the user.</remarks>
        public static async Task<List<FileManagerEntry>> GetArticleFolderContents(StorageContext storageContext, int articleNumber, string path = "")
        {
            path = $"/pub/articles/{articleNumber}/{path.TrimStart('/')}";

            var contents = await storageContext.GetFolderContents(path);

            return contents;
        }

        /// <summary>
        /// Gets the articles for a given user.
        /// </summary>
        /// <param name="dbcontext">Database context.</param>
        /// <param name="user">User claims.</param>
        /// <returns>Returns a <see cref="TableOfContentsItem"/> <see cref="List{T}"/>.</returns>
        public static async Task<List<TableOfContentsItem>> GetArticlesForUser(ApplicationDbContext dbcontext, ClaimsPrincipal user)
        {
            var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);

            var objectIds = await dbcontext.UserRoles.Where(w => w.UserId == userId).Select(s => s.RoleId).ToListAsync();
            objectIds.Add(userId);

            var articleNumbers = await dbcontext.ArticleCatalog
                .Where(
                    w => w.ArticlePermissions.Any() == false ||
                    w.ArticlePermissions.Any(a => objectIds.Contains(a.IdentityObjectId)))
                .Select(s => s.ArticleNumber).ToArrayAsync();

            var data = await dbcontext.Pages.Where(w => articleNumbers.Contains(w.ArticleNumber))
                .Select(s => new TableOfContentsItem()
                {
                    AuthorInfo = s.AuthorInfo,
                    BannerImage = s.BannerImage,
                    Published = s.Published.Value,
                    Title = s.Title,
                    Updated = s.Updated,
                    UrlPath = s.UrlPath
                }).ToListAsync();

            return data;
        }
    }
}

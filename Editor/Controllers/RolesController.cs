﻿// <copyright file="RolesController.cs" company="Moonrise Software, LLC">
// Copyright (c) Moonrise Software, LLC. All rights reserved.
// Licensed under the GNU Public License, Version 3.0 (https://www.gnu.org/licenses/gpl-3.0.html)
// See https://github.com/MoonriseSoftwareCalifornia/CosmosCMS
// for more information concerning the license and the contributors participating to this project.
// </copyright>

namespace Sky.Editor.Controllers
{
    using System;
    using System.Linq;
    using System.Threading.Tasks;
    using Cosmos.Common.Data;
    using Cosmos.Editor.Services;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Identity;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.EntityFrameworkCore;
    using Sky.Cms.Models;

    /// <summary>
    /// Role management controller.
    /// </summary>
    // [ResponseCache(NoStore = true)]
    [Authorize(Roles = "Administrators")]
    public class RolesController : Controller
    {
        private readonly RoleManager<IdentityRole> roleManager;
        private readonly UserManager<IdentityUser> userManager;
        private readonly ApplicationDbContext dbContext;

        /// <summary>
        /// Initializes a new instance of the <see cref="RolesController"/> class.
        /// </summary>
        /// <param name="userManager">User manager service.</param>
        /// <param name="roleManager">Role manager service.</param>
        /// <param name="dbContext">Database context.</param>
        public RolesController(
            UserManager<IdentityUser> userManager,
            RoleManager<IdentityRole> roleManager,
            ApplicationDbContext dbContext
            )
        {
            this.userManager = userManager;
            this.roleManager = roleManager;
            this.dbContext = dbContext;

            SetupNewAdministrator.Ensure_Roles_Exists(roleManager).Wait();
        }

        /// <summary>
        /// Adds a new role.
        /// </summary>
        /// <param name="roleName">Role name.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(string roleName)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            if (string.IsNullOrEmpty(roleName))
            {
                return BadRequest("Rule name is required.");
            }

            if (await roleManager.RoleExistsAsync(roleName))
            {
                return BadRequest($"Role '{roleName}' already exists");
            }

            try
            {
                await roleManager.CreateAsync(new IdentityRole()
                {
                    Id = Guid.NewGuid().ToString(),
                    Name = roleName
                });
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }

            return Ok();
        }

        /// <summary>
        /// Role inventory.
        /// </summary>
        /// <param name="ids">Role IDs.</param>
        /// <param name="sortOrder">Sort direction.</param>
        /// <param name="currentSort">Current sort field.</param>
        /// <param name="pageNo">Page number.</param>
        /// <param name="pageSize">Page size.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public async Task<IActionResult> Index([Bind("ids")] string ids, string sortOrder = "asc", string currentSort = "Name", int pageNo = 0, int pageSize = 10)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            if (string.IsNullOrEmpty(ids))
            {
                ViewData["Ids"] = null;
            }
            else
            {
                ViewData["Ids"] = ids.Split(',');
            }

            ViewData["sortOrder"] = sortOrder;
            ViewData["currentSort"] = currentSort;
            ViewData["pageNo"] = pageNo;
            ViewData["pageSize"] = pageSize;

            var query = roleManager.Roles.AsQueryable();

            ViewData["RowCount"] = await query.CountAsync();

            if (sortOrder == "desc")
            {
                if (!string.IsNullOrEmpty(currentSort))
                {
                    switch (currentSort)
                    {
                        case "Name":
                            query = query.OrderByDescending(o => o.Name);
                            break;
                    }
                }
            }
            else
            {
                if (!string.IsNullOrEmpty(currentSort))
                {
                    switch (currentSort)
                    {
                        case "Name":
                            query = query.OrderBy(o => o.Name);
                            break;
                    }
                }
            }

            var model = query.Skip(pageNo * pageSize).Take(pageSize);

            return View(await model.ToListAsync());
        }

        /// <summary>
        /// Deletes roles.
        /// </summary>
        /// <param name="ids">IDs of roles to delete.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        [HttpPost]
        public async Task<ActionResult> Delete(string[] ids)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var safeRoles = new string[] { "authors", "administrators", "authors", "editors", "reviewers", "anonymous", "authenticated" };

            var roles = await roleManager.Roles
                .Where(w => ids.Contains(w.Id)).ToListAsync();

            if (roles != null && ModelState.IsValid)
            {
                foreach (var role in roles)
                {
                    if (!safeRoles.Contains(role.Name.ToLower()))
                    {
                        var identityRole = await roleManager.FindByIdAsync(role.Id);

                        await roleManager.DeleteAsync(identityRole);
                    }
                }
            }

            return Ok();
        }

        /// <summary>
        /// Gets users for a given email query.
        /// </summary>
        /// <param name="startsWith">Starts with string.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public async Task<IActionResult> GetUsers(string startsWith)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var query = userManager.Users.OrderBy(o => o.Email)
                .Select(
                  s => new
                  {
                      s.Id,
                      s.Email
                  }
                ).AsQueryable();

            if (!string.IsNullOrEmpty(startsWith))
            {
                query = query.Where(s => s.Email.ToLower().StartsWith(startsWith.ToLower()));
            }

            var users = await query.ToListAsync();

            return Json(users);
        }

        /// <summary>
        /// Page designed to add/remove users from a single role.
        /// </summary>
        /// <param name="id">Role id.</param>
        /// <param name="sortOrder">Sort direction.</param>
        /// <param name="currentSort">Sort field.</param>
        /// <param name="pageNo">Page number.</param>
        /// <param name="pageSize">Page size.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public async Task<IActionResult> UsersInRole(string id = "", string sortOrder = "asc", string currentSort = "EmailAddress", int pageNo = 0, int pageSize = 10)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var role = await roleManager.FindByIdAsync(id);
            if (role == null)
            {
                return NotFound();
            }

            ViewData["RoleInfo"] = new UsersInRoleViewModel()
            {
                RoleId = role.Id,
                RoleName = role.Name
            };
            ViewData["sortOrder"] = sortOrder;
            ViewData["currentSort"] = currentSort;
            ViewData["pageNo"] = pageNo;
            ViewData["pageSize"] = pageSize;

            var usersInRole = await userManager.GetUsersInRoleAsync(role.Name);

            var query = dbContext.Users.AsQueryable();

            if (!string.IsNullOrEmpty(id))
            {
                var identityRole = await roleManager.FindByIdAsync(id);

                var ids = usersInRole.Select(s => s.Id).ToArray();
                query = query.Where(w => ids.Contains(w.Id));
            }

            // Get count
            ViewData["RowCount"] = await query.CountAsync();

            if (sortOrder.Equals("desc", StringComparison.InvariantCultureIgnoreCase))
            {
                switch (currentSort)
                {
                    case "EmailConfirmed":
                        query = query.OrderByDescending(o => o.EmailConfirmed);
                        break;
                    case "EmailAddress":
                        query = query.OrderByDescending(o => o.Email);
                        break;
                    case "PhoneNumber":
                        query.OrderByDescending(o => o.PhoneNumber);
                        break;
                    case "TwoFactorEnabled":
                        query = query.OrderByDescending(o => o.TwoFactorEnabled);
                        break;
                }
            }
            else
            {
                switch (currentSort)
                {
                    case "EmailConfirmed":
                        query = query.OrderBy(o => o.EmailConfirmed);
                        break;
                    case "EmailAddress":
                        query = query.OrderBy(o => o.Email);
                        break;
                    case "PhoneNumber":
                        query = query.OrderBy(o => o.PhoneNumber);
                        break;
                    case "TwoFactorEnabled":
                        query = query.OrderBy(o => o.TwoFactorEnabled);
                        break;
                }
            }

            query = query.Skip(pageNo * pageSize).Take(pageSize);

            var data = await query.ToListAsync();

            var users = data.Select(s => new UserIndexViewModel()
            {
                UserId = s.Id,
                EmailAddress = s.Email,
                EmailConfirmed = s.EmailConfirmed,
                PhoneNumber = s.PhoneNumber,
                IsLockedOut = s.LockoutEnd.HasValue ? s.LockoutEnd < DateTimeOffset.UtcNow : false,
                TwoFactorEnabled = s.TwoFactorEnabled
            }).ToList();

            // Now get the role for these people
            var roles = await dbContext.Roles.ToListAsync();
            var userIds = await query.Select(s => s.Id).ToListAsync();
            var links = await dbContext.UserRoles.Where(ur => userIds.Contains(ur.UserId)).ToListAsync();

            foreach (var user in users)
            {
                var roleIds = links.Where(w => w.UserId == user.UserId).Select(s => s.RoleId).ToList();
                user.RoleMembership = roles.Where(w => roleIds.Contains(w.Id)).Select(s => s.Name).ToList();
            }

            // s.LockoutEnd.HasValue ? s.LockoutEnd < DateTimeOffset.UtcNow 
            return View(users);
        }

        /// <summary>
        /// Saves changes to the user assignments in a role.
        /// </summary>
        /// <param name="model">Post model.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UsersInRole(UsersInRoleViewModel model)
        {
            if (ModelState.IsValid)
            {
                foreach (var id in model.UserIds)
                {
                    var user = await userManager.FindByIdAsync(id);
                    await userManager.AddToRoleAsync(user, model.RoleName);
                }

                model.UserIds = null;

                return View(model);
            }

            // Not valid, return the selected users.
            model.Users = await userManager.Users.Where(w => model.UserIds.Contains(w.Id))
                .Select(
                s => new SelectedUserViewModel()
                {
                    Id = s.Id,
                    Email = s.Email
                }
                ).ToListAsync();

            return View(model);
        }

        /// <summary>
        /// Removes users from a Role.
        /// </summary>
        /// <param name="roleId">Role ID.</param>
        /// <param name="userIds">User IDs.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveUsers(string roleId, string[] userIds)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var role = await roleManager.FindByIdAsync(roleId);

            foreach (var userId in userIds)
            {
                var identityUser = await userManager.FindByIdAsync(userId);

                // Make sure there is at least one administrator remaining
                if (role.Name.Equals("User Administrators", StringComparison.InvariantCultureIgnoreCase))
                {
                    var administrators = await userManager.GetUsersInRoleAsync("User Administrators");

                    if (administrators.Count() > 0)
                    {
                        await userManager.RemoveFromRoleAsync(identityUser, role.Name);
                    }
                }
                else
                {
                    var result = await userManager.RemoveFromRoleAsync(identityUser, role.Name);
                    var t = result;
                }
            }

            return RedirectToAction("Index");
        }
    }
}

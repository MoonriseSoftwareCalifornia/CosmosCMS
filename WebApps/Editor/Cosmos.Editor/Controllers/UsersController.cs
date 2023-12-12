// <copyright file="UsersController.cs" company="Moonrise Software, LLC">
// Copyright (c) Moonrise Software, LLC. All rights reserved.
// Licensed under the GNU Public License, Version 3.0 (https://www.gnu.org/licenses/gpl-3.0.html)
// See https://github.com/MoonriseSoftwareCalifornia/CosmosCMS
// for more information concerning the license and the contributors participating to this project.
// </copyright>

namespace Cosmos.Cms.Controllers
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Text;
    using System.Text.Encodings.Web;
    using System.Threading.Tasks;
    using Cosmos.Cms.Common.Services.Configurations;
    using Cosmos.Cms.Models;
    using Cosmos.Common.Data;
    using Cosmos.EmailServices;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Identity;
    using Microsoft.AspNetCore.Identity.UI.Services;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.AspNetCore.WebUtilities;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Options;

    // See: https://docs.microsoft.com/en-us/aspnet/core/mvc/controllers/areas?view=aspnetcore-3.1

    /// <summary>
    /// User management controller.
    /// </summary>
    // [ResponseCache(NoStore = true)]
    [Authorize(Roles = "Administrators")]
    public class UsersController : Controller
    {
        private readonly ILogger<UsersController> logger;
        private readonly RoleManager<IdentityRole> roleManager;
        private readonly UserManager<IdentityUser> userManager;
        private readonly SendGridEmailSender emailSender;
        private readonly ApplicationDbContext dbContext;

        /// <summary>
        /// Initializes a new instance of the <see cref="UsersController"/> class.
        /// Constructor.
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="userManager"></param>
        /// <param name="roleManager"></param>
        /// <param name="emailSender"></param>
        /// <param name="dbContext"></param>
        public UsersController(
            ILogger<UsersController> logger,
            UserManager<IdentityUser> userManager,
            RoleManager<IdentityRole> roleManager,
            IEmailSender emailSender,
            ApplicationDbContext dbContext)
        {
            this.logger = logger;
            this.userManager = userManager;
            this.roleManager = roleManager;
            this.emailSender = (SendGridEmailSender)emailSender;
            this.dbContext = dbContext;
        }

        /// <summary>
        /// User account inventory.
        /// </summary>
        /// <param name="sortOrder"></param>
        /// <param name="currentSort"></param>
        /// <param name="pageNo"></param>
        /// <param name="pageSize"></param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public async Task<IActionResult> AuthorInfos(string sortOrder = "asc", string currentSort = "EmailAddress", int pageNo = 0, int pageSize = 10)
        {
            ViewData["sortOrder"] = sortOrder;
            ViewData["currentSort"] = currentSort;
            ViewData["pageNo"] = pageNo;
            ViewData["pageSize"] = pageSize;

            // Get the current list of infos
            var ids = await dbContext.AuthorInfos.Select(s => s.UserId).ToArrayAsync();
            var check = await dbContext.Users.Select(s => new AuthorInfo { UserId = s.Id, EmailAddress = s.Email }).ToListAsync();
            var missing = check.Where(w => ids.Contains(w.UserId) == false).ToList();
            // Get missing infos and add them
            dbContext.AuthorInfos.AddRange(missing);
            await dbContext.SaveChangesAsync();

            var query = dbContext.AuthorInfos.AsQueryable();

            // Get count
            ViewData["RowCount"] = await query.CountAsync();

            if (sortOrder.Equals("desc", StringComparison.InvariantCultureIgnoreCase))
            {
                switch (currentSort)
                {
                    case "EmailAddress":
                        query = query.OrderByDescending(o => o.EmailAddress);
                        break;
                    case "UserId":
                        query = query.OrderByDescending(o => o.UserId);
                        break;
                    case "AuthorName":
                        query.OrderByDescending(o => o.AuthorName);
                        break;
                }
            }
            else
            {
                switch (currentSort)
                {
                    case "EmailAddress":
                        query = query.OrderBy(o => o.EmailAddress);
                        break;
                    case "UserId":
                        query = query.OrderBy(o => o.UserId);
                        break;
                    case "AuthorName":
                        query = query.OrderBy(o => o.AuthorName);
                        break;
                }
            }

            query = query.Skip(pageNo * pageSize).Take(pageSize);

            var data = await query.ToListAsync();

            return View(data);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="Id"></param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public async Task<IActionResult> AuthorInfoEdit(string Id)
        {
            return View(await dbContext.AuthorInfos.FindAsync(Id));
        }

        /// <summary>
        /// Edit Editor/Author information.
        /// </summary>
        /// <param name="model"></param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        [HttpPost]
        public async Task<IActionResult> AuthorInfoEdit(AuthorInfo model)
        {
            if (ModelState.IsValid)
            {
                dbContext.Entry(model).State = EntityState.Modified;
                await dbContext.SaveChangesAsync();
                return RedirectToAction("AuthorInfos");
            }

            return View(model);
        }

        /// <summary>
        /// User account inventory.
        /// </summary>
        /// <param name="Id"></param>
        /// <param name="sortOrder"></param>
        /// <param name="currentSort"></param>
        /// <param name="pageNo"></param>
        /// <param name="pageSize"></param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public async Task<IActionResult> Index(string Id = "", string sortOrder = "asc", string currentSort = "EmailAddress", int pageNo = 0, int pageSize = 10)
        {
            ViewData["sortOrder"] = sortOrder;
            ViewData["currentSort"] = currentSort;
            ViewData["pageNo"] = pageNo;
            ViewData["pageSize"] = pageSize;

            var query = dbContext.Users.AsQueryable();

            if (!string.IsNullOrEmpty(Id))
            {
                var identityRole = await roleManager.FindByIdAsync(Id);

                var usersInRole = await userManager.GetUsersInRoleAsync(identityRole.Name);

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
        /// Create a user.
        /// </summary>
        /// <returns>Returns a new View with a new <see cref="UserCreateViewModel"/>.</returns>
        public IActionResult Create()
        {
            return View(new UserCreateViewModel());
        }

        /// <summary>
        /// Create a user.
        /// </summary>
        /// <param name="model"></param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(UserCreateViewModel model)
        {
            try
            {
                if (string.IsNullOrEmpty(model.Password) && model.GenerateRandomPassword == false)
                {
                    ModelState.AddModelError("GenerateRandomPassword", "Must generate password if one not given.");
                }

                if (!ModelState.IsValid)
                {
                    return View(model);
                }

                var result = await CreateAccount(model);

                if (result.IdentityResult.Succeeded)
                {
                    if (result.UserCreateViewModel == null)
                    {
                        return View("UserCreated", null);
                    }
                    else
                    {
                        return View("UserCreated", new UserCreatedViewModel(result.UserCreateViewModel, result.SendResult));
                    }
                }

                foreach (var error in result.IdentityResult.Errors)
                {
                    ModelState.AddModelError(string.Empty, $"Code: {error.Code} Description: {error.Description}");
                }

                return View(model);
            }
            catch (Exception e)
            {
                logger.LogError(e.Message, e);
                throw;
            }
        }


        /// <summary>
        /// Creates a single user account.
        /// </summary>
        /// <param name="model">New user view model.</param>
        /// <param name="isBatchJob">Email verifications work flows are different for users created in batch.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        private async Task<BulkUserCreatedResult> CreateAccount(UserCreateViewModel model, bool isBatchJob = false)
        {
            var result = new BulkUserCreatedResult();

            if (string.IsNullOrEmpty(model.Password) && model.GenerateRandomPassword == false)
            {
                ModelState.AddModelError("GenerateRandomPassword", "Must generate password if one not given.");
            }

            if (!ModelState.IsValid)
            {
                return null;
            }

            if (model.GenerateRandomPassword)
            {
                var password = new PasswordGenerator.Password();
                model.Password = password.Next();
            }

            var user = new IdentityUser()
            {
                Email = model.EmailAddress,
                EmailConfirmed = model.EmailConfirmed,
                NormalizedEmail = model.EmailAddress.ToUpperInvariant(),
                UserName = model.EmailAddress,
                NormalizedUserName = model.EmailAddress.ToUpperInvariant(),
                Id = Guid.NewGuid().ToString(),
                PhoneNumber = model.PhoneNumber,
                PhoneNumberConfirmed = model.PhoneNumberConfirmed
            };

            result.IdentityResult = await userManager.CreateAsync(user, model.Password);

            if (result.IdentityResult.Succeeded)
            {
                if (model.EmailConfirmed)
                {
                    // Confirm email if set.
                    var emailCode = await userManager.GenerateEmailConfirmationTokenAsync(user);
                    var result2 = await userManager.ConfirmEmailAsync(user,
                        Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(emailCode)));
                }

                // Do we have to send an email confirmation or a reset password message?
                if (isBatchJob)
                {
                    // Always send a reset password email in this case
                    // For more information on how to enable account confirmation and password reset please
                    // visit https://go.microsoft.com/fwlink/?LinkID=532713
                    var code = await userManager.GeneratePasswordResetTokenAsync(user);
                    code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));
                    var callbackUrl = Url.Page(
                        "/Account/ResetPassword",
                        pageHandler: null,
                        values: new { area = "Identity", code },
                        protocol: Request.Scheme);

                    var identityUser = await userManager.GetUserAsync(User);

                    await emailSender.SendEmailAsync(
                        user.Email,
                        "Create Password",
                        $"A new user account was created for you by {identityUser.Email}. Now we need you to create a password for your account by  <a href='{HtmlEncoder.Default.Encode(callbackUrl)}'>clicking here</a>.");

                    result.SendResult = new SendResult()
                    {
                        Message = await emailSender.Response.Body.ReadAsStringAsync(),
                        StatusCode = emailSender.Response.StatusCode
                    };

                    result.UserCreateViewModel = model;

                    if (!result.SendResult.IsSuccessStatusCode)
                    {
                        ModelState.AddModelError(string.Empty, $"Could not send reset password email to: '{model.EmailAddress}'. Error: {result.SendResult.Message}");
                    }
                }
                else
                {
                    // Send an email confirmation if required.
                    var code = await userManager.GenerateEmailConfirmationTokenAsync(user);
                    code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));
                    var callbackUrl = Url.Page(
                        "/Account/ConfirmEmail",
                        pageHandler: null,
                        values: new { area = "Identity", userId = user.Id, code, returnUrl = "/" },
                        protocol: Request.Scheme);

                    await emailSender.SendEmailAsync(
                        user.Email,
                        "Confirm your email",
                        htmlMessage: $"Please confirm your account by <a href='{HtmlEncoder.Default.Encode(callbackUrl)}'>clicking here</a>.");

                    result.SendResult = new SendResult()
                    {
                        Message = await emailSender.Response.Body.ReadAsStringAsync(),
                        StatusCode = emailSender.Response.StatusCode
                    };

                    result.UserCreateViewModel = model;

                    if (!result.SendResult.IsSuccessStatusCode)
                    {
                        ModelState.AddModelError(string.Empty, $"Could not send email to: '{model.EmailAddress}'. Error: {result.SendResult.Message}");
                    }
                }

                return result;
            }

            foreach (var error in result.IdentityResult.Errors)
            {
                ModelState.AddModelError(string.Empty, $"Code: {error.Code} Description: {error.Description}");
            }

            return null;
        }

        /// <summary>
        /// Deletes users.
        /// </summary>
        /// <param name="userIds"></param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteUsers(string userIds)
        {
            if (userManager.Users.Count() < 2)
            {
                ModelState.AddModelError(string.Empty, "Cannot delete the last user account.");
            }

            var ids = userIds.Split(',');

            foreach (var id in ids)
            {
                var identityUser = await userManager.FindByIdAsync(id);

                var roles = await userManager.GetRolesAsync(identityUser);

                if (roles.Any(a => a.Equals("Administrators", StringComparison.InvariantCultureIgnoreCase)))
                {
                    ModelState.AddModelError(string.Empty, "Cannot remove a member of the User Administrators role.");
                }
                else
                {
                    await userManager.DeleteAsync(identityUser);
                }
            }

            return RedirectToAction("Index");
        }

        /// <summary>
        /// Gets the role assignments for a user.
        /// </summary>
        /// <param name="id"></param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        private async Task<UserRoleAssignmentsViewModel> GetRoleAssignmentsForUser(string id)
        {
            var roles = new List<IdentityRole>();
            var user = await userManager.FindByIdAsync(id);
            var roleNames = await userManager.GetRolesAsync(user);
            foreach (var name in roleNames)
            {
                roles.Add(await roleManager.FindByNameAsync(name));
            }

            return new UserRoleAssignmentsViewModel()
            {
                Id = user.Id,
                Email = user.Email,
                RoleIds = roles.Select(s => s.Id).ToArray()
            };
        }

        /// <summary>
        /// Gets a total list of roles.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public async Task<IActionResult> GetRoles(string text)
        {
            var query = roleManager.Roles.OrderBy(o => o.Name).Select(s => new
            {
                s.Id,
                s.Name
            }).AsQueryable();

            if (!string.IsNullOrEmpty(text))
            {
                query = query.Where(s => s.Name.ToLower().StartsWith(text.ToLower()));
            }

            var roles = await query.ToListAsync();

            return Json(roles);
        }

        /// <summary>
        ///     Gets the role membership for a user by id.
        /// </summary>
        /// <param name="id">User ID.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public async Task<IActionResult> RoleMembership([Bind("id")] string id)
        {
            var user = await userManager.FindByIdAsync(id);

            if (user == null)
            {
                return NotFound();
            }

            ViewData["saved"] = null;

            var roleList = (await userManager.GetRolesAsync(await userManager.FindByIdAsync(id))).ToList();

            return View();
        }

        /// <summary>
        /// Resends a user's email confirmation.
        /// </summary>
        /// <param name="Id"></param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        [HttpPost]
        public async Task<IActionResult> ResendEmailConfirmation(string Id)
        {
            var user = await userManager.FindByIdAsync(Id);
            if (user == null)
            {
                return NotFound();
            }

            var result = new ResendEmailConfirmResult();

            try
            {
                var code = await userManager.GenerateEmailConfirmationTokenAsync(user);
                code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));
                var callbackUrl = Url.Page(
                    "/Account/ConfirmEmail",
                    pageHandler: null,
                    values: new { userId = Id, code },
                    protocol: Request.Scheme);

                await emailSender.SendEmailAsync(
                    user.Email,
                    "Confirm your email",
                    $"Please confirm your account by <a href='{HtmlEncoder.Default.Encode(callbackUrl)}'>clicking here</a>.");

                var sendResult = new SendResult()
                {
                    Message = await emailSender.Response.Body.ReadAsStringAsync(),
                    StatusCode = emailSender.Response.StatusCode
                };

                if (sendResult != null)
                {
                    if (sendResult.IsSuccessStatusCode)
                    {
                        result.Success = true;
                    }
                    else
                    {
                        result.Error = sendResult.Message;
                    }
                }
            }
            catch (Exception e)
            {
                result.Error = e.Message;
            }

            return Json(result);
        }

        /// <summary>
        /// Manages the role assignments for a user.
        /// </summary>
        /// <param name="id">User Id.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public async Task<IActionResult> UserRoles(string id)
        {
            ViewData["RoleList"] = await roleManager.Roles.OrderBy(o => o.Name).ToListAsync();

            var model = await GetRoleAssignmentsForUser(id);

            return View(model);
        }

        /// <summary>
        /// Sends a password reset to an account holder.
        /// </summary>
        /// <param name="emailAddress">User email address.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        [HttpGet]
        public async Task<IActionResult> SendPasswordReset(string emailAddress)
        {
            var user = await userManager.FindByEmailAsync(emailAddress);
            if (user == null)
            {
                return NotFound();
            }

            // For more information on how to enable account confirmation and password reset please 
            // visit https://go.microsoft.com/fwlink/?LinkID=532713
            var code = await userManager.GeneratePasswordResetTokenAsync(user);
            code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));

            var callbackUrl = Url.Page(
                "/Account/ResetPassword",
                null,
                new { area = "Identity", code },
                Request.Scheme);

            await emailSender.SendEmailAsync(
                emailAddress,
                "Reset Password",
                $"Please reset your password by <a href='{HtmlEncoder.Default.Encode(callbackUrl)}'>clicking here</a>.");

            return Ok(200);
        }

        /// <summary>
        /// Updates a user's role assignments.
        /// </summary>
        /// <param name="model"></param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UserRoles(UserRoleAssignmentsViewModel model)
        {
            var user = await userManager.FindByIdAsync(model.Id);

            var exisitingRoles = await userManager.GetRolesAsync(user);

            ViewData["RoleList"] = await roleManager.Roles.OrderBy(o => o.Name).ToListAsync();

            // First, remove from all roles
            foreach (var name in exisitingRoles)
            {
                if (name.Equals("Administrators", StringComparison.InvariantCultureIgnoreCase))
                {
                    // Make sure there is at least one administrator remaining
                    var administrators = await userManager.GetUsersInRoleAsync("Administrators");

                    if (administrators.Count() > 1)
                    {
                        await userManager.RemoveFromRoleAsync(user, name);
                    }
                }
                else
                {
                    await userManager.RemoveFromRoleAsync(user, name);
                }
            }

            var roles = new List<IdentityRole>();

            foreach (var roleId in model.RoleIds)
            {
                roles.Add(await roleManager.FindByIdAsync(roleId));
            }

            // Now add back the new assignments
            foreach (var role in roles)
            {
                await userManager.AddToRoleAsync(user, role.Name);
            }

            return View(await GetRoleAssignmentsForUser(model.Id));
        }

        /// <summary>
        /// Privacy page.
        /// </summary>
        /// <returns></returns>
        public IActionResult Privacy()
        {
            return View();
        }

        /// <summary>
        /// Error page.
        /// </summary>
        /// <returns></returns>
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}

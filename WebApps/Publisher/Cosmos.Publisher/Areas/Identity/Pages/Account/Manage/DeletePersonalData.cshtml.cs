// <copyright file="DeletePersonalData.cshtml.cs" company="Moonrise Software, LLC">
// Copyright (c) Moonrise Software, LLC. All rights reserved.
// Licensed under the GNU Public License, Version 3.0 (https://www.gnu.org/licenses/gpl-3.0.html)
// See https://github.com/MoonriseSoftwareCalifornia/CosmosCMS
// for more information concerning the license and the contributors participating to this project.
// </copyright>

namespace Cosmos.Cms.Areas.Identity.Pages.Account.Manage
{
    using System;
    using System.ComponentModel.DataAnnotations;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Identity;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.AspNetCore.Mvc.RazorPages;
    using Microsoft.Extensions.Logging;

    /// <summary>
    /// Delete personoal data model.
    /// </summary>
    public class DeletePersonalDataModel : PageModel
    {
        private readonly ILogger<DeletePersonalDataModel> logger;
        private readonly SignInManager<IdentityUser> signInManager;
        private readonly UserManager<IdentityUser> userManager;

        /// <summary>
        /// Initializes a new instance of the <see cref="DeletePersonalDataModel"/> class.
        /// </summary>
        /// <param name="userManager">User manager.</param>
        /// <param name="signInManager">Sign in manager.</param>
        /// <param name="logger">Logger.</param>
        public DeletePersonalDataModel(
            UserManager<IdentityUser> userManager,
            SignInManager<IdentityUser> signInManager,
            ILogger<DeletePersonalDataModel> logger)
        {
            this.userManager = userManager;
            this.signInManager = signInManager;
            this.logger = logger;
        }

        /// <summary>
        /// Gets or sets input model.
        /// </summary>
        [BindProperty]
        public InputModel Input { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether password.
        /// </summary>
        public bool RequirePassword { get; set; }

        /// <summary>
        /// On get event handler.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public async Task<IActionResult> OnGet()
        {
            var user = await userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound($"Unable to load user with ID '{userManager.GetUserId(User)}'.");
            }

            RequirePassword = await userManager.HasPasswordAsync(user);
            return Page();
        }

        /// <summary>
        /// On post event handler.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public async Task<IActionResult> OnPostAsync()
        {
            var user = await userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound($"Unable to load user with ID '{userManager.GetUserId(User)}'.");
            }

            RequirePassword = await userManager.HasPasswordAsync(user);
            if (RequirePassword)
            {
                if (!await userManager.CheckPasswordAsync(user, Input.Password))
                {
                    ModelState.AddModelError(string.Empty, "Incorrect password.");
                    return Page();
                }
            }

            var result = await userManager.DeleteAsync(user);
            var userId = await userManager.GetUserIdAsync(user);
            if (!result.Succeeded)
            {
                throw new InvalidOperationException($"Unexpected error occurred deleting user with ID '{userId}'.");
            }

            await signInManager.SignOutAsync();

            logger.LogInformation("User with ID '{UserId}' deleted themselves.", userId);

            return Redirect("~/");
        }

        /// <summary>
        /// Input model.
        /// </summary>
        public class InputModel
        {
            /// <summary>
            /// Gets or sets password.
            /// </summary>
            [Required]
            [DataType(DataType.Password)]
            public string Password { get; set; }
        }
    }
}
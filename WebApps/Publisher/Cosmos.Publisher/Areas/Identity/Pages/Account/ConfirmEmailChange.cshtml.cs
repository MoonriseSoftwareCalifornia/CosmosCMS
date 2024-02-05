// <copyright file="ConfirmEmailChange.cshtml.cs" company="Moonrise Software, LLC">
// Copyright (c) Moonrise Software, LLC. All rights reserved.
// Licensed under the GNU Public License, Version 3.0 (https://www.gnu.org/licenses/gpl-3.0.html)
// See https://github.com/MoonriseSoftwareCalifornia/CosmosCMS
// for more information concerning the license and the contributors participating to this project.
// </copyright>

namespace Cosmos.Cms.Areas.Identity.Pages.Account
{
    using System.Diagnostics.CodeAnalysis;
    using System.Text;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Identity;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.AspNetCore.Mvc.RazorPages;
    using Microsoft.AspNetCore.WebUtilities;

    /// <summary>
    /// Confirm email change page model.
    /// </summary>
    [AllowAnonymous]
    public class ConfirmEmailChangeModel : PageModel
    {
        private readonly SignInManager<IdentityUser> signInManager;
        private readonly UserManager<IdentityUser> userManager;

        /// <summary>
        /// Initializes a new instance of the <see cref="ConfirmEmailChangeModel"/> class.
        /// </summary>
        /// <param name="userManager">User manager.</param>
        /// <param name="signInManager">Signin manager.</param>
        public ConfirmEmailChangeModel([NotNull] UserManager<IdentityUser> userManager, [NotNull] SignInManager<IdentityUser> signInManager)
        {
            this.userManager = userManager;
            this.signInManager = signInManager;
        }

        /// <summary>
        /// Gets or sets status messasge.
        /// </summary>
        [TempData]
        public string StatusMessage { get; set; }

        /// <summary>
        /// On get method handler.
        /// </summary>
        /// <param name="userId">User ID.</param>
        /// <param name="email">User email address.</param>
        /// <param name="code">Confirmation code.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public async Task<IActionResult> OnGetAsync(string userId, string email, string code)
        {
            if (userId == null || email == null || code == null)
            {
                return RedirectToPage("/Index");
            }

            var user = await userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return NotFound($"Unable to load user with ID '{userId}'.");
            }

            code = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(code));
            var result = await userManager.ChangeEmailAsync(user, email, code);
            if (!result.Succeeded)
            {
                StatusMessage = "Error changing email.";
                return Page();
            }

            // In our UI email and user name are one and the same, so when we update the email
            // we need to update the user name.
            var setUserNameResult = await userManager.SetUserNameAsync(user, email);
            if (!setUserNameResult.Succeeded)
            {
                StatusMessage = "Error changing user name.";
                return Page();
            }

            await signInManager.RefreshSignInAsync(user);
            StatusMessage = "Thank you for confirming your email change.";
            return Page();
        }
    }
}
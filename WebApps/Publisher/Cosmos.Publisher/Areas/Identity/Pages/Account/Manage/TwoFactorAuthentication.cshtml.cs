// <copyright file="TwoFactorAuthentication.cshtml.cs" company="Moonrise Software, LLC">
// Copyright (c) Moonrise Software, LLC. All rights reserved.
// Licensed under the GNU Public License, Version 3.0 (https://www.gnu.org/licenses/gpl-3.0.html)
// See https://github.com/MoonriseSoftwareCalifornia/CosmosCMS
// for more information concerning the license and the contributors participating to this project.
// </copyright>

namespace Cosmos.Cms.Areas.Identity.Pages.Account.Manage
{
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Identity;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.AspNetCore.Mvc.RazorPages;

    /// <summary>
    /// Two factor authentication page model.
    /// </summary>
    public class TwoFactorAuthenticationModel : PageModel
    {
        private const string AuthenicatorUriFormat = "otpauth://totp/{0}:{1}?secret={2}&issuer={0}";
        private readonly SignInManager<IdentityUser> signInManager;

        private readonly UserManager<IdentityUser> userManager;

        /// <summary>
        /// Initializes a new instance of the <see cref="TwoFactorAuthenticationModel"/> class.
        /// </summary>
        /// <param name="userManager">User manager.</param>
        /// <param name="signInManager">Sign in manager.</param>
        public TwoFactorAuthenticationModel(
            UserManager<IdentityUser> userManager,
            SignInManager<IdentityUser> signInManager)
        {
            this.userManager = userManager;
            this.signInManager = signInManager;
        }

        /// <summary>
        /// Gets or sets a value indicating whether user has authenticator.
        /// </summary>
        public bool HasAuthenticator { get; set; }

        /// <summary>
        /// Gets or sets remaining recovery codes.
        /// </summary>
        public int RecoveryCodesLeft { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether two factor is enabled.
        /// </summary>
        [BindProperty]
        public bool Is2faEnabled { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether is machine remembered.
        /// </summary>
        public bool IsMachineRemembered { get; set; }

        /// <summary>
        /// Gets or sets status mesage.
        /// </summary>
        [TempData]
        public string StatusMessage { get; set; }

        /// <summary>
        /// GET method handler.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public async Task<IActionResult> OnGet()
        {
            var user = await userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound($"Unable to load user with ID '{userManager.GetUserId(User)}'.");
            }

            HasAuthenticator = await userManager.GetAuthenticatorKeyAsync(user) != null;
            Is2faEnabled = await userManager.GetTwoFactorEnabledAsync(user);
            IsMachineRemembered = await signInManager.IsTwoFactorClientRememberedAsync(user);
            RecoveryCodesLeft = await userManager.CountRecoveryCodesAsync(user);

            return Page();
        }

        /// <summary>
        /// POST method handler.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public async Task<IActionResult> OnPost()
        {
            var user = await userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound($"Unable to load user with ID '{userManager.GetUserId(User)}'.");
            }

            await signInManager.ForgetTwoFactorClientAsync();
            StatusMessage =
                "The current browser has been forgotten. When you login again from this browser you will be prompted for your 2fa code.";
            return RedirectToPage();
        }
    }
}
// <copyright file="LoginWithRecoveryCode.cshtml.cs" company="Moonrise Software, LLC">
// Copyright (c) Moonrise Software, LLC. All rights reserved.
// Licensed under the GNU Public License, Version 3.0 (https://www.gnu.org/licenses/gpl-3.0.html)
// See https://github.com/MoonriseSoftwareCalifornia/CosmosCMS
// for more information concerning the license and the contributors participating to this project.
// </copyright>

namespace Cosmos.Cms.Areas.Identity.Pages.Account
{
    using System;
    using System.ComponentModel.DataAnnotations;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Identity;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.AspNetCore.Mvc.RazorPages;
    using Microsoft.Extensions.Logging;

    /// <summary>
    /// Login with recovery code model.
    /// </summary>
    [AllowAnonymous]
    public class LoginWithRecoveryCodeModel : PageModel
    {
        private readonly ILogger<LoginWithRecoveryCodeModel> logger;
        private readonly SignInManager<IdentityUser> signInManager;

        /// <summary>
        /// Initializes a new instance of the <see cref="LoginWithRecoveryCodeModel"/> class.
        /// </summary>
        /// <param name="signInManager">Sign in manager.</param>
        /// <param name="logger">Logger.</param>
        public LoginWithRecoveryCodeModel(SignInManager<IdentityUser> signInManager, ILogger<LoginWithRecoveryCodeModel> logger)
        {
            this.signInManager = signInManager;
            this.logger = logger;
        }

        /// <summary>
        /// Gets or sets input model.
        /// </summary>
        [BindProperty]
        public InputModel Input { get; set; }

        /// <summary>
        /// Gets or sets return URL.
        /// </summary>
        public string ReturnUrl { get; set; }

        /// <summary>
        /// On get method handler.
        /// </summary>
        /// <param name="returnUrl">Return URL to pass along.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public async Task<IActionResult> OnGetAsync(string returnUrl = null)
        {
            // Ensure the user has gone through the username & password screen first
            var user = await signInManager.GetTwoFactorAuthenticationUserAsync();
            if (user == null)
            {
                throw new InvalidOperationException("Unable to load two-factor authentication user.");
            }

            ReturnUrl = returnUrl;

            return Page();
        }

        /// <summary>
        /// On post method handler.
        /// </summary>
        /// <param name="returnUrl">Return URL to pass along.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public async Task<IActionResult> OnPostAsync(string returnUrl = null)
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            var user = await signInManager.GetTwoFactorAuthenticationUserAsync();
            if (user == null)
            {
                throw new InvalidOperationException("Unable to load two-factor authentication user.");
            }

            var recoveryCode = Input.RecoveryCode.Replace(" ", string.Empty);

            var result = await signInManager.TwoFactorRecoveryCodeSignInAsync(recoveryCode);

            if (result.Succeeded)
            {
                logger.LogInformation("User with ID '{UserId}' logged in with a recovery code.", user.Id);
                return LocalRedirect(returnUrl ?? Url.Content("~/"));
            }

            if (result.IsLockedOut)
            {
                logger.LogWarning("User with ID '{UserId}' account locked out.", user.Id);
                return RedirectToPage("./Lockout");
            }

            logger.LogWarning("Invalid recovery code entered for user with ID '{UserId}' ", user.Id);
            ModelState.AddModelError(string.Empty, "Invalid recovery code entered.");
            return Page();
        }

        /// <summary>
        /// Input model.
        /// </summary>
        public class InputModel
        {
            /// <summary>
            /// Gets or sets recovery code.
            /// </summary>
            [BindProperty]
            [Required]
            [DataType(DataType.Text)]
            [Display(Name = "Recovery Code")]
            public string RecoveryCode { get; set; }
        }
    }
}
// <copyright file="ExternalLogin.cshtml.cs" company="Moonrise Software, LLC">
// Copyright (c) Moonrise Software, LLC. All rights reserved.
// Licensed under the GNU Public License, Version 3.0 (https://www.gnu.org/licenses/gpl-3.0.html)
// See https://github.com/MoonriseSoftwareCalifornia/CosmosCMS
// for more information concerning the license and the contributors participating to this project.
// </copyright>

namespace Cosmos.Cms.Areas.Identity.Pages.Account
{
    using System.ComponentModel.DataAnnotations;
    using System.Linq;
    using System.Security.Claims;
    using System.Text;
    using System.Text.Encodings.Web;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Identity;
    using Microsoft.AspNetCore.Identity.UI.Services;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.AspNetCore.Mvc.RazorPages;
    using Microsoft.AspNetCore.RateLimiting;
    using Microsoft.AspNetCore.WebUtilities;
    using Microsoft.Extensions.Logging;

    /// <summary>
    /// External login page model.
    /// </summary>
    [AllowAnonymous]
    [EnableRateLimiting("fixed")]
    public class ExternalLoginModel : PageModel
    {
        private readonly IEmailSender emailSender;
        private readonly ILogger<ExternalLoginModel> logger;
        private readonly SignInManager<IdentityUser> signInManager;
        private readonly UserManager<IdentityUser> userManager;

        /// <summary>
        /// Initializes a new instance of the <see cref="ExternalLoginModel"/> class.
        /// </summary>
        /// <param name="signInManager">Sign in manager.</param>
        /// <param name="userManager">User Manager.</param>
        /// <param name="logger">Logger.</param>
        /// <param name="emailSender">Email sender.</param>
        public ExternalLoginModel(
            SignInManager<IdentityUser> signInManager,
            UserManager<IdentityUser> userManager,
            ILogger<ExternalLoginModel> logger,
            IEmailSender emailSender)
        {
            this.signInManager = signInManager;
            this.userManager = userManager;
            this.logger = logger;
            this.emailSender = emailSender;
        }

        /// <summary>
        /// Gets or sets input model.
        /// </summary>
        [BindProperty]
        public InputModel Input { get; set; }

        /// <summary>
        /// Gets or sets provider display name.
        /// </summary>
        public string ProviderDisplayName { get; set; }

        /// <summary>
        /// Gets or sets return url.
        /// </summary>
        public string ReturnUrl { get; set; }

        /// <summary>
        /// Gets or sets error message.
        /// </summary>
        [TempData]
        public string ErrorMessage { get; set; }

        /// <summary>
        /// On get method handler.
        /// </summary>
        /// <returns>Returns an <see cref="IActionResult"/>.</returns>
        public IActionResult OnGetAsync()
        {
            return RedirectToPage("./Login");
        }

        /// <summary>
        /// On post method handler.
        /// </summary>
        /// <param name="provider">Login provider name.</param>
        /// <param name="returnUrl">Return URL to pass along.</param>
        /// <returns>Returns an <see cref="IActionResult"/>.</returns>
        public IActionResult OnPost(string provider, string returnUrl = null)
        {
            // Request a redirect to the external login provider.
            var redirectUrl = Url.Page("./ExternalLogin", "Callback", new { returnUrl });
            var properties = signInManager.ConfigureExternalAuthenticationProperties(provider, redirectUrl);

            // need to modify this to ensure return URL is https
            // return new ChallengeResult(provider, properties);
            var challenge = new ChallengeResult(provider, properties);

            if (!string.IsNullOrEmpty(challenge.Properties.RedirectUri) &&
                challenge.Properties.RedirectUri.StartsWith("http:"))
            {
                challenge.Properties.RedirectUri = challenge.Properties.RedirectUri.Replace("http:", "https:");
            }

            return challenge;
        }

        /// <summary>
        /// On get callback method handler.
        /// </summary>
        /// <param name="returnUrl">Return URL to pass along.</param>
        /// <param name="remoteError">Remote login error.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public async Task<IActionResult> OnGetCallbackAsync(string returnUrl = null, string remoteError = null)
        {
            returnUrl = returnUrl ?? Url.Content("~/");
            if (remoteError != null)
            {
                ErrorMessage = $"Error from external provider: {remoteError}";
                return RedirectToPage("./Login", new { ReturnUrl = returnUrl });
            }

            var info = await signInManager.GetExternalLoginInfoAsync();
            if (info == null)
            {
                ErrorMessage = "Error loading external login information.";
                return RedirectToPage("./Login", new { ReturnUrl = returnUrl });
            }

            // Sign in the user with this external login provider if the user already has a login.
            var result =
                await signInManager.ExternalLoginSignInAsync(info.LoginProvider, info.ProviderKey, false, true);
            if (result.Succeeded)
            {
                logger.LogInformation("{Name} logged in with {LoginProvider} provider.", info.Principal.Identity.Name, info.LoginProvider);
                return LocalRedirect(returnUrl);
            }

            if (result.IsLockedOut)
            {
                return RedirectToPage("./Lockout");
            }

            // If the user does not have an account, then ask the user to create an account.
            ReturnUrl = returnUrl;
            ProviderDisplayName = info.ProviderDisplayName;
            if (info.Principal.HasClaim(c => c.Type == ClaimTypes.Email))
            {
                Input = new InputModel
                {
                    Email = info.Principal.FindFirstValue(ClaimTypes.Email)
                };

                var user = await userManager.FindByEmailAsync(Input.Email);

                if (user != null && await userManager.IsEmailConfirmedAsync(user) == false)
                {
                    ViewData["ShowResendConfirmEmail"] = true;
                }
            }

            return Page();
        }

        /// <summary>
        /// On post confirmation handler.
        /// </summary>
        /// <param name="returnUrl">Return URL to pass along.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public async Task<IActionResult> OnPostConfirmationAsync(string returnUrl = null)
        {
            returnUrl = returnUrl ?? Url.Content("~/");

            // Get the information about the user from the external login provider
            var info = await signInManager.GetExternalLoginInfoAsync();
            if (info == null)
            {
                ErrorMessage = "Error loading external login information during confirmation.";
                return RedirectToPage("./Login", new { ReturnUrl = returnUrl });
            }

            if (ModelState.IsValid)
            {
                var user = new IdentityUser { UserName = Input.Email, Email = Input.Email };

                var result = await userManager.CreateAsync(user);
                if (result.Succeeded)
                {
                    result = await userManager.AddLoginAsync(user, info);
                    if (result.Succeeded)
                    {
                        logger.LogInformation("User created an account using {Name} provider.", info.LoginProvider);

                        var userId = await userManager.GetUserIdAsync(user);
                        var code = await userManager.GenerateEmailConfirmationTokenAsync(user);
                        code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));
                        var callbackUrl = Url.Page(
                            "/Account/ConfirmEmail",
                            null,
                            new { area = "Identity", userId, code },
                            Request.Scheme);

                        await emailSender.SendEmailAsync(Input.Email, "Confirm your email", $"Please confirm your account by <a href='{HtmlEncoder.Default.Encode(callbackUrl)}'>clicking here</a>.");

                        // If account confirmation is required, we need to show the link if we don't have a real email sender
                        if (userManager.Options.SignIn.RequireConfirmedAccount)
                        {
                            return RedirectToPage("./RegisterConfirmation", new { Input.Email });
                        }

                        await signInManager.SignInAsync(user, false, info.LoginProvider);

                        return LocalRedirect(returnUrl);
                    }
                }

                if (result.Errors.Any(a => a.Code == "DuplicateUserName"))
                {
                    if (await userManager.IsEmailConfirmedAsync(user) == false)
                    {
                        ViewData["ShowResendConfirmEmail"] = true;
                    }
                }

                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }

            ProviderDisplayName = info.ProviderDisplayName;
            ReturnUrl = returnUrl;
            return Page();
        }

        /// <summary>
        /// Input model.
        /// </summary>
        public class InputModel
        {
            /// <summary>
            /// Gets or sets email address.
            /// </summary>
            [Required]
            [EmailAddress]
            public string Email { get; set; }
        }
    }
}
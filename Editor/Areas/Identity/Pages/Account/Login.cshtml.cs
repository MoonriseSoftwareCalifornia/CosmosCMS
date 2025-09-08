// <copyright file="Login.cshtml.cs" company="Moonrise Software, LLC">
// Copyright (c) Moonrise Software, LLC. All rights reserved.
// Licensed under the GNU Public License, Version 3.0 (https://www.gnu.org/licenses/gpl-3.0.html)
// See https://github.com/MoonriseSoftwareCalifornia/CosmosCMS
// for more information concerning the license and the contributors participating to this project.
// </copyright>

namespace Cosmos.Cms.Areas.Identity.Pages.Account
{
    using Cosmos.Cms.Common.Services.Configurations;
    using Cosmos.Common.Data;
    using Cosmos.Common.Services;
    using Cosmos.DynamicConfig;
    using Cosmos.Editor.Boot;
    using Cosmos.EmailServices;
    using Microsoft.AspNetCore.Authentication;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Identity;
    using Microsoft.AspNetCore.Identity.UI.Services;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.AspNetCore.Mvc.RazorPages;
    using Microsoft.AspNetCore.RateLimiting;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Options;
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    /// <summary>
    /// Login page model.
    /// </summary>
    [AllowAnonymous]
    [EnableRateLimiting("fixed")]
    public class LoginModel : PageModel
    {
        private readonly ILogger<LoginModel> logger;
        private readonly IOptions<SiteSettings> options;
        private readonly IServiceProvider services;
        private readonly bool isMultiTenant;

        /// <summary>
        /// Initializes a new instance of the <see cref="LoginModel"/> class.
        /// Constructor.
        /// </summary>
        /// <param name="logger">Log service.</param>
        /// <param name="options">Cosmos site options.</param>
        /// <param name="services">App services.</param>
        /// <param name="configuration">App configuration.</param>
        /// <param name="emailSender">Email sender service.</param>
        public LoginModel(
            ILogger<LoginModel> logger,
            IOptions<SiteSettings> options,
            IServiceProvider services,
            IConfiguration configuration,
            IEmailSender emailSender)
        {
            this.logger = logger;
            this.options = options;
            this.services = services;
            this.isMultiTenant = configuration.GetValue<bool?>("MultiTenantEditor") ?? false;
        }

        /// <summary>
        /// Gets or sets input model.
        /// </summary>
        [BindProperty]
        public InputModel Input { get; set; } = new InputModel();

        /// <summary>
        /// Gets or sets external logins.
        /// </summary>
        public IList<AuthenticationScheme> ExternalLogins { get; set; }

        /// <summary>
        /// Gets or sets page URL.
        /// </summary>
        public string ReturnUrl { get; set; }

        /// <summary>
        /// Gets or sets error message.
        /// </summary>
        [TempData]
        public string ErrorMessage { get; set; }

        private SignInManager<IdentityUser> SignInManager
        {
            get
            {
                var manager = services.GetService<SignInManager<IdentityUser>>();
                return manager;
            }
        }

        private UserManager<IdentityUser> UserManager
        {
            get
            {
                var manager = services.GetService<UserManager<IdentityUser>>();
                return manager;
            }
        }

        private ApplicationDbContext DbContext
        {
            get
            {
                var manager = services.GetService<ApplicationDbContext>();
                return manager;
            }
        }

        /// <summary>
        /// On get method handler.
        /// </summary>
        /// <param name="returnUrl">Return URL after logging in (optional).</param>
        /// <param name="website">Website domain name for TOTP (optional).</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public async Task<IActionResult> OnGetAsync(string returnUrl = "", string website = "")
        {
            if (isMultiTenant && !IsDbContextConfigured())
            {
                return NotFound("This site is not yet configured. Please contact your administrator.");
            }

            // Get a clean return URL.
            returnUrl = await GetReturnUrl(returnUrl);

            //// Allow setup if enabled for a non-multitenant editor.
            if (options.Value.AllowSetup)
            {
                await DbContext.Database.EnsureCreatedAsync();
            }

            // If the user is already logged in, redirect to the home page or return URL.
            if (User.Identity.IsAuthenticated)
            {
                if (string.IsNullOrWhiteSpace(returnUrl))
                {
                    RedirectToAction("Index", "Home");
                }
                else
                {
                    return Redirect(returnUrl);
                }
            }

            ExternalLogins = (await SignInManager.GetExternalAuthenticationSchemesAsync()).ToList();

            // If there are no users yet, go strait to the register page.
            var userCount = await DbContext.Users.CountAsync();
            if (userCount == 0)
            {
                return RedirectToPage("Register");
            }

            // Check for one time passworld token in the query string.
            var totpToken = Request.Query["ccmsopt"].ToString().Trim('"');
            if (!string.IsNullOrWhiteSpace(totpToken))
            {
                // Process the TOTP token.
                return await ProcessTotp(returnUrl, totpToken, website);
            }

            return Page();
        }

        /// <summary>
        /// On post method handler.
        /// </summary>
        /// <param name="returnUrl">Return to page.</param>
        /// <param name="website">This is used by the dynamic db config.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public async Task<IActionResult> OnPostAsync(string returnUrl = "", string website = "")
        {
            returnUrl = await GetReturnUrl(returnUrl);
                        
            ExternalLogins = (await SignInManager.GetExternalAuthenticationSchemesAsync()).ToList();

            if (ModelState.IsValid)
            {
                // This doesn't count login failures towards account lockout
                // To enable password failures to trigger account lockout, set lockoutOnFailure: true
                var result =
                    await SignInManager.PasswordSignInAsync(Input.Email, Input.Password, Input.RememberMe, false);

                if (result.Succeeded)
                {
                    if (!User.IsInRole("Reviewers") && !User.IsInRole("Authors") && !User.IsInRole("Editors") &&
                        !User.IsInRole("Administrators"))
                    {
                        ViewData["AccessPending"] = true;
                        return Page();
                    }

                    logger.LogInformation("User logged in.");
                    var test = SignInManager.IsSignedIn(User);
                    return LocalRedirect(returnUrl);
                }

                if (result.RequiresTwoFactor)
                {
                    return RedirectToPage("./LoginWith2fa", new { ReturnUrl = returnUrl, Input.RememberMe });
                }

                if (result.IsLockedOut)
                {
                    logger.LogWarning("User account locked out.");
                    return RedirectToPage("./Lockout");
                }

                ModelState.AddModelError("Input.Password", "Invalid password.");
            }

            // If we got this far, something failed, redisplay form
            return Page();
        }

        private async Task<IActionResult> ProcessTotp(string returnUrl, string totpToken, string website)
        {
            if (string.IsNullOrWhiteSpace(totpToken))
            {
                ModelState.AddModelError(string.Empty, "Invalid login link.");
                return Page();
            }

            var email = Request.Query["ccmsemail"].ToString();
            if (string.IsNullOrWhiteSpace(email))
            {
                ModelState.AddModelError(string.Empty, "Invalid email address.");
                return Page();
            }

            var user = await UserManager.FindByEmailAsync(email);
            if (user == null)
            {
                ModelState.AddModelError(string.Empty, "User not found.");
                return Page();
            }

            if (user.EmailConfirmed == false)
            {
                ModelState.AddModelError(string.Empty, "Email address not confirmed.");
                return Page();
            }

            if (user.LockoutEnabled && user.LockoutEnd.HasValue && user.LockoutEnd > DateTimeOffset.UtcNow)
            {
                ModelState.AddModelError(string.Empty, "User account is locked out.");
                return Page();
            }

            var totpProvider = new OneTimeTokenProvider<IdentityUser>(DbContext, logger);
            var result = await totpProvider.ValidateAsync(totpToken, user, true);

            if (result == OneTimeTokenProvider<IdentityUser>.VerificationResult.Valid)
            {
                await SignInManager.SignInAsync(user, true, "TOTP");

                var principal = await SignInManager.CreateUserPrincipalAsync(user);

                if (SignInManager.IsSignedIn(principal))
                {
                    return LocalRedirect(returnUrl);
                }
            }

            if (result == OneTimeTokenProvider<IdentityUser>.VerificationResult.Invalid)
            {
                ModelState.AddModelError(string.Empty, "Invalid login link.");
                return Page();
            }
            else if (result == OneTimeTokenProvider<IdentityUser>.VerificationResult.Expired)
            {
                ModelState.AddModelError(string.Empty, "Login link has expired.");
                return Page();
            }

            ModelState.AddModelError(string.Empty, "Could not log in user account.");
            return Page();
        }

        /// <summary>
        /// Gets the return URL.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        private async Task<string> GetReturnUrl(string returnUrl)
        {
            if (!await DbContext.Articles.CosmosAnyAsync())
            {
                return "/";
            }

            return string.IsNullOrWhiteSpace(returnUrl) ? "/" : returnUrl.Replace("http://", "https://");
        }

        private bool IsDbContextConfigured()
        {
            // ProviderName is set when the database is configured
            try
            {
                var databaseProvider = DbContext?.Database.ProviderName;
                if (string.IsNullOrEmpty(databaseProvider))
                {
                    return false;
                }
            }
            catch
            {
                return false;
            }

            return true;
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

            /// <summary>
            /// Gets or sets password.
            /// </summary>
            [Required]
            [DataType(DataType.Password)]
            public string Password { get; set; }

            /// <summary>
            /// Gets or sets website domain name.
            /// </summary>
            [Display(Name = "Website Domain Name")]
            public string WebsiteDomainName { get; set; } = string.Empty;

            /// <summary>
            /// Gets or sets a value indicating whether remember me.
            /// </summary>
            [Display(Name = "Remember me?")]
            public bool RememberMe { get; set; }

            /// <summary>
            /// Gets or sets a value indicating whether to use TOTP (Time-based One-Time Password).
            /// </summary>
            [Display(Name = "Send login link via email?")]
            public bool UseTotp { get; set; } = false;
        }
    }
}
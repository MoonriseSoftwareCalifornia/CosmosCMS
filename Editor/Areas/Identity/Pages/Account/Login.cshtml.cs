// <copyright file="Login.cshtml.cs" company="Moonrise Software, LLC">
// Copyright (c) Moonrise Software, LLC. All rights reserved.
// Licensed under the GNU Public License, Version 3.0 (https://www.gnu.org/licenses/gpl-3.0.html)
// See https://github.com/MoonriseSoftwareCalifornia/CosmosCMS
// for more information concerning the license and the contributors participating to this project.
// </copyright>

namespace Cosmos.Cms.Areas.Identity.Pages.Account
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.Linq;
    using System.Threading.Tasks;
    using Cosmos.Cms.Common.Services.Configurations;
    using Cosmos.Common.Data;
    using Cosmos.DynamicConfig;
    using Microsoft.AspNetCore.Authentication;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Identity;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.AspNetCore.Mvc.RazorPages;
    using Microsoft.AspNetCore.RateLimiting;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Options;

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
        private readonly IConfiguration configuration;
        private readonly bool isMultiTenantEditor;

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
        /// Initializes a new instance of the <see cref="LoginModel"/> class.
        /// Constructor.
        /// </summary>
        /// <param name="logger">Log service.</param>
        /// <param name="options">Cosmos site options.</param>
        /// <param name="services">App services.</param>
        /// <param name="configuration">App configuration.</param>
        public LoginModel(
            ILogger<LoginModel> logger,
            IOptions<SiteSettings> options,
            IServiceProvider services,
            IConfiguration configuration)
        {
            this.logger = logger;
            this.options = options;
            this.services = services;
            this.configuration = configuration;
            isMultiTenantEditor = this.configuration.GetValue<bool?>("MultiTenantEditor") ?? false;
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

        /// <summary>
        /// On get method handler.
        /// </summary>
        /// <param name="returnUrl">Return URL after logging in (optional).</param>
        /// <param name="website">Website DNS name (optional).</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public async Task<IActionResult> OnGetAsync(string returnUrl = "", string website = "")
        {
            // Get a clean return URL.
            returnUrl = await GetReturnUrl(returnUrl);

            // Clear the existing external cookie to ensure a clean login process
            await SignInManager.SignOutAsync();
            await HttpContext.SignOutAsync(IdentityConstants.ExternalScheme);

            // Get the cookie value for the website domain name.
            var cookieValue = Request.Cookies[DynamicConfigurationProvider.StandardCookieName];

            if (isMultiTenantEditor)
            {
                var cookieExists = string.IsNullOrWhiteSpace(cookieValue) == false;
                var isValidDomain = string.IsNullOrWhiteSpace(website) ? true : await DynamicConfigurationProvider.ValidateDomainName(configuration, website);
                website = isValidDomain ? website : string.Empty;

                // If person is already logged in or the cookie exists, reset and restart.
                if (cookieExists || isValidDomain == false)
                {
                    // Reset the login if the cookie exists or the user is logged in.
                    // Also reset the website to nothing if it is not valid.
                    return ResetLogin(cookieExists, returnUrl, website);
                }

                var futureWebsite = DynamicConfigurationProvider.GetTenantDomainNameFromRequest(configuration, HttpContext, false);
                Input.NeedsCookieSet = true;
                Input.WebsiteDomainName = futureWebsite;

                return Page();
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

            // Allow setup if enabled for a non-multitenant editor.
            if (!isMultiTenantEditor && options.Value.AllowSetup)
            {
                await DbContext.Database.EnsureCreatedAsync();
            }

            // If there are no users yet, go strait to the register page.
            if (await UserManager.Users.CountAsync() == 0)
            {
                return RedirectToPage("Register");
            }
            else
            {
                return Page();
            }
        }

        /// <summary>
        /// On post method handler.
        /// </summary>
        /// <param name="returnUrl">Return to page.</param>
        /// <param name="website">Website domain name.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public async Task<IActionResult> OnPostAsync(string returnUrl = null, string website = null)
        {
            ViewData["IsMultiTenantEditor"] = isMultiTenantEditor;
            returnUrl = await GetReturnUrl(returnUrl);

            if (Input.NeedsCookieSet)
            {
                if (string.IsNullOrWhiteSpace(Input.WebsiteDomainName))
                {
                    ModelState.AddModelError(string.Empty, "Please enter a valid domain name.");
                    return Page();
                }

                Input.WebsiteDomainName = DynamicConfigurationProvider.CleanUpDomainName(Input.WebsiteDomainName);

                // Automatically add the website domain name if given in the query string.
                if (await DynamicConfigurationProvider.ValidateDomainName(configuration, Input.WebsiteDomainName))
                {
                    // Automatically add the website domain name if given in the query string.
                    Response.Cookies.Append(DynamicConfigurationProvider.StandardCookieName, Input.WebsiteDomainName);
                    Input.NeedsCookieSet = false;
                }
                else
                {
                    ModelState.AddModelError("Input.WebsiteDomainName", "Please enter a valid domain name.");
                }

                return Page();
            }

            if (isMultiTenantEditor)
            {
                var database = configuration.GetValue<string>($"CosmosIdentityDbName") ?? "cosmoscms";
                var databaseStatus = ApplicationDbContext.EnsureDatabaseExists(DbContext, false, database);
                if (databaseStatus == DbStatus.DoesNotExist)
                {
                    ModelState.AddModelError(string.Empty, "Database not setup. Please setup using the multi-tenant administrator.");
                    return Page();
                }

                if (databaseStatus == DbStatus.ExistsWithMissingContainers)
                {
                    ModelState.AddModelError(string.Empty, "Database setup not complete. Complete setup using the multi-tenant administrator.");
                    return Page();
                }

                if (databaseStatus == DbStatus.ExistsWithNoUsers)
                {
                    return RedirectToPage("Register");
                }
            }

            ExternalLogins = (await SignInManager.GetExternalAuthenticationSchemesAsync()).ToList();

            if (ModelState.IsValid)
            {
                // This doesn't count login failures towards account lockout
                // To enable password failures to trigger account lockout, set lockoutOnFailure: true
                var result =
                    await SignInManager.PasswordSignInAsync(Input.Email, Input.Password, Input.RememberMe, false);
                if (result.Succeeded)
                {
                    logger.LogInformation("User logged in.");
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

                ModelState.AddModelError("Input.Password", "Invalid login attempt.");
            }

            // If we got this far, something failed, redisplay form
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

        /// <summary>
        /// Resets the login removing the cookie or existing logged in session.
        /// </summary>
        /// <param name="cookieExists">Cookie already exists.</param>
        /// <param name="returnUrl">Return URL.</param>
        /// <param name="website">Website domain name.</param>
        /// <returns>IActionResult.</returns>
        private IActionResult ResetLogin(bool cookieExists, string returnUrl, string website)
        {
            Response.Cookies.Delete(DynamicConfigurationProvider.StandardCookieName);

            if (string.IsNullOrWhiteSpace(website))
            {
                return RedirectToPage("Login", new { returnUrl });
            }

            return RedirectToPage("Login", new { returnUrl, website });
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
            /// Gets or sets a value indicating whether the cookie needs to be set.
            /// </summary>
            public bool NeedsCookieSet { get; set; } = false;

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
        }
    }
}
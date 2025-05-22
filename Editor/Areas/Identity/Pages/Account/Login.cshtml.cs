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
            returnUrl = await GetReturnUrl(returnUrl);

            if (!string.IsNullOrEmpty(ErrorMessage))
            {
                ModelState.AddModelError(string.Empty, ErrorMessage);
            }

            if (User.Identity.IsAuthenticated)
            {
                return RedirectToAction(returnUrl);
            }

            if (isMultiTenantEditor)
            {
                Input.NeedsCookieSet = await NeedsAccountCookieSet();

                Input.WebsiteDomainName = DynamicConfigurationProvider.GetTenantDomainNameFromRequest(configuration, HttpContext);

                if (Input.NeedsCookieSet)
                {
                    // We need to get the account set before processing the rest.
                    return Page();
                }
            }

            // Clear the existing external cookie to ensure a clean login process
            await HttpContext.SignOutAsync(IdentityConstants.ExternalScheme);

            ExternalLogins = (await SignInManager.GetExternalAuthenticationSchemesAsync()).ToList();

            // If there are no users yet, go strait to the register page.
            if (options.Value.AllowSetup)
            {
                await DbContext.Database.EnsureCreatedAsync();
            }

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
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public async Task<IActionResult> OnPostAsync(string returnUrl = null)
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
                var databaseStatus = ApplicationDbContext.EnsureDatabaseExists(DbContext, false);
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
        /// Indicates if the account cookie needs to be set.
        /// </summary>
        /// <returns>True or false.</returns>
        /// <remarks>This also sets the ViewData['NeedsAccountCookieSet'] so that the view knows what to show.</remarks>
        private async Task<bool> NeedsAccountCookieSet()
        {
            if (!isMultiTenantEditor)
            {
                return false;
            }

            var domainName = DynamicConfigurationProvider.GetTenantDomainNameFromCookieOrHost(configuration, HttpContext);
            if (await DynamicConfigurationProvider.ValidateDomainName(configuration, domainName))
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
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
    using System.Text;
    using System.Threading.Tasks;
    using Cosmos.Cms.Common.Services.Configurations;
    using Cosmos.Common.Data;
    using Cosmos.Common.Services;
    using Cosmos.DynamicConfig;
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
        private readonly ICosmosEmailSender emailSender;

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
            this.configuration = configuration;
            isMultiTenantEditor = this.configuration.GetValue<bool?>("MultiTenantEditor") ?? false;
            this.emailSender = emailSender as ICosmosEmailSender ?? throw new ArgumentNullException(nameof(emailSender), "Email sender cannot be null.");
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

            HttpContext.Response.Cookies.Delete(DynamicConfigurationProvider.StandardCookieName);

            // Get a clean return URL.
            returnUrl = await GetReturnUrl(returnUrl);

            ViewData["IsMultiTenantEditor"] = isMultiTenantEditor;

            //// Allow setup if enabled for a non-multitenant editor.
            if (!isMultiTenantEditor && options.Value.AllowSetup)
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

            if (isMultiTenantEditor)
            {
                // Get the cookie value for the website domain name.
                Input.WebsiteDomainName = DynamicConfigurationProvider.GetTenantDomainNameFromRequest(configuration, HttpContext);

                if (!await DynamicConfigurationProvider.ValidateDomainName(configuration, Input.WebsiteDomainName))
                {
                    Input.WebsiteDomainName = string.Empty;
                    HttpContext.Response.Cookies.Delete(DynamicConfigurationProvider.StandardCookieName);
                }
            }

            ExternalLogins = (await SignInManager.GetExternalAuthenticationSchemesAsync()).ToList();

            // If there are no users yet, go strait to the register page.
            if (!isMultiTenantEditor && await UserManager.Users.CountAsync() == 0)
            {
                return RedirectToPage("Register");
            }

            // Check for one time passworld token in the query string.
            var totpToken = Request.Query["ccmsopt"].ToString().Trim('"');
            if (!string.IsNullOrWhiteSpace(totpToken))
            {
                if (string.IsNullOrWhiteSpace(website))
                {
                    ModelState.AddModelError(string.Empty, "Website domain name is required for TOTP login.");
                    return Page();
                }

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
            ViewData["IsMultiTenantEditor"] = isMultiTenantEditor;
            returnUrl = await GetReturnUrl(returnUrl);
                        
            if (isMultiTenantEditor)
            {
                if (string.IsNullOrWhiteSpace(Input.WebsiteDomainName))
                {
                    ModelState.AddModelError("Input.WebsiteDomainName", "Please enter youe website's domain name.");
                    return Page();
                }

                var domainName = DynamicConfigurationProvider.GetTenantDomainNameFromRequest(configuration, HttpContext);
                if (string.IsNullOrWhiteSpace(domainName))
                {
                    throw new InvalidOperationException("Website domain name is required for multi-tenant editor.");
                }

                if (!await DynamicConfigurationProvider.ValidateDomainNameAndEmailAddress(configuration, Input.WebsiteDomainName, Input.Email))
                {
                    ModelState.AddModelError("Input.Email", $"Email address '{Input.Email}' for website '{Input.WebsiteDomainName}' not found.");
                    return Page();
                }

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
                if (Input.UseTotp)
                {
                    var user = await UserManager.FindByEmailAsync(Input.Email);
                    if (user != null)
                    {
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

                        var db = Editor.Data.ApplicationDbContextUtilities.GetDbContextForDomain(Input.WebsiteDomainName, services);

                        var totpProvider = new OneTimeTokenProvider<IdentityUser>(db, logger);
                        var token = await totpProvider.GenerateAsync(user);
                        if (token != null)
                        {
                            var port = Request.Host.Port.HasValue ? $":{Request.Host.Port.Value}" : string.Empty;
                            
                            var link = $"https://{Request.Host.Host}";
                            if (!string.IsNullOrWhiteSpace(port) && port != ":80" && port != ":443")
                            {
                                link += port;
                            }

                            link += $"/Identity/Account/Login?ccmsopt={token}&ccmsemail={Uri.EscapeDataString(user.Email)}&website={Uri.EscapeDataString(Input.WebsiteDomainName)}&returnUrl={Uri.EscapeDataString(returnUrl)}";

                            var msg = new StringBuilder();
                            msg.AppendLine($"<p><a href='{link}'>Click this link</a> to log into your website.</p>");
                            msg.AppendLine($"<p>This link is active for 20 minutes and can only be used once.</p>");
                            msg.AppendLine("<p>If you did not request this, please contact your website administrator.</p>");
                            msg.AppendLine($"<p></p>");
                            msg.AppendLine($"<p>If the link does not work, paste the following URL into your web browser:</p>");
                            msg.AppendLine($"<p>{link}</p>");

                            // Send the token via email.
                            await this.emailSender.SendEmailAsync(
                                    Input.Email,
                                    "Login Link",
                                    msg.ToString());

                            logger.LogInformation($"Login link sent to user '{Input.Email}' via email.");

                            ViewData["TOTP_Sent"] = true;
                            return Page();
                        }
                        else
                        {
                            ModelState.AddModelError(string.Empty, "Failed to generate login link.");
                            return Page();
                        }
                    }
                    else
                    {
                        ModelState.AddModelError("Input_Email", "Invalid email address.");
                        return Page();
                    }
                }

                // This doesn't count login failures towards account lockout
                // To enable password failures to trigger account lockout, set lockoutOnFailure: true
                var result =
                    await SignInManager.PasswordSignInAsync(Input.Email, Input.Password, Input.RememberMe, false);
                if (result.Succeeded)
                {
                    if (isMultiTenantEditor)
                    {
                        HttpContext.Response.Cookies.Delete(DynamicConfigurationProvider.StandardCookieName);

                        // Set the cookie for the website domain name.
                        HttpContext.Response.Cookies.Append(
                            DynamicConfigurationProvider.StandardCookieName,
                            Input.WebsiteDomainName,
                            new CookieOptions
                            {
                                HttpOnly = true,
                                Secure = true
                            });
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
            var result = await totpProvider.ValidateAsync(totpToken, UserManager, user, true);

            if (result == OneTimeTokenProvider<IdentityUser>.VerificationResult.Valid)
            {
                await SignInManager.SignInAsync(user, true, "TOTP");

                var principal = await SignInManager.CreateUserPrincipalAsync(user);


                if (SignInManager.IsSignedIn(principal))
                {
                    if (isMultiTenantEditor)
                    {
                        HttpContext.Response.Cookies.Delete(DynamicConfigurationProvider.StandardCookieName);

                        // Set the cookie for the website domain name.
                        HttpContext.Response.Cookies.Append(
                            DynamicConfigurationProvider.StandardCookieName,
                            website,
                            new CookieOptions
                            {
                                HttpOnly = true,
                                Secure = true
                            });
                    }

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
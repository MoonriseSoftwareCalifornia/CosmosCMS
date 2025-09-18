// <copyright file="Register.cshtml.cs" company="Moonrise Software, LLC">
// Copyright (c) Moonrise Software, LLC. All rights reserved.
// Licensed under the GNU Public License, Version 3.0 (https://www.gnu.org/licenses/gpl-3.0.html)
// See https://github.com/MoonriseSoftwareCalifornia/CosmosCMS
// for more information concerning the license and the contributors participating to this project.
// </copyright>

namespace Sky.Cms.Areas.Identity.Pages.Account
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using Cosmos.Cms.Common.Services.Configurations;
    using Cosmos.Cms.Data;
    using Cosmos.Common.Data;
    using Cosmos.DynamicConfig;
    using Cosmos.Editor.Services;
    using Cosmos.EmailServices;
    using Microsoft.AspNetCore.Authentication;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Identity;
    using Microsoft.AspNetCore.Identity.UI.Services;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.AspNetCore.Mvc.RazorPages;
    using Microsoft.AspNetCore.RateLimiting;
    using Microsoft.AspNetCore.WebUtilities;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Options;
    using NUglify.Helpers;

    /// <summary>
    /// Register page model.
    /// </summary>
    [AllowAnonymous]
    [EnableRateLimiting("fixed")]
    public class RegisterModel : PageModel
    {
        private readonly ICosmosEmailSender emailSender;
        private readonly ILogger<RegisterModel> logger;
        private readonly IOptions<SiteSettings> options;
        private readonly IServiceProvider services;

        /// <summary>
        /// Initializes a new instance of the <see cref="RegisterModel"/> class.
        /// Constructor.
        /// </summary>
        /// <param name="logger">Log service.</param>
        /// <param name="options">Cosmos site options.</param>
        /// <param name="services">App services.</param>
        /// <param name="configuration">App configuration.</param>
        /// <param name="emailSender">Email sender service.</param>
        public RegisterModel(
            ILogger<RegisterModel> logger,
            IOptions<SiteSettings> options,
            IServiceProvider services,
            IConfiguration configuration,
            IEmailSender emailSender)
        {
            this.logger = logger;
            this.options = options;
            this.services = services;
            this.emailSender = (ICosmosEmailSender)emailSender;
        }

        /// <summary>
        /// Gets or sets page input model.
        /// </summary>
        [BindProperty]
        public InputModel Input { get; set; } = new InputModel();

        /// <summary>
        /// Gets or sets return URL.
        /// </summary>
        public string ReturnUrl { get; set; }

        /// <summary>
        /// Gets or sets external logins.
        /// </summary>
        public IList<AuthenticationScheme> ExternalLogins { get; set; }

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

        private RoleManager<IdentityRole> RoleManager
        {
            get
            {
                var manager = services.GetService<RoleManager<IdentityRole>>();
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
        /// GET method handler.
        /// </summary>
        /// <param name="returnUrl">Returns a <see cref="PageResult"/>.</param>
        /// <param name="website">Website DNS name.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public async Task<IActionResult> OnGetAsync(string returnUrl = null, string website = "")
        {
            ReturnUrl = returnUrl;
            ExternalLogins = (await SignInManager.GetExternalAuthenticationSchemesAsync()).ToList();

            ViewData["ShowLogin"] = await UserManager.Users.CosmosAnyAsync();

            return Page();
        }

        /// <summary>
        /// POST method handler.
        /// </summary>
        /// <param name="returnUrl">Returns a <see cref="PageResult"/>.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public async Task<IActionResult> OnPostAsync(string returnUrl = null)
        {
            returnUrl = returnUrl ?? Url.Content("~/");

            ExternalLogins = (await SignInManager.GetExternalAuthenticationSchemesAsync()).ToList();

            if (!Input.AgreeToTerms)
            {
                ModelState.AddModelError("Input.AgreeToTerms", "Please check box indicating you have read and agree with our terms and guidelines.");
            }

            if (ModelState.IsValid)
            {
                var t = DbContext.Database.EnsureCreatedAsync();
                t.Wait();

                var user = new IdentityUser { UserName = Input.Email, Email = Input.Email };
                var result = await UserManager.CreateAsync(user, Input.Password);
                if (result.Succeeded)
                {
                    logger.LogInformation("User created a new account with password.");

                    var newAdministrator = false;
                    var admins = await UserManager.GetUsersInRoleAsync(RequiredIdentityRoles.Administrators);
                    if (admins.Count == 0)
                    {
                        newAdministrator = await SetupNewAdministrator.Ensure_RolesAndAdmin_Exists(RoleManager, UserManager, user);
                    }

                    var code = await UserManager.GenerateEmailConfirmationTokenAsync(user);
                    code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));
                    var callbackUrl = Url.Page(
                        "/Account/ConfirmEmail",
                        null,
                        new { area = "Identity", userId = user.Id, code, returnUrl },
                        Request.Scheme);

                    // If the user is a new administrator, don't do these things
                    if (!newAdministrator)
                    {
                        foreach (var admin in admins)
                        {
                            await emailSender.SendEmailAsync(admin.Email, $"New account request for: {user.Email} requested an account.", $"{user.Email} requested a user account on publisher website: {Request.Host}.");
                        }

                        var homePage = await DbContext.Pages.Select(s => new { s.Title, s.UrlPath }).FirstOrDefaultAsync(f => f.UrlPath == "root");
                        var websiteName = homePage.Title ?? Request.Host.Host;

                        var emailHandler = new EmailHandler(emailSender, logger);
                        await emailHandler.SendCallbackTemplateEmail(EmailHandler.CallbackTemplate.NewAccountConfirmEmail, callbackUrl, Request.Host.Host, Input.Email, websiteName);

                        if (UserManager.Options.SignIn.RequireConfirmedAccount)
                        {
                            return RedirectToPage("RegisterConfirmation", new { email = Input.Email, returnUrl });
                        }
                    }

                    await SignInManager.SignInAsync(user, false);

                    return LocalRedirect(returnUrl);
                }

                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }

            ViewData["ShowLogin"] = await UserManager.Users.CosmosAnyAsync();

            // If we got this far, something failed, redisplay form
            return Page();
        }

        /// <summary>
        /// Post model.
        /// </summary>
        public class InputModel
        {
            /// <summary>
            /// Gets or sets email address.
            /// </summary>
            [Required]
            [EmailAddress]
            [Display(Name = "Email")]
            public string Email { get; set; }

            /// <summary>
            /// Gets or sets a value indicating whether user agreement with terms.
            /// </summary>
            [Display(Name = "Agree to terms")]
            public bool AgreeToTerms { get; set; } = false;

            /// <summary>
            /// Gets or sets password.
            /// </summary>
            [Required]
            [StringLength(100, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 6)]
            [DataType(DataType.Password)]
            [Display(Name = "Password")]
            public string Password { get; set; }

            /// <summary>
            /// Gets or sets password is confirmed.
            /// </summary>
            [DataType(DataType.Password)]
            [Display(Name = "Confirm password")]
            [Compare("Password", ErrorMessage = "The password and confirmation password do not match.")]
            public string ConfirmPassword { get; set; }
        }
    }
}
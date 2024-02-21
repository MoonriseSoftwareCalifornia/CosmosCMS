// <copyright file="Register.cshtml.cs" company="Moonrise Software, LLC">
// Copyright (c) Moonrise Software, LLC. All rights reserved.
// Licensed under the GNU Public License, Version 3.0 (https://www.gnu.org/licenses/gpl-3.0.html)
// See https://github.com/MoonriseSoftwareCalifornia/CosmosCMS
// for more information concerning the license and the contributors participating to this project.
// </copyright>

namespace Cosmos.Cms.Areas.Identity.Pages.Account
{
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using Cosmos.Common.Data;
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
    using Microsoft.Extensions.Logging;

    /// <summary>
    /// Register page model.
    /// </summary>
    [AllowAnonymous]
    [EnableRateLimiting("fixed")]
    public class RegisterModel : PageModel
    {
        private readonly ICosmosEmailSender emailSender;
        private readonly ILogger<RegisterModel> logger;
        private readonly SignInManager<IdentityUser> signInManager;
        private readonly UserManager<IdentityUser> userManager;
        private readonly ApplicationDbContext dbContext;

        /// <summary>
        /// Initializes a new instance of the <see cref="RegisterModel"/> class.
        /// Constructor.
        /// </summary>
        /// <param name="userManager">User manager service.</param>
        /// <param name="signInManager">Sign-in service.</param>
        /// <param name="logger">Logger service.</param>
        /// <param name="emailSender">Email sender service.</param>
        /// <param name="dbContext">Database context.</param>
        public RegisterModel(
            UserManager<IdentityUser> userManager,
            SignInManager<IdentityUser> signInManager,
            ILogger<RegisterModel> logger,
            IEmailSender emailSender,
            ApplicationDbContext dbContext)
        {
            this.userManager = userManager;
            this.signInManager = signInManager;
            this.logger = logger;
            this.emailSender = (ICosmosEmailSender)emailSender;
            this.dbContext = dbContext;
        }

        /// <summary>
        /// Gets or sets page input model.
        /// </summary>
        [BindProperty]
        public InputModel Input { get; set; }

        /// <summary>
        /// Gets or sets return URL.
        /// </summary>
        public string ReturnUrl { get; set; }

        /// <summary>
        /// Gets or sets external logins.
        /// </summary>
        public IList<AuthenticationScheme> ExternalLogins { get; set; }

        /// <summary>
        /// GET method handler.
        /// </summary>
        /// <param name="returnUrl">Return URL to pass along.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public async Task<IActionResult> OnGetAsync(string returnUrl = null)
        {
            ReturnUrl = returnUrl;
            ExternalLogins = (await signInManager.GetExternalAuthenticationSchemesAsync()).ToList();

            return Page();
        }

        /// <summary>
        /// POST method handler.
        /// </summary>
        /// <param name="returnUrl">Return URL to pass along.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public async Task<IActionResult> OnPostAsync(string returnUrl = null)
        {
            returnUrl = returnUrl ?? Url.Content("~/");
            ExternalLogins = (await signInManager.GetExternalAuthenticationSchemesAsync()).ToList();

            if (!Input.AgreeToTerms)
            {
                ModelState.AddModelError("Input.AgreeToTerms", "Please check box indicating you have read and agree with our terms and guidelines.");
            }

            if (ModelState.IsValid)
            {
                var user = new IdentityUser { UserName = Input.Email, Email = Input.Email };
                var result = await userManager.CreateAsync(user, Input.Password);
                if (result.Succeeded)
                {
                    logger.LogInformation("User created a new account with password.");

                    var admins = await userManager.GetUsersInRoleAsync("Administrators");
                    var editors = await userManager.GetUsersInRoleAsync("Administrators");

                    foreach (var admin in admins)
                    {
                        await emailSender.SendEmailAsync(admin.Email, $"New account request for: {user.Email} requested an account.", $"{user.Email} requested a user account on publisher website: {Request.Host}.");
                    }

                    var code = await userManager.GenerateEmailConfirmationTokenAsync(user);
                    code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));

                    var callbackUrl = Url.Page(
                        "/Account/ConfirmEmail",
                        null,
                        new { area = "Identity", userId = user.Id, code, returnUrl },
                        Request.Scheme);

                    var homePage = await dbContext.Pages.Select(s => new { s.Title, s.UrlPath }).FirstOrDefaultAsync(f => f.UrlPath == "root");
                    var websiteName = homePage.Title ?? Request.Host.Host;

                    var emailHandler = new EmailHandler(emailSender, logger);
                    await emailHandler.SendCallbackTemplateEmail(EmailHandler.CallbackTemplate.NewAccountConfirmEmail, callbackUrl, Request.Host.Host, Input.Email, websiteName);

                    return RedirectToPage("RegisterConfirmation", new { email = Input.Email, returnUrl });
                }

                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }

            ViewData["ShowLogin"] = await userManager.Users.CosmosAnyAsync();

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
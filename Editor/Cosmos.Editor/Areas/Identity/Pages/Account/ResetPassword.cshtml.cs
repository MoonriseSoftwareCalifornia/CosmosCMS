// <copyright file="ResetPassword.cshtml.cs" company="Moonrise Software, LLC">
// Copyright (c) Moonrise Software, LLC. All rights reserved.
// Licensed under the GNU Public License, Version 3.0 (https://www.gnu.org/licenses/gpl-3.0.html)
// See https://github.com/MoonriseSoftwareCalifornia/CosmosCMS
// for more information concerning the license and the contributors participating to this project.
// </copyright>

namespace Cosmos.Cms.Areas.Identity.Pages.Account
{
    using System;
    using System.ComponentModel.DataAnnotations;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using Cosmos.Cms.Common.Services.Configurations;
    using Cosmos.Common.Data;
    using Cosmos.EmailServices;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Identity;
    using Microsoft.AspNetCore.Identity.UI.Services;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.AspNetCore.Mvc.RazorPages;
    using Microsoft.AspNetCore.RateLimiting;
    using Microsoft.AspNetCore.WebUtilities;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Options;

    /// <summary>
    /// Reset password page model.
    /// </summary>
    [AllowAnonymous]
    [EnableRateLimiting("fixed")]
    public class ResetPasswordModel : PageModel
    {
        private readonly IOptions<SiteSettings> options;
        private readonly ICosmosEmailSender emailSender;
        private readonly ApplicationDbContext dbContext;
        private readonly ILogger<ForgotPasswordModel> logger;
        private readonly UserManager<IdentityUser> userManager;

        /// <summary>
        /// Initializes a new instance of the <see cref="ResetPasswordModel"/> class.
        /// </summary>
        /// <param name="userManager">User manager.</param>
        /// <param name="options">Site settings.</param>
        /// <param name="emailSender">Email sender service.</param>
        /// <param name="dbContext">Database context.</param>
        /// <param name="logger">Log service.</param>
        public ResetPasswordModel(UserManager<IdentityUser> userManager, IOptions<SiteSettings> options, IEmailSender emailSender, ApplicationDbContext dbContext, ILogger<ForgotPasswordModel> logger)
        {
            this.userManager = userManager;
            this.options = options;
            this.emailSender = (ICosmosEmailSender)emailSender;
            this.dbContext = dbContext;
            this.logger = logger;
        }

        /// <summary>
        /// Gets or sets input model.
        /// </summary>
        [BindProperty]
        public InputModel Input { get; set; }

        /// <summary>
        /// Get handler.
        /// </summary>
        /// <param name="code">Reset password verification code.</param>
        /// <returns>Returns an <see cref="IActionResult"/>.</returns>
        public IActionResult OnGet(string code = null)
        {
            if (code == null)
            {
                return BadRequest("A code must be supplied for password reset.");
            }

            Input = new InputModel
            {
                Code = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(code))
            };
            return Page();
        }

        /// <summary>
        /// Post handler.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            var homePage = await dbContext.Pages.Select(s => new { s.Title, s.UrlPath }).FirstOrDefaultAsync(f => f.UrlPath == "root");
            var websiteName = homePage.Title ?? Request.Host.Host;

            var admins = await userManager.GetUsersInRoleAsync("Administrators");
            var emailHandler = new EmailHandler(emailSender, logger);

            var user = await userManager.FindByEmailAsync(Input.Email);
            if (user == null)
            {
                await emailHandler.SendGeneralInfoTemplateEmail(
                        "User without an account tried to change a password",
                        "System Notification",
                        websiteName,
                        Request.Host.Host,
                        $"<p>This is a notification that '{Input.Email},' who does not have an account on this website, tried to change a password for website '{Request.Host.Host}' on {DateTime.UtcNow.ToString()} (UTC). No password reset email was sent.</p>",
                        admins.Select(s => s.Email).ToList());

                // Don't reveal that the user does not exist
                return RedirectToPage("./ResetPasswordConfirmation");
            }

            var result = await userManager.ResetPasswordAsync(user, Input.Code, Input.Password);
            if (result.Succeeded)
            {
                // Notify the administrators of a password change.
                await emailHandler.SendGeneralInfoTemplateEmail(
                    "Password was changed.",
                    "System Notification",
                    websiteName,
                    Request.Host.Host,
                    $"<p>This is a notification that '{Input.Email}' changed their password for website '{Request.Host.Host}' on {DateTime.UtcNow.ToString()} (UTC).</p>",
                    admins.Select(s => s.Email).ToList());

                // Notify the user of a password change.
                await emailHandler.SendGeneralInfoTemplateEmail(
                    "Password was changed.",
                    "System Notification",
                    websiteName,
                    Request.Host.Host,
                    $"<p>This is a confirmation that your password was changed for website '{Request.Host.Host}' on {DateTime.UtcNow.ToString()} (UTC).</p>",
                    Input.Email);

                return RedirectToPage("./ResetPasswordConfirmation");
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            return Page();
        }

        /// <summary>
        /// Form input model.
        /// </summary>
        public class InputModel
        {
            /// <summary>
            /// Gets or sets user email address.
            /// </summary>
            [Required]
            [EmailAddress]
            public string Email { get; set; }

            /// <summary>
            /// Gets or sets error message (if any).
            /// </summary>
            [Required]
            [StringLength(100, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 6)]
            [DataType(DataType.Password)]
            public string Password { get; set; }

            /// <summary>
            /// Gets or sets confirm password field.
            /// </summary>
            [DataType(DataType.Password)]
            [Display(Name = "Confirm password")]
            [Compare("Password", ErrorMessage = "The password and confirmation password do not match.")]
            public string ConfirmPassword { get; set; }

            /// <summary>
            /// Gets or sets code field.
            /// </summary>
            public string Code { get; set; }
        }
    }
}
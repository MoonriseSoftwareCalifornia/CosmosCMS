// <copyright file="ForgotPassword.cshtml.cs" company="Moonrise Software, LLC">
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

    /// <summary>
    /// Forgot password page model.
    /// </summary>
    [AllowAnonymous]
    [EnableRateLimiting("fixed")]
    public class ForgotPasswordModel : PageModel
    {
        private readonly ICosmosEmailSender emailSender;
        private readonly ApplicationDbContext dbContext;
        private readonly ILogger<ForgotPasswordModel> logger;
        private readonly UserManager<IdentityUser> userManager;

        /// <summary>
        /// Initializes a new instance of the <see cref="ForgotPasswordModel"/> class.
        /// </summary>
        /// <param name="userManager">User manager.</param>
        /// <param name="emailSender">Email sender.</param>
        /// <param name="dbContext">Database context.</param>
        /// <param name="logger">Log service.</param>
        public ForgotPasswordModel(
            UserManager<IdentityUser> userManager,
            IEmailSender emailSender,
            ApplicationDbContext dbContext,
            ILogger<ForgotPasswordModel> logger)
        {
            this.userManager = userManager;
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
        /// On get method handler.
        /// </summary>
        /// <param name="returnUrl">Return URL to pass along.</param>
        /// <returns>Returns an <see cref="IActionResult"/>.</returns>
        public IActionResult OnGetAsync(string returnUrl = null)
        {
            return Page();
        }

        /// <summary>
        /// On post method handler.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public async Task<IActionResult> OnPostAsync()
        {
            if (ModelState.IsValid)
            {
                var homePage = await dbContext.Pages.Select(s => new { s.Title, s.UrlPath }).FirstOrDefaultAsync(f => f.UrlPath == "root");
                var websiteName = homePage.Title ?? Request.Host.Host;

                var admins = await userManager.GetUsersInRoleAsync("Administrators");
                var emailHandler = new EmailHandler(emailSender, logger);

                var user = await userManager.FindByEmailAsync(Input.Email);
                if (user == null)
                {
                    await emailHandler.SendGeneralInfoTemplateEmail(
                            "User without an account requested password reset",
                            "System Notification",
                            websiteName,
                            Request.Host.Host,
                            $"<p>This is a notification that '{Input.Email},' who does not have an account on this website, requested a password reset for website '{Request.Host.Host}' on {DateTime.UtcNow.ToString()} (UTC). No password reset email was sent.</p>",
                            admins.Select(s => s.Email).ToList());

                    // Don't reveal that the user does not exist or is not confirmed
                    return RedirectToPage("./ForgotPasswordConfirmation");
                }

                if (!await userManager.IsEmailConfirmedAsync(user))
                {
                    await emailHandler.SendGeneralInfoTemplateEmail(
                            "User with an unconfirmed email requested password reset",
                            "System Notification",
                            websiteName,
                            Request.Host.Host,
                            $"<p>This is a notification that '{Input.Email},' who's email has not yet been confirmed, requested a password reset for website '{Request.Host.Host}' on {DateTime.UtcNow.ToString()} (UTC). No password reset email was sent.</p>",
                            admins.Select(s => s.Email).ToList());

                    // Don't reveal that the user does not exist or is not confirmed
                    return RedirectToPage("./ForgotPasswordConfirmation");
                }

                // For more information on how to enable account confirmation and password reset please 
                // visit https://go.microsoft.com/fwlink/?LinkID=532713
                var code = await userManager.GeneratePasswordResetTokenAsync(user);
                code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));
                var callbackUrl = Url.Page(
                    "/Account/ResetPassword",
                    null,
                    new { area = "Identity", code },
                    Request.Scheme);

                await emailHandler.SendCallbackTemplateEmail(EmailHandler.CallbackTemplate.ResetPasswordTemplate, callbackUrl, Request.Host.Host, Input.Email, websiteName);

                await emailHandler.SendGeneralInfoTemplateEmail(
                        "Password Reset Email Requested",
                        "System Notification",
                        websiteName,
                        Request.Host.Host,
                        $"<p>This is a notification that '{Input.Email}' requested a password reset for website '{Request.Host.Host}' on {DateTime.UtcNow.ToString()} (UTC).</p>",
                        admins.Select(s => s.Email).ToList());

                return RedirectToPage("./ForgotPasswordConfirmation");
            }

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
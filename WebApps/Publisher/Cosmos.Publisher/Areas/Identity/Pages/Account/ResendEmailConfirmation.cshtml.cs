// <copyright file="ResendEmailConfirmation.cshtml.cs" company="Moonrise Software, LLC">
// Copyright (c) Moonrise Software, LLC. All rights reserved.
// Licensed under the GNU Public License, Version 3.0 (https://www.gnu.org/licenses/gpl-3.0.html)
// See https://github.com/MoonriseSoftwareCalifornia/CosmosCMS
// for more information concerning the license and the contributors participating to this project.
// </copyright>

namespace Cosmos.Cms.Areas.Identity.Pages.Account
{
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
    /// Resend email confirmation page model.
    /// </summary>
    [AllowAnonymous]
    [EnableRateLimiting("fixed")]
    public class ResendEmailConfirmationModel : PageModel
    {
        private readonly ICosmosEmailSender emailSender;
        private readonly ILogger<RegisterModel> logger;
        private readonly UserManager<IdentityUser> userManager;
        private readonly ApplicationDbContext dbContext;

        /// <summary>
        /// Initializes a new instance of the <see cref="ResendEmailConfirmationModel"/> class.
        /// </summary>
        /// <param name="userManager">User manager service.</param>
        /// <param name="logger">Logger service.</param>
        /// <param name="emailSender">Email sender service.</param>
        /// <param name="dbContext">Database context.</param>
        public ResendEmailConfirmationModel(
            UserManager<IdentityUser> userManager,
            ILogger<RegisterModel> logger,
            IEmailSender emailSender,
            ApplicationDbContext dbContext)
        {
            this.userManager = userManager;
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
        /// GET method handler.
        /// </summary>
        public void OnGet()
        {
        }

        /// <summary>
        /// POST method handler.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            var user = await userManager.FindByEmailAsync(Input.Email);
            if (user == null)
            {
                ModelState.AddModelError(string.Empty, "Verification email sent. Please check your email.");
                return Page();
            }

            var userId = await userManager.GetUserIdAsync(user);
            var code = await userManager.GenerateEmailConfirmationTokenAsync(user);
            code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));

            var callbackUrl = Url.Page(
                "/Account/ConfirmEmail",
                null,
                new { userId, code },
                Request.Scheme);

            var homePage = await dbContext.Pages.Select(s => new { s.Title, s.UrlPath }).FirstOrDefaultAsync(f => f.UrlPath == "root");
            var websiteName = homePage.Title ?? Request.Host.Host;

            var emailHandler = new EmailHandler(emailSender, logger);
            await emailHandler.SendCallbackTemplateEmail(EmailHandler.CallbackTemplate.NewAccountConfirmEmail, callbackUrl, Request.Host.Host, Input.Email, websiteName);

            ViewData["SendGridResponse"] = ((SendGridEmailSender)emailSender).Response;
            ModelState.AddModelError(string.Empty, "Verification email sent. Please check your email.");
            return Page();
        }

        /// <summary>
        /// Page input model.
        /// </summary>
        public class InputModel
        {
            /// <summary>
            /// Gets or sets email address.
            /// </summary>
            [Required]
            [EmailAddress]
            required public string Email { get; set; }
        }
    }
}
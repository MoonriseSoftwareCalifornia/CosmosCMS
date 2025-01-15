// <copyright file="ConfirmEmail.cshtml.cs" company="Moonrise Software, LLC">
// Copyright (c) Moonrise Software, LLC. All rights reserved.
// Licensed under the GNU Public License, Version 3.0 (https://www.gnu.org/licenses/gpl-3.0.html)
// See https://github.com/MoonriseSoftwareCalifornia/CosmosCMS
// for more information concerning the license and the contributors participating to this project.
// </copyright>

namespace Cosmos.Cms.Areas.Identity.Pages.Account
{
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
    using Microsoft.AspNetCore.WebUtilities;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.Logging;

    /// <summary>
    /// Confirm Email Razor page.
    /// </summary>
    [AllowAnonymous]
    public class ConfirmEmailModel : PageModel
    {
        private readonly ICosmosEmailSender emailSender;
        private readonly ILogger<ConfirmEmailModel> logger;
        private readonly UserManager<IdentityUser> userManager;
        private readonly ApplicationDbContext dbContext;

        /// <summary>
        /// Initializes a new instance of the <see cref="ConfirmEmailModel"/> class.
        /// Constructor.
        /// </summary>
        /// <param name="userManager">User manager.</param>
        /// <param name="logger">Log service.</param>
        /// <param name="emailSender">Email sending service.</param>
        /// <param name="dbContext">Database context.</param>
        public ConfirmEmailModel(
            UserManager<IdentityUser> userManager,
            ILogger<ConfirmEmailModel> logger,
            IEmailSender emailSender,
            ApplicationDbContext dbContext)
        {
            this.userManager = userManager;
            this.logger = logger;
            this.emailSender = (ICosmosEmailSender)emailSender;
            this.dbContext = dbContext;
        }

        /// <summary>
        /// Gets or sets status message.
        /// </summary>
        [TempData]
        public string StatusMessage { get; set; }

        /// <summary>
        /// Get method handler.
        /// </summary>
        /// <param name="userId">User ID.</param>
        /// <param name="code">Verification code.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public async Task<IActionResult> OnGetAsync(string userId, string code)
        {
            if (userId == null || code == null)
            {
                return RedirectToPage("/Index");
            }

            var user = await userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return NotFound($"Unable to load user with ID '{userId}'.");
            }

            code = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(code));
            var result = await userManager.ConfirmEmailAsync(user, code);
            StatusMessage = result.Succeeded ? "Thank you for confirming your email." : "Error confirming your email.";

            var homePage = await dbContext.Pages.Select(s => new { s.Title, s.UrlPath }).FirstOrDefaultAsync(f => f.UrlPath == "root");
            var websiteName = homePage.Title ?? Request.Host.Host;

            var emailBody = new StringBuilder();
            emailBody.AppendLine("<p>Before you can use your account, a systems administrator needs to grant your account permissions.</p>");
            emailBody.AppendLine("<p>Meanwhile, an administrator has been notified that your account has been created and that you need your permissions set.</p>");
            var emailHandler = new EmailHandler(emailSender, logger);

            await emailHandler.SendGeneralInfoTemplateEmail("Almost Done!", "Waiting for administrator.", websiteName, Request.Host.Host, emailBody.ToString(), user.Email);

            return Page();
        }
    }
}
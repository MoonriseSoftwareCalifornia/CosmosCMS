// <copyright file="RegisterConfirmation.cshtml.cs" company="Moonrise Software, LLC">
// Copyright (c) Moonrise Software, LLC. All rights reserved.
// Licensed under the GNU Public License, Version 3.0 (https://www.gnu.org/licenses/gpl-3.0.html)
// See https://github.com/MoonriseSoftwareCalifornia/CosmosCMS
// for more information concerning the license and the contributors participating to this project.
// </copyright>

namespace Sky.Cms.Areas.Identity.Pages.Account
{
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Identity;
    using Microsoft.AspNetCore.Identity.UI.Services;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.AspNetCore.Mvc.RazorPages;

    /// <summary>
    /// Registration confirmation page model.
    /// </summary>
    [AllowAnonymous]
    public class RegisterConfirmationModel : PageModel
    {
        private readonly IEmailSender sender;
        private readonly UserManager<IdentityUser> userManager;

        /// <summary>
        /// Initializes a new instance of the <see cref="RegisterConfirmationModel"/> class.
        /// Constructor.
        /// </summary>
        /// <param name="userManager">User manager.</param>
        /// <param name="sender">Email sender service.</param>
        public RegisterConfirmationModel(UserManager<IdentityUser> userManager, IEmailSender sender)
        {
            this.userManager = userManager;
            this.sender = sender;
        }

        /// <summary>
        /// Gets or sets email address.
        /// </summary>
        public string Email { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether should display confirmation account link.
        /// </summary>
        public bool DisplayConfirmAccountLink { get; set; }

        /// <summary>
        /// Gets or sets email confirmation URL.
        /// </summary>
        public string EmailConfirmationUrl { get; set; }

        /// <summary>
        /// GET method handler.
        /// </summary>
        /// <param name="email"></param>
        /// <param name="returnUrl"></param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public async Task<IActionResult> OnGetAsync(string email, string returnUrl = null)
        {
            if (email == null)
            {
                return RedirectToPage("/Index");
            }

            var user = await userManager.FindByEmailAsync(email);
            if (user == null)
            {
                return NotFound($"Unable to load user with email '{email}'.");
            }

            Email = email;
            // Once you add a real email sender, you should remove this code that lets you confirm the account
            // DisplayConfirmAccountLink = true;
            // if (DisplayConfirmAccountLink)
            // {
            //    var userId = await _userManager.GetUserIdAsync(user);
            //    var code = await _userManager.GenerateEmailConfirmationTokenAsync(user);
            //    code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));
            //    EmailConfirmationUrl = Url.Page(
            //        "/Account/ConfirmEmail",
            //        pageHandler: null,
            //        values: new { area = "Identity", userId = userId, code = code, returnUrl = returnUrl },
            //        protocol: Request.Scheme);
            // }
            return Page();
        }
    }
}
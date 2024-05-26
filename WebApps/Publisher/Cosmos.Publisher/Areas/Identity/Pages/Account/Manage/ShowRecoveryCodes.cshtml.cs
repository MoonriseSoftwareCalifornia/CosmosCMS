// <copyright file="ShowRecoveryCodes.cshtml.cs" company="Moonrise Software, LLC">
// Copyright (c) Moonrise Software, LLC. All rights reserved.
// Licensed under the GNU Public License, Version 3.0 (https://www.gnu.org/licenses/gpl-3.0.html)
// See https://github.com/MoonriseSoftwareCalifornia/CosmosCMS
// for more information concerning the license and the contributors participating to this project.
// </copyright>

namespace Cosmos.Cms.Areas.Identity.Pages.Account.Manage
{
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.AspNetCore.Mvc.RazorPages;

    /// <summary>
    /// Show recover codes page model.
    /// </summary>
    public class ShowRecoveryCodesModel : PageModel
    {
        /// <summary>
        /// Gets or sets recovery codes.
        /// </summary>
        [TempData]
        public string[] RecoveryCodes { get; set; }

        /// <summary>
        /// Gets or sets status message.
        /// </summary>
        [TempData]
        public string StatusMessage { get; set; }

        /// <summary>
        /// Get handler.
        /// </summary>
        /// <returns>Returns an <see cref="IActionResult"/>.</returns>
        public IActionResult OnGet()
        {
            if (RecoveryCodes == null || RecoveryCodes.Length == 0)
            {
                return RedirectToPage("./TwoFactorAuthentication");
            }

            return Page();
        }
    }
}
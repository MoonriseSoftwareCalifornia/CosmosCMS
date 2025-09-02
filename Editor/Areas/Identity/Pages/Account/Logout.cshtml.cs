﻿// <copyright file="Logout.cshtml.cs" company="Moonrise Software, LLC">
// Copyright (c) Moonrise Software, LLC. All rights reserved.
// Licensed under the GNU Public License, Version 3.0 (https://www.gnu.org/licenses/gpl-3.0.html)
// See https://github.com/MoonriseSoftwareCalifornia/CosmosCMS
// for more information concerning the license and the contributors participating to this project.
// </copyright>

namespace Cosmos.Cms.Areas.Identity.Pages.Account
{
    using System.Threading.Tasks;
    using Cosmos.DynamicConfig;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Identity;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.AspNetCore.Mvc.RazorPages;
    using Microsoft.Extensions.Logging;

    /// <summary>
    /// Logout page model.
    /// </summary>
    [AllowAnonymous]
    public class LogoutModel : PageModel
    {
        private readonly ILogger<LogoutModel> logger;
        private readonly SignInManager<IdentityUser> signInManager;

        /// <summary>
        /// Initializes a new instance of the <see cref="LogoutModel"/> class.
        /// Constructor.
        /// </summary>
        /// <param name="signInManager">Sign in manager service.</param>
        /// <param name="logger">Logger service.</param>
        public LogoutModel(SignInManager<IdentityUser> signInManager, ILogger<LogoutModel> logger)
        {
            this.signInManager = signInManager;
            this.logger = logger;
        }

        /// <summary>
        /// Handle GET method.
        /// </summary>
        /// <returns>IActionResult.</returns>
        public async Task<IActionResult> OnGet()
        {
            await signInManager.SignOutAsync();
            logger.LogInformation("User logged out.");
            return Redirect("/");
        }

        /// <summary>
        /// Handle POST method.
        /// </summary>
        /// <param name="returnUrl">Return URL.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public async Task<IActionResult> OnPost(string returnUrl = null)
        {
            await signInManager.SignOutAsync();
            logger.LogInformation("User logged out.");
            if (returnUrl != null)
            {
                return LocalRedirect(returnUrl);
            }

            return Redirect("/");
        }
    }
}
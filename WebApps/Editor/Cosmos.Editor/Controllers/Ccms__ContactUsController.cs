// <copyright file="Ccms__ContactUsController.cs" company="Moonrise Software, LLC">
// Copyright (c) Moonrise Software, LLC. All rights reserved.
// Licensed under the GNU Public License, Version 3.0 (https://www.gnu.org/licenses/gpl-3.0.html)
// See https://github.com/MoonriseSoftwareCalifornia/CosmosCMS
// for more information concerning the license and the contributors participating to this project.
// </copyright>

namespace Cosmos.Editor.Controllers
{
    using System;
    using System.Threading.Tasks;
    using Cosmos.Cms.Common.Services.Configurations;
    using Cosmos.Cms.Data.Logic;
    using Cosmos.Common.Models;
    using Cosmos.EmailServices;
    using Microsoft.AspNetCore.Identity;
    using Microsoft.AspNetCore.Identity.UI.Services;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Options;

    /// <summary>
    /// Contact Us Controller.
    /// </summary>
    public class Ccms__ContactUsController : Controller
    {
        private readonly ArticleEditLogic articleLogic;
        private readonly UserManager<IdentityUser> userManager;
        private readonly AzureCommunicationEmailSender emailSender;
        private readonly ILogger<Ccms__ContactUsController> logger;
        private readonly IOptions<CosmosConfig> cosmosOptions;

        /// <summary>
        /// Initializes a new instance of the <see cref="Ccms__ContactUsController"/> class.
        /// Constructor.
        /// </summary>
        /// <param name="cosmosOptions"></param>
        /// <param name="logger"></param>
        /// <param name="emailSender"></param>
        /// <param name="articleLogic"></param>
        /// <param name="userManager"></param>
        public Ccms__ContactUsController(
            IOptions<CosmosConfig> cosmosOptions,
            ILogger<Ccms__ContactUsController> logger,
            IEmailSender emailSender,
            ArticleEditLogic articleLogic,
            UserManager<IdentityUser> userManager)
        {
            this.cosmosOptions = cosmosOptions;
            this.logger = logger;
            this.emailSender = (AzureCommunicationEmailSender)emailSender;
            this.articleLogic = articleLogic;
            this.userManager = userManager;
        }

        /// <summary>
        /// Contact us email form.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public async Task<IActionResult> Index()
        {
            var user = await userManager.GetUserAsync(User);

            return View(new EmailMessageViewModel()
            {
                FromEmail = user.Email
            });
        }

        /// <summary>
        /// Send Email Message.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Index(EmailMessageViewModel model)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var user = await userManager.GetUserAsync(User);

                    await emailSender.SendEmailAsync(user.Email,
                        model.Subject, $"<h5>{model.FromEmail} sent the following message:</h5><br />{model.Content}");

                    model.SendSuccess = true;
                }
                catch (Exception e)
                {
                    logger.LogError(e.Message, e);
                }
            }

            return View(model);
        }
    }
}

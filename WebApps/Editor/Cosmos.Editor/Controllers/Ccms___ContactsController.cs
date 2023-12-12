// <copyright file="Ccms___ContactsController.cs" company="Moonrise Software, LLC">
// Copyright (c) Moonrise Software, LLC. All rights reserved.
// Licensed under the GNU Public License, Version 3.0 (https://www.gnu.org/licenses/gpl-3.0.html)
// See https://github.com/MoonriseSoftwareCalifornia/CosmosCMS
// for more information concerning the license and the contributors participating to this project.
// </copyright>

namespace Cosmos.Editor.Controllers
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Cosmos.Cms.Common.Services.Configurations;
    using Cosmos.Common.Data;
    using Cosmos.Common.Models;
    using Microsoft.AspNetCore.Identity.UI.Services;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Options;

    /// <summary>
    /// Contact Us Controller.
    /// </summary>
    public class Ccms___ContactsController : Controller
    {
        private readonly IEmailSender emailSender;
        private readonly ILogger<Ccms___ContactsController> logger;
        private readonly IOptions<CosmosConfig> cosmosOptions;
        private readonly ApplicationDbContext dbContext;

        /// <summary>
        /// Initializes a new instance of the <see cref="Ccms___ContactsController"/> class.
        /// Constructor.
        /// </summary>
        /// <param name="cosmosOptions">Cosmos options.</param>
        /// <param name="logger">Logger.</param>
        /// <param name="emailSender">Sendgrid Email sender</param>
        /// <param name="dbContext">Database context.</param>
        public Ccms___ContactsController(
            IOptions<CosmosConfig> cosmosOptions,
            ILogger<Ccms___ContactsController> logger,
            IEmailSender emailSender,
            ApplicationDbContext dbContext)
        {
            this.cosmosOptions = cosmosOptions;
            this.logger = logger;
            this.emailSender = emailSender;
            this.dbContext = dbContext;
        }

        /// <summary>
        /// Gets the contact list page.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public IActionResult Index()
        {
            return View();
        }

        /// <summary>
        /// Gets the contact list.
        /// </summary>
        /// <returns>Returns a list of contacts.</returns>
        [HttpGet]
        public async Task<IActionResult> GetContacts()
        {
            var result = new Result();

            result.data = await dbContext.Contacts.OrderBy(o => o.Email).ToListAsync();

            return Json(result);
        }

        private class Result
        {
            public List<Contact> data { get; set; }
        }
    }
}

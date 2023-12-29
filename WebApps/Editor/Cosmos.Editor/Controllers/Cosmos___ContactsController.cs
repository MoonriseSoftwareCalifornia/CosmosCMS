// <copyright file="Ccms___ContactsController.cs" company="Moonrise Software, LLC">
// Copyright (c) Moonrise Software, LLC. All rights reserved.
// Licensed under the GNU Public License, Version 3.0 (https://www.gnu.org/licenses/gpl-3.0.html)
// See https://github.com/MoonriseSoftwareCalifornia/CosmosCMS
// for more information concerning the license and the contributors participating to this project.
// </copyright>

namespace Cosmos.Editor.Controllers
{
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;
    using Cosmos.Cms.Common.Services.Configurations;
    using Cosmos.Common.Data;
    using Cosmos.Editor.Models;
    using CsvHelper;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Identity.UI.Services;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Options;

    /// <summary>
    /// Contact Us Controller.
    /// </summary>
    [Authorize(Roles = "Administrators,Editors")]
    public class Cosmos___ContactsController : Controller
    {
        private readonly IEmailSender emailSender;
        private readonly ILogger<Cosmos___ContactsController> logger;
        private readonly IOptions<CosmosConfig> cosmosOptions;
        private readonly ApplicationDbContext dbContext;

        /// <summary>
        /// Initializes a new instance of the <see cref="Cosmos___ContactsController"/> class.
        /// Constructor.
        /// </summary>
        /// <param name="cosmosOptions">Cosmos options.</param>
        /// <param name="logger">Logger.</param>
        /// <param name="emailSender">Sendgrid Email sender</param>
        /// <param name="dbContext">Database context.</param>
        public Cosmos___ContactsController(
            IOptions<CosmosConfig> cosmosOptions,
            ILogger<Cosmos___ContactsController> logger,
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

        /// <summary>
        /// Returns the list of contacts in the database.
        /// </summary>
        /// <returns>Returns a CSV file.</returns>
        public async Task<IActionResult> ExportContacts()
        {
            var data = await dbContext.Contacts.OrderBy(o => o.Created).ToListAsync();
            var export = new List<ContactsExportViewModel>();
            var index = 1;

            foreach (var contact in data)
            {
                export.Add(new ContactsExportViewModel()
                {
                    Id = index++,
                    Created = contact.Created,
                    Email = contact.Email,
                    FirstName = contact.FirstName,
                    LastName = contact.LastName,
                    Phone = contact.Phone,
                    Updated = contact.Updated
                });
            }

            await using var memoryStream = new MemoryStream();
            await using var writer = new StreamWriter(memoryStream);
            await using var csv = new CsvWriter(writer, CultureInfo.InvariantCulture, false);

            await csv.WriteRecordsAsync(export);
            await csv.FlushAsync();

            return File(memoryStream.ToArray(), "application/csv", fileDownloadName: "contact-list.csv");
        }

        /// <summary>
        /// This model is used to create a JSON file by the <see cref="GetContacts"/> method for use with jQuery <seealso href="https://datatables.net/">DataTable plugin.</seealso>
        /// </summary>
        private class Result
        {
#pragma warning disable SA1300 // Element should begin with upper-case letter
            public List<Contact> data { get; set; }
#pragma warning restore SA1300 // Element should begin with upper-case letter
        }
    }
}

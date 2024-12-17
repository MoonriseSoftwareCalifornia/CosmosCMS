// <copyright file="Cosmos___ContactsController.cs" company="Moonrise Software, LLC">
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
    using Cosmos.Common.Data;
    using Cosmos.Common.Services.Configurations;
    using Cosmos.Editor.Models;
    using CsvHelper;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.EntityFrameworkCore;

    /// <summary>
    /// Contact Us Controller.
    /// </summary>
    [Authorize(Roles = "Administrators,Editors")]
    public class Cosmos___ContactsController : Controller
    {
        private readonly ApplicationDbContext dbContext;

        /// <summary>
        /// Initializes a new instance of the <see cref="Cosmos___ContactsController"/> class.
        /// Constructor.
        /// </summary>
        /// <param name="dbContext">Database context.</param>
        public Cosmos___ContactsController(ApplicationDbContext dbContext)
        {
            this.dbContext = dbContext;
        }

        /// <summary>
        /// Gets the contact list page.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public async Task<IActionResult> Index()
        {
            ViewData["MailChimpIntegrated"] = await dbContext.Settings.Where(w => w.Group == "MailChimp").CosmosAnyAsync();
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
        /// Opens the MailChimp configuration page.
        /// </summary>
        /// <returns>Returns the view.</returns>
        public async Task<IActionResult> MailChimp()
        {
            var settings = await dbContext.Settings.Where(w => w.Group == "MailChimp").ToListAsync();

            var model = new MailChimpConfig();

            if (settings.Count == 0)
            {
                return View(model);
            }

            model.ContactListName = settings.FirstOrDefault(f => f.Name == "ContactListName")?.Value;
            model.ApiKey = settings.FirstOrDefault(f => f.Name == "ApiKey")?.Value;

            return View(model);
        }

        /// <summary>
        /// Removes MailChimp settings.
        /// </summary>
        /// <returns>Redirects to Index when done.</returns>
        public async Task<IActionResult> RemoveMailChimp()
        {
            var settings = await dbContext.Settings.Where(w => w.Group == "MailChimp").ToListAsync();
            dbContext.Settings.RemoveRange(settings);
            await dbContext.SaveChangesAsync();
            return RedirectToAction("Index");
        }

        /// <summary>
        /// Saves MailChimp settings.
        /// </summary>
        /// <param name="model">MailChimp configuration model.</param>
        /// <returns>Redirects to index if successful.</returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MailChimp(MailChimpConfig model)
        {
            if (ModelState.IsValid)
            {
                var settings = await dbContext.Settings.Where(w => w.Group == "MailChimp").ToListAsync();
                var key = settings.FirstOrDefault(f => f.Name == "ApiKey");
                var list = settings.FirstOrDefault(f => f.Name == "ContactListName");

                if (key == null)
                {
                    key = new Setting() { Group = "MailChimp", Name = "ApiKey", Value = model.ApiKey, Description = "MailChip API Key." };
                    dbContext.Settings.Add(key);
                }
                else
                {
                    key.Value = model.ApiKey;
                }

                if (list == null)
                {
                    list = new Setting() { Group = "MailChimp", Name = "ContactListName", Value = model.ContactListName.Trim(), Description = "List name that contacts are added to." };
                    dbContext.Settings.Add(list);
                }
                else
                {
                    list.Value = model.ContactListName;
                }

                await dbContext.SaveChangesAsync();

                return RedirectToAction("Index");

            }

            // Something isn't right.
            return View(model);
        }

        /// <summary>
        /// This model is used to create a JSON file by the <see cref="GetContacts"/> method for use with jQuery. <seealso href="https://datatables.net/">DataTable plugin.</seealso>
        /// </summary>
        private class Result
        {
#pragma warning disable SA1300 // Element should begin with upper-case letter
            public List<Contact> data { get; set; }
#pragma warning restore SA1300 // Element should begin with upper-case letter
        }
    }
}

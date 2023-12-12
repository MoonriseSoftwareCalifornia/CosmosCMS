// <copyright file="BaseController.cs" company="Moonrise Software, LLC">
// Copyright (c) Moonrise Software, LLC. All rights reserved.
// Licensed under the GNU Public License, Version 3.0 (https://www.gnu.org/licenses/gpl-3.0.html)
// See https://github.com/MoonriseSoftwareCalifornia/CosmosCMS
// for more information concerning the license and the contributors participating to this project.
// </copyright>

namespace Cosmos.Cms.Controllers
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using System.Web;
    using Cosmos.Cms.Common.Services.Configurations;
    using Cosmos.Cms.Data.Logic;
    using Cosmos.Cms.Services;
    using Cosmos.Common.Data;
    using Cosmos.Common.Models;
    using HtmlAgilityPack;
    using Microsoft.AspNetCore.Identity;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.AspNetCore.Mvc.ModelBinding;
    using Microsoft.AspNetCore.Mvc.Rendering;
    using Microsoft.EntityFrameworkCore;

    /// <summary>
    /// Base controller.
    /// </summary>
    public abstract class BaseController : Controller
    {
        private readonly UserManager<IdentityUser> baseUserManager;
        private readonly ApplicationDbContext dbContext;

        /// <summary>
        /// Initializes a new instance of the <see cref="BaseController"/> class.
        ///     Constructor.
        /// </summary>
        /// <param name="dbContext">Database context.</param>
        /// <param name="userManager">User manager.</param>
        internal BaseController(ApplicationDbContext dbContext, UserManager<IdentityUser> userManager)
        {
            this.dbContext = dbContext;
            baseUserManager = userManager;
        }

        /// <summary>
        ///     Server-side validation of HTML.
        /// </summary>
        /// <param name="fieldName">Field name to validate.</param>
        /// <param name="inputHtml">HTML data to check.</param>
        /// <returns>HTML content.</returns>
        /// <remarks>
        ///     <para>
        ///         The purpose of this method is to validate HTML prior to be saved to the database.
        ///         It uses an instance of <see cref="HtmlAgilityPack.HtmlDocument" /> to check HTML formatting.
        ///     </para>
        /// </remarks>
        internal string BaseValidateHtml(string fieldName, string inputHtml)
        {
            if (!string.IsNullOrEmpty(inputHtml))
            {
                var contentHtmlDocument = new HtmlDocument();
                contentHtmlDocument.LoadHtml(HttpUtility.HtmlDecode(inputHtml));
                return contentHtmlDocument.ParsedText.Trim();
            }

            return string.Empty;
        }

        /// <summary>
        ///     Get Layout List Items.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        internal async Task<List<SelectListItem>> BaseGetLayoutListItems()
        {
            var layouts = await dbContext.Layouts.Select(s => new SelectListItem
            {
                Value = s.Id.ToString(),
                Text = s.LayoutName
            }).ToListAsync();
            if (layouts != null)
            {
                return layouts;
            }

            var layoutViewModel = new LayoutViewModel();

            dbContext.Layouts.Add(layoutViewModel.GetLayout());
            await dbContext.SaveChangesAsync();

            return await dbContext.Layouts.Select(s => new SelectListItem
            {
                Value = s.Id.ToString(),
                Text = s.LayoutName
            }).ToListAsync();
        }

        /// <summary>
        /// Strips Byte Order Marks.
        /// </summary>
        /// <param name="data">HTML data.</param>
        /// <returns>Un-BOMed html.</returns>
        internal string StripBOM(string data)
        {
            // See: https://danielwertheim.se/utf-8-bom-adventures-in-c/
            if (string.IsNullOrEmpty(data) || string.IsNullOrWhiteSpace(data))
            {
                return data;
            }

            // Get rid of Zero Length strings
            var rows = data.Split("\r\n");
            var builder = new StringBuilder();
            foreach (var row in rows)
            {
                if (row.Trim().Equals(string.Empty) == false)
                {
                    builder.AppendLine(row);
                }
            }

            data = builder.ToString();

            // Search for and eliminate BOM
            var filtered = new string(data.ToArray().Where(c => c != '\uFEFF' && c != '\u00a0').ToArray());

            using var memStream = new MemoryStream();
            using var writer = new StreamWriter(memStream, new UTF8Encoding(false));
            writer.Write(filtered);
            writer.Flush();

            var clean = Encoding.UTF8.GetString(memStream.ToArray());

            return clean;
        }

        /// <summary>
        /// Returns model state errors as serialization.
        /// </summary>
        /// <param name="modelState">Model state.</param>
        /// <returns>Errors.</returns>
        internal string SerializeErrors(ModelStateDictionary modelState)
        {
            var errors = modelState.Values
                .Where(w => w.ValidationState == ModelValidationState.Invalid).Select(s => s.Errors)
                .ToList();

            return Newtonsoft.Json.JsonConvert.SerializeObject(errors);
        }

        /// <summary>
        /// Gets the user Email address of the currently logged in user.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        protected async Task<string> GetUserEmail()
        {
            // Get the user's ID for logging.
            var user = await baseUserManager.GetUserAsync(User);
            return user.Email;
        }

        /// <summary>
        /// Gets the user ID of the currently logged in user.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        protected async Task<string> GetUserId()
        {
            // Get the user's ID for logging.
            var user = await baseUserManager.GetUserAsync(User);
            return user.Id;
        }
    }
}
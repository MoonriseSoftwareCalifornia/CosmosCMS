// <copyright file="Cosmos___ApiController.cs" company="Moonrise Software, LLC">
// Copyright (c) Moonrise Software, LLC. All rights reserved.
// Licensed under the GNU Public License, Version 3.0 (https://www.gnu.org/licenses/gpl-3.0.html)
// See https://github.com/MoonriseSoftwareCalifornia/CosmosCMS
// for more information concerning the license and the contributors participating to this project.
// </copyright>

namespace Cosmos.Editor.Controllers
{
    using System;
    using System.Collections.Generic;
    using System.Net.Mail;
    using System.Threading.Tasks;
    using AspNetCore.Identity.FlexDb;
    using Cosmos.Common.Data;
    using Cosmos.DynamicConfig;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.Configuration;

    /// <summary>
    /// Cosmos___ApiController class.
    /// </summary>
    public class Cosmos___ApiController : Controller
    {
        private readonly IConfiguration configuration;

        /// <summary>
        /// Initializes a new instance of the <see cref="Cosmos___ApiController"/> class.
        /// </summary>
        /// <param name="configuration">Get the configuration.</param>
        public Cosmos___ApiController(IConfiguration configuration)
        {
            this.configuration = configuration;
        }

        /// <summary>
        /// Gets the website for a user's email address.
        /// </summary>
        /// <param name="emailAddress">Email address.</param>
        /// <returns>Website domain name list.</returns>
        [AllowAnonymous]
        [HttpPost]
        [Route("cosmos___api/getWebsitesByEmail")]
        [ValidateAntiForgeryToken]
        public async Task<IEnumerable<string>> GetWebsitesByEmail([Bind("emailAddress")] string emailAddress)
        {
            var websites = new List<string>();
            if (!ModelState.IsValid)
            {
                return websites;
            }

            if (string.IsNullOrWhiteSpace(emailAddress))
            {
                return websites;
            }

            try
            {
                var addr = new MailAddress(emailAddress);
            }
            catch (FormatException)
            {
                return websites;
            }

            var connectionString = configuration.GetConnectionString("ConfigDbConnectionString");

            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new ArgumentException("Connection string 'ConfigDbConnectionString' not found.");
            }

            var options = CosmosDbOptionsBuilder.GetDbOptions<DynamicConfigDbContext>(connectionString);
            using var dynamicDbContext = new DynamicConfigDbContext(options);

            var result = await dynamicDbContext.Connections.ToListAsync();

            foreach (var connection in result)
            {
                using var dbContext = new ApplicationDbContext(connection.DbConn);
                var user = await dbContext.Users.FirstOrDefaultAsync(u => u.Email == emailAddress);
                if (user != null)
                {
                    var website = connection.WebsiteUrl;
                    if (!string.IsNullOrWhiteSpace(website))
                    {
                        var uri = new Uri(website);
                        websites.Add(uri.Host.ToLower());
                    }
                }
            }

            return websites;
        }
    }
}

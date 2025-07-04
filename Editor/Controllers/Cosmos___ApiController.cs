using Cosmos.Common.Data;
using Cosmos.DynamicConfig;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Net.Mail;
using System.Threading.Tasks;

namespace Cosmos.Editor.Controllers
{
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

            var options = new DbContextOptionsBuilder<DynamicConfigDbContext>()
                    .UseCosmos(connectionString, databaseName: "configs")
                    .Options;
            using var dynamicDbContext = new DynamicConfigDbContext(options);

            var result = await dynamicDbContext.Connections.ToListAsync();

            foreach (var connection in result)
            {
                var dbOptions = new DbContextOptionsBuilder<ApplicationDbContext>()
                    .UseCosmos(connection.DbConn, connection.DbName)
                    .Options;
                var dbContext = new ApplicationDbContext(dbOptions);
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

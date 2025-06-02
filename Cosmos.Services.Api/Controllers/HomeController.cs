using Cosmos.BlobService;
using Cosmos.Cms.Common.Services.Configurations;
using Cosmos.Common;
using Cosmos.Common.Data;
using Cosmos.Common.Data.Logic;
using Cosmos.Common.Models;
using Cosmos.Common.Services;
using Cosmos.Common.Services.PowerBI;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;

namespace Cosmos.Services.Api.Controllers
{
    [ApiController]
    [Route("api")]
    public class HomeController : ControllerBase
    {
        private readonly ILogger<HomeController> logger;
        private readonly IEmailSender emailSender;
        private readonly IAntiforgery antiforgery;
        private readonly IWebHostEnvironment env;
        private readonly ApplicationDbContext dbContext;

        /// <summary>
        /// Initializes a new instance of the <see cref="HomeController"/> class.
        /// </summary>
        /// <param name="services">Services provider.</param>
        /// <param name="configuration">Configuration.</param>
        /// <param name="logger">Logger.</param>
        /// <param name="articleLogic">Article logic.</param>
        /// <param name="options">Cosmos options.</param>
        /// <param name="dbContext">Database Context.</param>
        /// <param name="storageContext">Storage context.</param>
        /// <param name="powerBiTokenService">Service used to get tokens from Power BI.</param>
        /// <param name="emailSender">Email services.</param>
        public HomeController(
            IConfiguration configuration,
            ILogger<HomeController> logger,
            ArticleLogic articleLogic,
            IOptions<CosmosConfig> options,
            ApplicationDbContext dbContext,
            StorageContext storageContext,
            PowerBiTokenService powerBiTokenService,
            IEmailSender emailSender)
        {
            this.logger = logger;
            //this.dbContext = dbContext;
            //try
            //{
            //    this.graphService = services.GetRequiredService<MsGraphService>();
            //}
            //catch
            //{
            //    // Ignore if the service is not registered.
            //}

            // Ensure the database is created if we are in setup mode.
            //if (options.Value.SiteSettings.AllowSetup)
            //{
            //    _ = dbContext.Database.EnsureCreatedAsync().Result;
            //}
        }

        [HttpGet(Name = "script")]
        public IActionResult script(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return BadRequest("Script id is required.");
            }
            var filePath = Path.Combine(env.WebRootPath, "scripts", $"{name}.js");
            if (!System.IO.File.Exists(filePath))
            {
                return NotFound($"Script '{name}' not found.");
            }
            var tokens = antiforgery.GetAndStoreTokens(HttpContext);
            Response.Cookies.Append("X-XSRF-TOKEN", tokens.RequestToken!, new CookieOptions
            {
                Domain = Request.Host.Host, // 🔒 Restrict to this domain only
                Path = "/",
                HttpOnly = false,
                Secure = true,
                SameSite = SameSiteMode.Strict
            });
            var bytes = System.IO.File.ReadAllBytes(filePath);
            return File(bytes, "application/javascript", name);
        }

        [HttpGet(Name = "PostContact")]
        public ActionResult<ContactViewModel> Post([FromBody] ContactViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            if (model == null)
            {
                return NotFound();
            }

            return Ok(model);

            //model.Id = Guid.NewGuid();
            //model.Created = DateTimeOffset.UtcNow;
            //model.Updated = DateTimeOffset.UtcNow;

            //var contactService = new ContactManagementService(dbContext, emailSender, logger, this.HttpContext);

            //var result = await contactService.AddContactAsync(model);

            //return result;

            //// Optionally send an email notification
            //await emailSender.SendEmailAsync(model.Email, "Contact Form Submission", "Thank you for contacting us.");

            //return Ok(new { message = "Contact information received." });
        }
    }
}

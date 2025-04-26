using Cosmos.Common.Data;
using Cosmos.Common.Models;
using MailChimp.Net;
using MailChimp.Net.Interfaces;
using MailChimp.Net.Models;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;

namespace Cosmos.Services.Api.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class CosmosController : ControllerBase
    {
        private readonly ILogger<CosmosController> logger;
        private readonly ApplicationDbContext dbContext;
        private readonly IEmailSender emailSender;

        public CosmosController(ILogger<CosmosController> logger, ApplicationDbContext dbContext, IEmailSender emailSender)
        {
            this.logger = logger;
            this.dbContext = dbContext;
            this.emailSender = emailSender;
        }

        /// <summary>
        /// Post contact information.
        /// </summary>
        /// <param name="model">Contact data model.</param>
        /// <returns>Returns OK if successful.</returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        [EnableRateLimiting("fixed")]
        public async Task<IActionResult> CCMS_POSTCONTACT_INFO(ContactViewModel model)
        {
            if (model == null)
            {
                return Ok();
            }

            model.Id = Guid.NewGuid();
            model.Created = DateTimeOffset.UtcNow;
            model.Updated = DateTimeOffset.UtcNow;
            if (ModelState.IsValid)
            {
                var contact = await dbContext.Contacts.FirstOrDefaultAsync(f => f.Email.ToLower() == model.Email.ToLower());

                if (contact == null)
                {
                    dbContext.Contacts.Add(new Common.Data.Contact() { Email = model.Email.ToLower(), FirstName = model.FirstName, LastName = model.LastName, Phone = model.Phone });
                }
                else
                {
                    contact.Updated = DateTimeOffset.UtcNow;
                    contact.FirstName = model.FirstName;
                    contact.LastName = model.LastName;
                    contact.Phone = model.Phone;
                }

                await dbContext.SaveChangesAsync();

                // MailChimp? If so add contact to list.
                var settings = await dbContext.Settings.Where(w => w.Group == "MailChimp").ToListAsync();
                if (settings.Count > 0)
                {
                    var key = settings.FirstOrDefault(f => f.Name == "ApiKey");
                    var list = settings.FirstOrDefault(f => f.Name == "ContactListName");
                    IMailChimpManager manager = new MailChimpManager(key.Value);

                    var lists = await manager.Lists.GetAllAsync();
                    var mclist = lists.FirstOrDefault(w => w.Name.Equals(list.Value, StringComparison.OrdinalIgnoreCase));

                    var member = new Member { FullName = $"{model.FirstName} {model.LastName}", EmailAddress = contact.Email, StatusIfNew = MailChimp.Net.Models.Status.Subscribed };

                    member.LastChanged = DateTimeOffset.UtcNow.ToString();

                    member.MergeFields.Add("FNAME", model.FirstName);
                    member.MergeFields.Add("LNAME", model.LastName);
                    var updated = await manager.Members.AddOrUpdateAsync(mclist.Id, member);

                    logger.LogInformation($"Add or updated {updated.FullName} {updated.EmailAddress} with MailChimp on {updated.LastChanged}.");
                }

                var alertsSetting = await dbContext.Settings.FirstOrDefaultAsync(w => w.Group == "ContactsConfig" && w.Name == "EnableAlerts");

                if (alertsSetting != null && bool.Parse(alertsSetting.Value) == true)
                {
                    // Not using UserManager because we don't want to require injection for that in this base class.
                    var adminUserGroup = await dbContext.Roles.FirstOrDefaultAsync(w => w.NormalizedName == "ADMINISTRATORS");
                    var roleUsers = await dbContext.UserRoles.Where(w => w.RoleId == adminUserGroup.Id).Select(s => s.UserId).ToArrayAsync();
                    var admins = await dbContext.Users.Where(w => roleUsers.Contains(w.Id)).Select(s => s.Email).ToArrayAsync();

                    var subject = "New Contact Information";
                    var body = $"<p>New contact information received from {model.FirstName} {model.LastName} at {model.Email} on website '{HttpContext.Request.Host}.'</p>";
                    body += $"<p>Open your editor to view and download the contact list.</p>";

                    foreach (var e in admins)
                    {
                        await emailSender.SendEmailAsync(e, subject, body);
                    }
                }

                return Ok();
            }
            return BadRequest(ModelState);
        }
    }
}

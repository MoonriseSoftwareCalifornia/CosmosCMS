// <copyright file="HomeControllerBase.cs" company="Moonrise Software, LLC">
// Copyright (c) Moonrise Software, LLC. All rights reserved.
// Licensed under the GNU Public License, Version 3.0 (https://www.gnu.org/licenses/gpl-3.0.html)
// See https://github.com/MoonriseSoftwareCalifornia/CosmosCMS
// for more information concerning the license and the contributors participating to this project.
// </copyright>

namespace Cosmos.Common
{
    using System;
    using System.Linq;
    using System.Threading.Tasks;
    using Cosmos.BlobService;
    using Cosmos.Common.Data;
    using Cosmos.Common.Data.Logic;
    using Cosmos.Common.Models;
    using Cosmos.Common.Services.PowerBI;
    using MailChimp.Net;
    using MailChimp.Net.Interfaces;
    using MailChimp.Net.Models;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Cors;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.AspNetCore.RateLimiting;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.Logging;
    using Microsoft.PowerBI.Api.Models;

    /// <summary>
    /// Methods common to both the editor and publisher home controllers.
    /// </summary>
    public class HomeControllerBase : Controller
    {
        private readonly ArticleLogic articleLogic;
        private readonly ApplicationDbContext dbContext;
        private readonly StorageContext storageContext;
        private readonly ILogger logger;
        private readonly PowerBiTokenService powerBiTokenService;
        private readonly string[] powerBiScopeBase = ["https://analysis.windows.net/powerbi/api/.default"];

        /// <summary>
        /// Initializes a new instance of the <see cref="HomeControllerBase"/> class.
        /// </summary>
        /// <param name="articleLogic">Article logic.</param>
        /// <param name="dbContext">Database context.</param>
        /// <param name="storageContext">Storage context.</param>
        /// <param name="logger">Logger service.</param>
        /// <param name="powerBiTokenService">Power BI Token Service.</param>
        public HomeControllerBase(
            ArticleLogic articleLogic,
            ApplicationDbContext dbContext,
            StorageContext storageContext,
            ILogger logger,
            PowerBiTokenService powerBiTokenService)
        {
            this.articleLogic = articleLogic;
            this.dbContext = dbContext;
            this.storageContext = storageContext;
            this.logger = logger;
            this.powerBiTokenService = powerBiTokenService;
        }

        /// <summary>
        /// Gets contents in an article folder.
        /// </summary>
        /// <param name="path">Path to article.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public async Task<IActionResult> CCMS_GetArticleFolderContents(string path = "")
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var articleNumber = await GetArticleNumberFromRequestHeaders();

            if (articleNumber == null)
            {
                return NotFound("Page not found.");
            }

            var contents = await CosmosUtilities.GetArticleFolderContents(storageContext, articleNumber.Value, path);

            return Json(contents);
        }

        /// <summary>
        /// Gets the children of a given page path.
        /// </summary>
        /// <param name="page">UrlPath.</param>
        /// <param name="orderByPub">Ordery by publishing date.</param>
        /// <param name="pageNo">Page number.</param>
        /// <param name="pageSize">Number of rows in each page.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        [EnableCors("AllCors")]
        public async Task<IActionResult> GetTOC(
            string page,
            bool? orderByPub,
            int? pageNo,
            int? pageSize)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var result = await articleLogic.GetTOC(page, pageNo ?? 0, pageSize ?? 10, orderByPub ?? false);
            return Json(result);
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
                return NotFound();
            }

            model.Id = Guid.NewGuid();
            model.Created = DateTimeOffset.UtcNow;
            model.Updated = DateTimeOffset.UtcNow;

            if (ModelState.IsValid)
            {
                var contact = await dbContext.Contacts.FirstOrDefaultAsync(f => f.Email.ToLower() == model.Email.ToLower());

                if (contact == null)
                {
                    dbContext.Contacts.Add(new Data.Contact() { Email = model.Email.ToLower(), FirstName = model.FirstName, LastName = model.LastName, Phone = model.Phone });
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

                return Json(model);
            }

            return BadRequest(ModelState);
        }

        /// <summary>
        /// Gets the power BI token.
        /// </summary>
        /// <param name="reportId">Power BI report ID.</param>
        /// <param name="workspaceId">Power BI workspace (group) ID.</param>
        /// <param name="additionalDataset">Additional dataset ID.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        /// <remarks>This method works only with new Power BI V2 workspace experience.</remarks>
        /// <returns>Returns <see cref="EmbedParams"/> as a Json object.</returns>
        [HttpGet]
        public async Task<IActionResult> CCMS_GET_POWER_BI_TOKEN(Guid? reportId, Guid? workspaceId, Guid? additionalDataset = null)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            if (!reportId.HasValue || !workspaceId.HasValue)
            {
                return NotFound("Report or workspace ID missing or not found.");
            }

            if (powerBiTokenService.IsConfigured)
            {
                // TODO: This is to check security.
                // var articleNumber = await GetArticleNumberFromRequestHeaders();
                if (additionalDataset.HasValue)
                {
                    return Json(await powerBiTokenService.GetEmbedParams(workspaceId.Value, reportId.Value, additionalDataset.Value));
                }
                else
                {
                    return Json(await powerBiTokenService.GetEmbedParams(workspaceId.Value, reportId.Value));
                }
            }

            return BadRequest("Not configured.");
        }

        /// <summary>
        /// Gets an embed token for a Power BI RDL report.
        /// </summary>
        /// <param name="reportId">Report ID.</param>
        /// <param name="workspaceId">Workspace ID.</param>
        /// <returns>Returns an <see cref="EmbedToken"/> as a Json object.</returns>
        [HttpGet]
        public async Task<IActionResult> CCMS_GET_POWER_BI_RDL_TOKEN(Guid reportId, Guid workspaceId)
        {
            if (powerBiTokenService.IsConfigured)
            {
                // TODO: This is to check security.
                var articleNumber = await GetArticleNumberFromRequestHeaders();
                var result = await powerBiTokenService.GetEmbedParams(workspaceId, reportId);
                return Json(result);
            }

            return BadRequest("Not configured.");
        }

        /// <summary>
        /// Searches published articles by keyword or phrase.
        /// </summary>
        /// <param name="searchTxt">Search string.</param>
        /// <returns>JsonResult</returns>
        [HttpPost]
        public async Task<IActionResult> CCMS___SEARCH(string searchTxt)
        {
            if (string.IsNullOrEmpty(searchTxt))
            {
                return BadRequest("Search term is required.");
            }

            var result = await articleLogic.Search(searchTxt);
            return Json(result);
        }

        /// <summary>
        /// Returns a health check.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        [AllowAnonymous]
        [EnableRateLimiting("fixed")]
        public async Task<IActionResult> CCMS_UTILITIES_NET_PING_HEALTH_CHECK()
        {
            try
            {
                _ = await dbContext.Database.CanConnectAsync();
                return Ok();
            }
            catch
            {
            }

            return StatusCode(500);
        }

        private async Task<int?> GetArticleNumberFromRequestHeaders()
        {
            string r = Request.Headers["referer"];
            var url = new Uri(r);

            // This is just for the Editor
            if (url.Query.Contains("articleNumber"))
            {
                var query = url.Query.Split('=');
                return int.Parse(query[1]);
            }
            else if (url.PathAndQuery.ToLower().Contains("editor/ccmscontent"))
            {
                var query = url.PathAndQuery.Split('/');
                return int.Parse(query.LastOrDefault());
            }
            else
            {
                var page = await dbContext.Pages.Select(s => new { s.ArticleNumber, s.UrlPath }).FirstOrDefaultAsync(f => f.UrlPath == url.AbsolutePath.TrimStart('/'));

                if (page == null)
                {
                    return null;
                }

                return page.ArticleNumber;
            }
        }
    }
}

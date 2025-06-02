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
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Cors;
    using Microsoft.AspNetCore.Identity.UI.Services;
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
        private readonly IEmailSender emailSender;

        /// <summary>
        /// Initializes a new instance of the <see cref="HomeControllerBase"/> class.
        /// </summary>
        /// <param name="articleLogic">Article logic.</param>
        /// <param name="dbContext">Database context.</param>
        /// <param name="storageContext">Storage context.</param>
        /// <param name="logger">Logger service.</param>
        /// <param name="powerBiTokenService">Power BI Token Service.</param>
        /// <param name="emailSender">Email sender service.</param>
        public HomeControllerBase(
            ArticleLogic articleLogic,
            ApplicationDbContext dbContext,
            StorageContext storageContext,
            ILogger logger,
            PowerBiTokenService powerBiTokenService,
            IEmailSender emailSender)
        {
            this.articleLogic = articleLogic;
            this.dbContext = dbContext;
            this.storageContext = storageContext;
            this.logger = logger;
            this.powerBiTokenService = powerBiTokenService;
            this.emailSender = emailSender;
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

            var result = await articleLogic.GetTableOfContents(page, pageNo ?? 0, pageSize ?? 10, orderByPub ?? false);
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
                var contactService = new Services.ContactManagementService(dbContext, emailSender, logger, this.HttpContext);

                var result = await contactService.AddContactAsync(model);

                return Json(result);
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
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            if (powerBiTokenService.IsConfigured)
            {
                // TODO: This is to check security.
                var result = await powerBiTokenService.GetEmbedParams(workspaceId, reportId);
                return Json(result);
            }

            return BadRequest("Not configured.");
        }

        /// <summary>
        /// Searches published articles by keyword or phrase.
        /// </summary>
        /// <param name="searchTxt">Search string.</param>
        /// <param name="includeText">Include text in search.</param>
        /// <returns>JsonResult.</returns>
        [HttpPost]
        public async Task<IActionResult> CCMS___SEARCH(string searchTxt, bool? includeText = null)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

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
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

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

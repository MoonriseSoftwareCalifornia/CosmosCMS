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
    using MailChimp.Net.Interfaces;
    using MailChimp.Net;
    using Microsoft.AspNetCore.Antiforgery;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Cors;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.Logging;
    using MailChimp.Net.Models;
    using System.Collections.Generic;

    /// <summary>
    /// Methods common to both the editor and publisher home controllers.
    /// </summary>
    public class HomeControllerBase : Controller
    {
        private readonly ArticleLogic articleLogic;
        private readonly ApplicationDbContext dbContext;
        private readonly StorageContext storageContext;
        private readonly ILogger logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="HomeControllerBase"/> class.
        /// </summary>
        /// <param name="articleLogic">Article logic.</param>
        /// <param name="dbContext">Database context.</param>
        /// <param name="storageContext">Storage context.</param>
        /// <param name="logger">Logger service.</param>
        public HomeControllerBase(
            ArticleLogic articleLogic,
            ApplicationDbContext dbContext,
            StorageContext storageContext,
            ILogger logger)
        {
            this.articleLogic = articleLogic;
            this.dbContext = dbContext;
            this.storageContext = storageContext;
            this.logger = logger;
        }

        /// <summary>
        /// Gets contents in an article folder.
        /// </summary>
        /// <param name="path">Path to article.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public async Task<IActionResult> CCMS_GetArticleFolderContents(string path = "")
        {
            try
            {

                string r = Request.Headers["referer"];
                var url = new Uri(r);

                int articleNumber;

                // This is just for the Editor
                if (url.Query.Contains("articleNumber"))
                {
                    var query = url.Query.Split('=');
                    articleNumber = int.Parse(query[1]);
                }
                else if (url.PathAndQuery.ToLower().Contains("editor/ccmscontent"))
                {
                    var query = url.PathAndQuery.Split('/');
                    articleNumber = int.Parse(query.LastOrDefault());
                }
                else
                {
                    var page = await dbContext.Pages.Select(s => new { s.ArticleNumber, s.UrlPath }).FirstOrDefaultAsync(f => f.UrlPath == url.AbsolutePath.TrimStart('/'));

                    if (page == null)
                    {
                        return Json("[]");
                    }

                    articleNumber = page.ArticleNumber;
                }

                var contents = await CosmosUtilities.GetArticleFolderContents(storageContext, articleNumber, path);

                return Json(contents);
            }
            catch (Exception e)
            {
                logger.LogError(e.Message, e);
                throw;
            }
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
            try
            {
                var result = await articleLogic.GetTOC(page, pageNo ?? 0, pageSize ?? 10, orderByPub ?? false);
                return Json(result);
            }
            catch (Exception e)
            {
                logger.LogError(e.Message, e);
                throw;
            }
        }

        /// <summary>
        /// Post contact information.
        /// </summary>
        /// <param name="model">Contact data model.</param>
        /// <returns>Returns OK if successful.</returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CCMS_POSTCONTACT_INFO(ContactViewModel model)
        {
            try
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

                        var member = new Member { FullName = $"{model.FirstName} {model.LastName}", EmailAddress = contact.Email, StatusIfNew = Status.Subscribed };
                        member.MergeFields.Add("FNAME", model.FirstName);
                        member.MergeFields.Add("LNAME", model.LastName);
                        
                        await manager.Members.AddOrUpdateAsync(mclist.Id, member);
                    }

                    return Json(model);
                }

                return BadRequest(ModelState);

            }
            catch (Exception e)
            {
                logger.LogError(e.Message, e);
                throw;
            }
        }



        /// <summary>
        /// Returns a health check.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        [AllowAnonymous]
        public async Task<IActionResult> CWPS_UTILITIES_NET_PING_HEALTH_CHECK()
        {
            try
            {
                _ = await dbContext.Users.Select(s => s.Id).FirstOrDefaultAsync();
                return Ok();
            }
            catch
            {
            }

            return StatusCode(500);
        }

    }
}

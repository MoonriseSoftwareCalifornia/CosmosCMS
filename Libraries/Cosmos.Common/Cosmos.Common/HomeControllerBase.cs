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
    using Microsoft.AspNetCore.Antiforgery;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Cors;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.EntityFrameworkCore;

    /// <summary>
    /// Methods common to both the editor and publisher home controllers.
    /// </summary>
    public class HomeControllerBase : Controller
    {
        private readonly IAntiforgery antiForgery;
        private readonly ArticleLogic articleLogic;
        private readonly ApplicationDbContext dbContext;
        private readonly StorageContext storageContext;

        /// <summary>
        /// Initializes a new instance of the <see cref="HomeControllerBase"/> class.
        /// </summary>
        /// <param name="articleLogic">Article logic.</param>
        /// <param name="antiForgery">Antiforgery service.</param>
        /// <param name="dbContext">Database context.</param>
        /// <param name="storageContext">Storage context.</param>
        public HomeControllerBase(
            ArticleLogic articleLogic,
            IAntiforgery antiForgery,
            ApplicationDbContext dbContext,
            StorageContext storageContext)
        {
            this.articleLogic = articleLogic;
            this.antiForgery = antiForgery;
            this.dbContext = dbContext;
            this.storageContext = storageContext;
        }

        /// <summary>
        /// Gets contents in an article folder.
        /// </summary>
        /// <param name="path">Path to article.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public async Task<IActionResult> CCMS_GetArticleFolderContents(string path = "")
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
            var result = await articleLogic.GetTOC(page, pageNo ?? 0, pageSize ?? 10, orderByPub ?? false);
            return Json(result);
        }

        /// <summary>
        /// Post contact information.
        /// </summary>
        /// <param name="model">Contact data model.</param>
        /// <returns>Returns OK if successful.</returns>
        [HttpPost]
        public async Task<IActionResult> CCMS_POSTCONTACT_INFO(ContactViewModel model)
        {
            if (model == null)
            {
                return NotFound();
            }
            if (await IsAntiForgeryTokenValid())
            {

                model.Id = Guid.NewGuid();
                model.Created = DateTimeOffset.UtcNow;
                model.Updated = DateTimeOffset.UtcNow;

                if (ModelState.IsValid)
                {
                    var contact = await dbContext.Contacts.FirstOrDefaultAsync(f => f.Email.ToLower() == model.Email.ToLower());

                    if (contact == null)
                    {
                        dbContext.Contacts.Add(new Contact() { Email = model.Email.ToLower(), Name = model.Name, Phone = model.Phone });
                    }
                    else
                    {
                        contact.Updated = DateTimeOffset.UtcNow;
                        contact.Email = model.Email.ToLower();
                        contact.Name = model.Name;
                        contact.Phone = model.Phone;
                    }

                    await dbContext.SaveChangesAsync();

                    return Json(model);
                }

                return BadRequest(ModelState);
            }

            return Unauthorized();
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

        private async Task<bool> IsAntiForgeryTokenValid()
        {
            try
            {
                await antiForgery.ValidateRequestAsync(this.HttpContext);
                return true;
            }
            catch (AntiforgeryValidationException e)
            {
                var d = e;
                return false;
            }
        }
    }
}

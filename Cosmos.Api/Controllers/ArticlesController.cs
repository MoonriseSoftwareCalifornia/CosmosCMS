using Cosmos.BlobService;
using Cosmos.Common;
using Cosmos.Common.Data.Logic;
using Cosmos.Common.Models;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;

namespace Cosmos.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ArticlesController : ControllerBase
    {
        private readonly ArticleLogic articleLogic;

        /// <summary>
        /// Initializes a new instance of the <see cref="ArticlesController"/> class.
        /// </summary>
        /// <param name="articleLogic">Article logic service.</param>
        /// <param name="storageContext">Storage context.</param>
        public ArticlesController(ArticleLogic articleLogic)
        {
            this.articleLogic = articleLogic;
        }

        /// <summary>
        /// Gets an article (page) by URL, and includes the layout if requested.
        /// </summary>
        /// <param name="path"></param>
        /// <param name="includeLayout"></param>
        /// <returns></returns>
        [HttpGet("GetArticle")]
        public async Task<ArticleViewModel?> GetArticle(string path, bool includeLayout)
        {
            var article = await articleLogic.GetPublishedPageByUrl(path, includeLayout: includeLayout);

            if (article == null)
            {
                Response.StatusCode = StatusCodes.Status404NotFound;
                return null;
            }
            return article;
        }

        /// <summary>
        /// Gets just the layout.
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        [HttpGet("GetLayout")]
        public async Task<LayoutViewModel?> GetLayout(string path)
        {
            var layout = await articleLogic.GetDefaultLayout();
            if (layout == null)
            {
                Response.StatusCode = StatusCodes.Status404NotFound;
                return null;
            }
            return layout;
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
        [HttpGet("GetTOC")]
        public async Task<TableOfContents?> GetTOC(
            string page,
            bool? orderByPub,
            int? pageNo,
            int? pageSize)
        {
            if (!ModelState.IsValid)
            {
                Response.StatusCode = StatusCodes.Status400BadRequest;
                return null;
            }

            var result = await articleLogic.GetTableOfContents(page, pageNo ?? 0, pageSize ?? 10, orderByPub ?? false);
            return result;
        }
    }
}

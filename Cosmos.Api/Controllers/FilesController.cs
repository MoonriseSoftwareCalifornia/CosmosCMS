using Cosmos.BlobService;
using Cosmos.Common;
using Cosmos.Common.Data.Logic;
using Microsoft.AspNetCore.Mvc;

namespace Cosmos.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class FilesController : ControllerBase
    {
        private readonly StorageContext storageContext;
        /// <summary>
        /// Initializes a new instance of the <see cref="ArticlesController"/> class.
        /// </summary>
        /// <param name="articleLogic">Article logic service.</param>
        /// <param name="storageContext">Storage context.</param>
        public FilesController(StorageContext storageContext)
        {
            this.storageContext = storageContext;
        }

        /// <summary>
        /// Gets a file from blob storage.
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        [HttpGet("GetFile")]
        public async Task<FileStreamResult?> GetFile(string path)
        {

            var client = storageContext.GetAppendBlobClient(path);

            if (client == null)
            {
                Response.StatusCode = StatusCodes.Status404NotFound;
                return null;
            }

            if (await client.ExistsAsync())
            {
                try
                {
                    var properties = await client.GetPropertiesAsync();
                    return File(
                        fileStream: await client.OpenReadAsync(), 
                        contentType: properties.Value.ContentType, 
                        lastModified: properties.Value.LastModified, 
                        entityTag: new Microsoft.Net.Http.Headers.EntityTagHeaderValue(properties.Value.ETag.ToString()));
                }
                catch (Exception)
                {
                    Response.StatusCode = StatusCodes.Status500InternalServerError;
                    return null;
                }
            }

            Response.StatusCode = StatusCodes.Status404NotFound;
            return null;
        }

        /// <summary>
        /// Gets contents in an article folder.
        /// </summary>
        /// <param name="path">Article Number.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        [HttpGet("GetArticleFolderContents")]
        public async Task<List<FileManagerEntry>> GetArticleFolderContents(int? articleNumber, string path = "")
        {
            if (!ModelState.IsValid)
            {
                Response.StatusCode = StatusCodes.Status400BadRequest;
                return new List<FileManagerEntry>();
            }

            if (articleNumber == null)
            {
                Response.StatusCode = StatusCodes.Status404NotFound;
                return new List<FileManagerEntry>();
            }

            var contents = await CosmosUtilities.GetArticleFolderContents(storageContext, articleNumber.Value, path);

            return contents;
        }
    }
}

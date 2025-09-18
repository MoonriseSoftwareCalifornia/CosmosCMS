// <copyright file="StaticProxyController.cs" company="Moonrise Software, LLC">
// Copyright (c) Moonrise Software, LLC. All rights reserved.
// Licensed under the GNU Public License, Version 3.0 (https://www.gnu.org/licenses/gpl-3.0.html)
// See https://github.com/MoonriseSoftwareCalifornia/CosmosCMS
// for more information concerning the license and the contributors participating to this project.
// </copyright>
namespace Cosmos.Publisher.Controllers
{
    using System.Net.Mime;
    using Cosmos.BlobService;
    using Cosmos.Publisher.Models;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Extensions.Caching.Memory;

    /// <summary>
    /// Static proxy controller.
    /// </summary>
    public class StaticProxyController : Controller
    {
        private readonly StorageContext storageContext;
        private readonly IMemoryCache memoryCache;

        /// <summary>
        /// Initializes a new instance of the <see cref="StaticProxyController"/> class.
        /// </summary>
        /// <param name="storageContext">The storage context used to manage and access data.</param>
        /// <param name="memoryCache">Memory cache.</param>
        public StaticProxyController(StorageContext storageContext, IMemoryCache memoryCache)
        {
            this.storageContext = storageContext;
            this.memoryCache = memoryCache;
        }

        /// <summary>
        ///  Retrieves and serves a static file based on the request path.
        /// </summary>
        /// <returns>Returns a file.</returns>
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            string path = string.IsNullOrWhiteSpace(HttpContext.Request.Path) || HttpContext.Request.Path == "/" ? "index.html" : HttpContext.Request.Path;
            
            try
            {
                memoryCache.TryGetValue(path, out FileCacheObject fileCacheObject);

                if (fileCacheObject == null) 
                {
                    var properties = await storageContext.GetFileAsync(path);
                    if (properties == null)
                    {
                        return NotFound();
                    }

                    fileCacheObject = new FileCacheObject(properties);

                    using var fileStream = await storageContext.GetStreamAsync(path);
                    using var ms = new MemoryStream();
                    await fileStream.CopyToAsync(ms);
                    fileCacheObject.FileData = ms.ToArray();

                    memoryCache.Set(path, fileCacheObject, new MemoryCacheEntryOptions()
                        .SetAbsoluteExpiration(TimeSpan.FromSeconds(10)));
                }

                var textTypes = new[]
                {
                    MediaTypeNames.Text.Plain,
                    MediaTypeNames.Text.Html,
                    MediaTypeNames.Text.Xml,
                    MediaTypeNames.Application.Json,
                    "application/javascript",
                    "application/xml",
                    "text/css",
                    "image/svg+xml",
                };

                if (textTypes.Contains(fileCacheObject.ContentType))
                {
                    // Convert byte[] to string for text content
                    return Content(System.Text.Encoding.UTF8.GetString(fileCacheObject.FileData), fileCacheObject.ContentType);
                }

                var contentType = Utilities.GetContentType(fileCacheObject.Name);

                return File(
                    fileStream: new MemoryStream(fileCacheObject.FileData),
                    contentType: contentType,
                    lastModified: fileCacheObject.ModifiedUtc,
                    entityTag: null);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                return NotFound();
            }
        }
    }
}

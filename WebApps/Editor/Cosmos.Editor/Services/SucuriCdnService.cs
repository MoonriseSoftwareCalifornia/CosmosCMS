// <copyright file="SucuriCdnService.cs" company="Moonrise Software, LLC">
// Copyright (c) Moonrise Software, LLC. All rights reserved.
// Licensed under the GNU Public License, Version 3.0 (https://www.gnu.org/licenses/gpl-3.0.html)
// See https://github.com/MoonriseSoftwareCalifornia/CosmosCMS
// for more information concerning the license and the contributors participating to this project.
// </copyright>

using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Cosmos.Editor.Controllers;

namespace Cosmos.Editor.Services
{
    /// <summary>
    /// Sucuri CDN service API class.
    /// </summary>
    public class SucuriCdnService
    {
        private readonly string sucuriApiKey;
        private readonly string sucuriApiSecret;

        /// <summary>
        /// Initializes a new instance of the <see cref="SucuriCdnService"/> class.
        /// </summary>
        /// <param name="sucuriApiKey">API Key.</param>
        /// <param name="sucuriApiSecret">API Secret.</param>
        public SucuriCdnService(string sucuriApiKey, string sucuriApiSecret)
        {
            this.sucuriApiKey = sucuriApiKey;
            this.sucuriApiSecret = sucuriApiSecret;
        }

        /// <summary>
        /// Purge content from the Sucuri CDN.
        /// </summary>
        /// <param name="paths">URL path.</param>
        /// <returns>ArmOperation.</returns>
        public async Task<List<CdnResult>> PurgeContentAsync(string[] paths)
        {
            var responses = new List<CdnResult>();

            if (paths.Length == 0 || paths.Length > 20 || (paths.Length == 1 && paths[0] == "/"))
            {
                responses.Add(await PurgeContentAsync(string.Empty));
            }
            else if (paths.Length == 1 || paths.Length > 20)
            {
                responses.Add(await PurgeContentAsync(paths[0]));
            }
            else
            {
                foreach (var path in paths)
                {
                    responses.Add(await PurgeContentAsync(path));
                }
            }

            return responses;
        }

        private async Task<CdnResult> PurgeContentAsync(string path)
        {
            using var client = new HttpClient();
            var requestUri = $"https://waf.sucuri.net/api?k={sucuriApiKey}&s={sucuriApiSecret}&a=clearcache";
            string json = string.Empty;
            if (!string.IsNullOrEmpty(path))
            {
                requestUri += $"&file={path}";
            }

            var result = await client.GetAsync(requestUri);

            var response = await result.Content.ReadAsStringAsync();

            return new CdnResult
            {
                ClientRequestId = Guid.NewGuid().ToString(),
                Id = Guid.NewGuid().ToString(),
                IsSuccessStatusCode = result.IsSuccessStatusCode,
                Status = result.StatusCode,
                ReasonPhrase = result.ReasonPhrase,
                EstimatedFlushDateTime = DateTimeOffset.UtcNow.AddMinutes(2)
            };
        }
    }
}

// <copyright file="AzureWebAppPublisher.cs" company="Moonrise Software, LLC">
// Copyright (c) Moonrise Software, LLC. All rights reserved.
// Licensed under the GNU Public License, Version 3.0 (https://www.gnu.org/licenses/gpl-3.0.html)
// See https://github.com/MoonriseSoftwareCalifornia/CosmosCMS
// for more information concerning the license and the contributors participating to this project.
// </copyright>

namespace CosmosCMS.Editor.Services
{
    using System;
    using System.IO;
    using System.Threading.Tasks;
    using Azure.Identity;
    using RestSharp;

    /// <summary>
    /// A service for publishing files to the wwwroot directory of an Azure Web App using the Kudu API.
    /// </summary>
    internal class AzureWebAppPublisher
    {
        private readonly string webAppName;
        private readonly string resourceGroupName;
        private readonly string subscriptionId;
        private readonly DefaultAzureCredential credential;

        /// <summary>
        /// Initializes a new instance of the <see cref="AzureWebAppPublisher"/> class with the specified Azure Web App details.
        /// </summary>
        /// <param name="webAppName">Web app name.</param>
        /// <param name="resourceGroupName">Resource group name.</param>
        /// <param name="subscriptionId">Subscription ID.</param>
        /// <param name="credential">Azure default credential.</param>
        public AzureWebAppPublisher(string webAppName, string resourceGroupName, string subscriptionId, DefaultAzureCredential credential)
        {
            this.webAppName = webAppName;
            this.resourceGroupName = resourceGroupName;
            this.subscriptionId = subscriptionId;
            this.credential = credential;
        }

        /// <summary>
        /// Publishes a file to the wwwroot directory of the Azure Web App using the Kudu API.
        /// </summary>
        /// <param name="filePath">File-path to place file.</param>
        /// <returns>Task.</returns>
        public async Task PublishFileToWwwrootAsync(string filePath)
        {
            var accessToken = await credential.GetTokenAsync(new Azure.Core.TokenRequestContext(new[] { "https://management.azure.com/.default" }));

            var kuduApiUrl = $"https://{webAppName}.scm.azurewebsites.net/api/vfs/site/wwwroot/{Path.GetFileName(filePath)}";
            var client = new RestClient(kuduApiUrl);
            var request = new RestRequest(filePath, Method.Put);
            request.AddHeader("Authorization", $"Bearer {accessToken.Token}");
            request.AddFile("file", filePath);

            var response = await client.ExecuteAsync(request);

            if (response.IsSuccessful)
            {
                Console.WriteLine($"File {filePath} published to wwwroot directory.");
            }
            else
            {
                Console.WriteLine($"Failed to publish file: {response.ErrorMessage}");
            }
        }

        /// <summary>
        /// Deletes a file from the wwwroot directory of the Azure Web App using the Kudu API.
        /// </summary>
        /// <param name="filePath">File path to delete.</param>
        /// <returns>A <see cref="Task"/> representing the result of the asynchronous operation.</returns>
        public async Task DeleteFileAtWwwrootAsync(string filePath)
        {
            var accessToken = await credential.GetTokenAsync(new Azure.Core.TokenRequestContext(new[] { "https://management.azure.com/.default" }));

            var kuduApiUrl = $"https://{webAppName}.scm.azurewebsites.net/api/vfs/site/wwwroot/{filePath}";
            var client = new RestClient(kuduApiUrl);
            var request = new RestRequest(filePath, Method.Delete);
            request.AddHeader("Authorization", $"Bearer {accessToken.Token}");

            var response = await client.ExecuteAsync(request);

            if (response.IsSuccessful)
            {
                Console.WriteLine($"File {filePath} deleted from wwwroot directory.");
            }
            else
            {
                Console.WriteLine($"Failed to delete file: {response.ErrorMessage}");
            }
        }
    }
}

// <copyright file="Cosmos___StoryDeskController.cs" company="Moonrise Software, LLC">
// Copyright (c) Moonrise Software, LLC. All rights reserved.
// Licensed under the GNU Public License, Version 3.0 (https://www.gnu.org/licenses/gpl-3.0.html)
// See https://github.com/MoonriseSoftwareCalifornia/CosmosCMS
// for more information concerning the license and the contributors participating to this project.
// </copyright>

namespace Cosmos.Editor.Controllers
{
    using Azure.Core;
    using Azure.Identity;
    using Cosmos.BlobService;
    using Cosmos.Cms.Common.Services.Configurations;
    using Cosmos.Cms.Services;
    using Cosmos.Common.Data;
    using Cosmos.Common.Models;
    using Cosmos.Common.Services;
    using Cosmos.DynamicConfig;
    using Cosmos.Editor.Data;
    using Cosmos.Editor.Data.Logic;
    using Cosmos.EmailServices;
    using MailChimp.Net.Models;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Identity;
    using Microsoft.AspNetCore.Identity.UI.Services;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.Caching.Memory;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Options;
    using Microsoft.Graph;
    using Microsoft.Graph.Models;
    using SixLabors.ImageSharp;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    /// <summary>
    /// Controller for handling StoryDesk operations using Microsoft Graph API.
    /// </summary>
    [AllowAnonymous]
    public class Cosmos___StoryDeskController : ControllerBase
    {
        private readonly StoryDeskConfig storyDeskConfig;
        private readonly TokenCredential tokenCredential;
        private readonly IConfiguration configuration;
        private readonly DynamicConfigDbContext configDbContext;
        private readonly StoryDeskDbContext storyDeskDbContext;
        private readonly ILogger<Cosmos___StoryDeskController> logger;
        private readonly StorageContext storageContext;
        private readonly ICosmosEmailSender emailSender;
        private readonly IMemoryCache memoryCache;
        private readonly IOptions<CosmosConfig> config;
        private readonly IViewRenderService viewRenderService;
        private readonly IHttpContextAccessor accessor;
        private readonly IEditorSettings settings;

        /// <summary>
        /// Initializes a new instance of the <see cref="Cosmos___StoryDeskController"/> class.
        /// </summary>
        /// <param name="configuration">Website configuration.</param>
        /// <param name="configDbContext">Configuration DB context.</param>
        /// <param name="storyDeskDbContext">Story desk DB context.</param>
        /// <param name="logger">Log service.</param>
        /// <param name="memoryCache">Memory cache.</param>
        /// <param name="storageContext">Storage context.</param>
        /// <param name="emailSender">Email services.</param>
        /// <param name="config">Cosmos options.</param>
        /// <param name="viewRenderService">View rendering service.</param>
        /// <param name="accessor">Http context access.</param>
        /// <param name="settings">Editor settings.</param>
        /// <exception cref="ArgumentNullException">Null argument exception.</exception>
        public Cosmos___StoryDeskController(
            IConfiguration configuration,
            DynamicConfigDbContext configDbContext,
            StoryDeskDbContext storyDeskDbContext,
            ILogger<Cosmos___StoryDeskController> logger,
            IMemoryCache memoryCache,
            StorageContext storageContext,
            IEmailSender emailSender,
            IOptions<CosmosConfig> config,
            IViewRenderService viewRenderService,
            IHttpContextAccessor accessor,
            IEditorSettings settings)
        {
            this.storageContext = storageContext ?? throw new ArgumentNullException(nameof(storageContext));

            this.configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));

            this.storyDeskConfig = this.configuration?.GetSection("StoryDesk")
                .Get<StoryDeskConfig>() ?? throw new ArgumentNullException("StoryDesk");

            tokenCredential = new ClientSecretCredential(
                this.storyDeskConfig.TenantId,
                this.storyDeskConfig.ClientId,
                this.storyDeskConfig.ClientSecret);

            this.configDbContext = configDbContext ?? throw new ArgumentNullException(nameof(configDbContext));
            this.storyDeskDbContext = storyDeskDbContext;
            this.logger = logger;
            this.emailSender = (ICosmosEmailSender)emailSender;
            this.memoryCache = memoryCache ?? throw new ArgumentNullException(nameof(memoryCache));
            this.config = config ?? throw new ArgumentNullException(nameof(config));
            this.viewRenderService = viewRenderService ?? throw new ArgumentNullException(nameof(viewRenderService));
            this.accessor = accessor;
            this.settings = settings;
        }

        private string[] ValidImageMimeTypes
        {
            get
            {
                return new string[] { "image/gif", "image/jpeg", "image/png", "image/webp" };
            }
        }

        /// <summary>
        /// Retrieves an email from the shared mailbox using Microsoft Graph API.
        /// </summary>
        /// <param name="id">Webhook Key.</param>
        /// <returns>Task.</returns>
        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> Index(string id)
        {
            if (id != storyDeskConfig.ApiKey)
            {
                logger.LogWarning($"Invalid StoryDesk API key '{id}' provided.");
                return Ok(); // Invalid key, do not proceed
            }

            var graphClient = new GraphServiceClient(tokenCredential);

            // With this line to retrieve only unread messages:
            var messages = await graphClient.Users[storyDeskConfig.Mailbox].Messages
                .GetAsync(g =>
                {
                    g.QueryParameters.Filter = "isRead eq false";
                    g.QueryParameters.Select = new[] { "subject", "from", "internetMessageHeaders", "attachments", "body" };
                    g.QueryParameters.Expand = new[] { "attachments" };
                });

            if (messages == null || messages.Value == null)
            {
                // No new messages found.
                return Ok();
            }

            if (messages.Value.Count < 1)
            {
                logger.LogInformation("No unread messages found in the shared mailbox.");
                return Ok(); // No unread messages to process
            }

            var configs = await storyDeskDbContext
                .WebsiteAuthors
                .ToListAsync();

            foreach (var message in messages.Value)
            {
                var websites = configs
                    .Where(c => c.EmailAddress.Equals(message.From.EmailAddress.Address, StringComparison.OrdinalIgnoreCase))
                    .ToList();

                // Mark the email as read and delete it if necessary.
                if (websites.Count < 1)
                {
                    logger.LogWarning("No websites found for email address {EmailAddress}.", message.From.EmailAddress.Address);
                    continue; // If no websites are found, do not process the email
                }

                await ProcessMessage(message, configs);

                // await MarkEmailAsReadAsync(graphClient, message.Id, delete: true);
            }

            return Ok();
        }

        private string GetTitleFromMessage(Microsoft.Graph.Models.Message message)
        {
            if (string.IsNullOrEmpty(message.Subject))
            {
                throw new ArgumentException("Email subject cannot be empty or null");
            }

            var subjectParts = message.Subject.Split('|');
            if (subjectParts.Length > 1)
            {
                return subjectParts[1];
            }

            return subjectParts[0];
        }

        private string GetHtmlFromMessage(Microsoft.Graph.Models.Message message)
        {
            var html = string.Empty;
            if (message.Body.ContentType == BodyType.Html)
            {
                var emailContent = new HtmlAgilityPack.HtmlDocument();
                emailContent.LoadHtml(message.Body.Content);

                RemoveAllCustomCss(emailContent);

                // Use the inner HTML of the <body> tag
                var body = emailContent.DocumentNode.SelectSingleNode("//body");
                if (body != null)
                {
                    html = body.InnerHtml;
                }
                else
                {
                    // Fallback to the entire document if no <body> tag is found
                    html = emailContent.DocumentNode.OuterHtml;
                }
            }
            else
            {
                // Convert plain text to HTML
                html = $"<p>{message.Body.Content.Replace(Environment.NewLine, "</p><p>")}</p>";
            }

            return html;
        }

        private StoryAuthorConfig GetConfigFromMessage(Microsoft.Graph.Models.Message message, List<StoryAuthorConfig> websites)
        {
            StoryAuthorConfig config = null;

            if (string.IsNullOrEmpty(message.Subject))
            {
                throw new ArgumentException("Email subject cannot be empty or null");
            }

            var subjectParts = message.Subject.Split('|');

            switch (subjectParts.Length)
            {
                case 1:
                    config = websites.FirstOrDefault(
                        w => w.EmailAddress.Equals(message.From.EmailAddress.Address, StringComparison.OrdinalIgnoreCase));
                    break;
                case 2:
                    {
                        var domainName = subjectParts[0];
                        config = websites.FirstOrDefault(
                            w => w.EmailAddress.Equals(message.From.EmailAddress.Address, StringComparison.OrdinalIgnoreCase)
                            && w.WebsiteDomain.Equals(domainName, StringComparison.OrdinalIgnoreCase));
                    }

                    break;
                default:
                    {
                        var domainName = subjectParts[0];
                        var templateName = subjectParts[2];
                        config = websites.FirstOrDefault(
                            w => w.EmailAddress.Equals(message.From.EmailAddress.Address, StringComparison.OrdinalIgnoreCase)
                            && w.WebsiteDomain.Equals(domainName, StringComparison.OrdinalIgnoreCase)
                            && w.TemplateName.Equals(templateName, StringComparison.OrdinalIgnoreCase));
                    }

                    break;
            }

            if (config == null)
            {
                throw new Exception("Did not find matching configuration for message.");
            }

            return config;
        }

        private async Task ProcessMessage(Microsoft.Graph.Models.Message message, List<StoryAuthorConfig> websites)
        {
            if (IsLikelySpamOrPhish(message))
            {
                logger.LogWarning("Message {MessageId} is likely spam or phishing. Skipping processing.", message.Id);
                return; // If the message is likely spam/phish, do not process it
            }

            if (message == null || websites == null || websites.Count < 1)
            {
                logger.LogWarning("Message or websites list is null or empty.");
                return; // Nothing to process
            }

            var title = GetTitleFromMessage(message);
            var body = GetHtmlFromMessage(message);
            var storyConfig = GetConfigFromMessage(message, websites);

            var connection = await configDbContext.Connections
                .FirstOrDefaultAsync(c => c.Id == storyConfig.ConnectionId);

            // Get the database context for the website.
            var dbContext = ApplicationDbContextUtilities.GetApplicationDbContext(connection);

            // Create an instance of ArticleEditLogic to handle article creation and updates.
            var articleEditLogic = new ArticleEditLogic(
                dbContext,
                memoryCache,
                config,
                viewRenderService,
                storageContext,
                accessor.HttpContext.RequestServices.GetRequiredService<ILogger<ArticleEditLogic>>(),
                accessor,
                settings);

            // Check for the author.
            var normalizedEmail = message.From.EmailAddress.Address.ToUpper();
            var user = await dbContext.Users.FirstOrDefaultAsync(f => f.NormalizedEmail == normalizedEmail && f.EmailConfirmed == true);

            if (user == null)
            {
                logger.LogWarning($"No user found for email address {message.From.EmailAddress.Address}.");
                return; // No user found, do not proceed
            }

            var allTemplates = await dbContext.Templates.ToListAsync();

            var template = dbContext.Templates
                .FirstOrDefault(t => t.Id == storyConfig.TemplateId);

            if (template == null)
            {
                logger.LogError(exception: new Exception($"Template with ID {storyConfig.TemplateId} not found for website {storyConfig.WebsiteDomain}."), message: $"Template with ID {storyConfig.TemplateId} not found for website {storyConfig.WebsiteDomain}.");
                return; // Template not found, do not proceed
            }

            var rootPath = storyConfig.Path.Trim('/');
            var title = rootPath + "/" + (subjectParts.Length > 1 ? subjectParts[1].Trim() : subject);

            var articleUrl = articleEditLogic.NormailizeArticleUrl(title);

            // Retrieve the article by URL.
            var article = await articleEditLogic.GetArticleByUrl(articleUrl);

            // If the article does not exist, create it.
            article ??= await articleEditLogic.CreateArticle(
                title,
                Guid.Parse(user.Id),
                storyConfig.TemplateId);

            HtmlAgilityPack.HtmlDocument doc = new HtmlAgilityPack.HtmlDocument();
            doc.LoadHtml(article.Content);

            var contentNode = doc.DocumentNode.SelectSingleNode("//*[@data-storydesk-id='content']");
           
            var titleNode = doc.DocumentNode.SelectSingleNode("//*[@data-storydesk-id='title']");
            var imageNode = doc.DocumentNode.SelectSingleNode("//*[@data-storydesk-id='image']");

            titleNode.InnerHtml = subjectParts.Length > 1 ? subjectParts[1].Trim() : subject.Trim();


            // To select the <body> element using HtmlAgilityPack's HtmlDocument:
            var bodyNode = storyDoc.DocumentNode.SelectSingleNode("//body");

            contentNode.InnerHtml = bodyNode.InnerHtml;

            var attachments = ExtractFileAttachments(message);

            if (attachments != null && attachments.Count > 0)
            {
                var attachment = attachments.FirstOrDefault();
                if (attachment.ContentBytes.Length == 0)
                {
                    logger.LogWarning($"Attachment {attachment.Name} has no content bytes.");
                }
                else
                {
                    var relativePath = $"/pub/articles/{article.ArticleNumber}/{attachment.Name}";

                    var byteArray = attachment.ContentBytes.ToArray();

                    using var memoryStream = new MemoryStream(byteArray);
                    var format = SixLabors.ImageSharp.Image.DetectFormat(memoryStream);
                    var image = await SixLabors.ImageSharp.Image.LoadAsync(memoryStream);

                    storageContext.AppendBlob(
                        memoryStream,
                        new BlobService.Models.FileUploadMetaData()
                        {
                            ChunkIndex = 0,
                            ContentType = attachment.ContentType,
                            FileName = attachment.Name,
                            RelativePath = relativePath,
                            TotalChunks = 1,
                            TotalFileSize = memoryStream.Length,
                            UploadUid = Guid.NewGuid().ToString(),
                            ImageHeight = image.Height.ToString(),
                            ImageWidth = image.Width.ToString(),
                        },
                        "block");

                    imageNode.SetAttributeValue("src", relativePath);
                }
            }

            // Set the article content to the processed HTML.
            article.Content = storyDoc.DocumentNode.OuterHtml;
            article.Published = null;

            await articleEditLogic.SaveArticle(article, Guid.Parse(user.Id));

            var builder = new StringBuilder();
            builder.AppendLine($"<p>New web page created from email: {message.Subject}.</p>");

            var uri = new Uri(storyConfig.WebsiteDomain);

            var oneTimeTokenProvider = new OneTimeTokenProvider<IdentityUser>(dbContext, logger);
            var oneTimeToken = await oneTimeTokenProvider.GenerateAsync(user);

            var returnUrl = $"https://{Request.Host.Host}/Identity/Account/Login?ccmsopt={oneTimeToken}&website={uri.Host}&returnUrl=/Editor/Edit/{article.ArticleNumber}&ccmsemail={user.Email}";

            builder.AppendLine($"<p><a href='{returnUrl}'>Click here to open web page to edit and publish.</a></p>");

            await this.emailSender.SendEmailAsync(message.From.EmailAddress.Address, "New web page created.", builder.ToString());

            logger.LogInformation("Article {ArticleNumber} created from email: {Subject}", article.ArticleNumber, message.Subject);

            return;
        }

        /// <summary>
        /// Marks an email as read and optionally deletes it from the shared mailbox.
        /// </summary>
        /// <param name="graphClient">The GraphServiceClient instance.</param>
        /// <param name="messageId">The message ID (Graph ID, not InternetMessageId).</param>
        /// <param name="delete">If true, deletes the message after marking as read.</param>
        private async Task MarkEmailAsReadAsync(GraphServiceClient graphClient, string messageId, bool delete = false)
        {
            // Mark as read
            await graphClient.Users[storyDeskConfig.Mailbox].Messages[messageId]
                .PatchAsync(new Message
                {
                    IsRead = true
                });

            if (delete)
            {
                await graphClient.Users[storyDeskConfig.Mailbox].Messages[messageId]
                    .DeleteAsync();
            }
        }

        // Pseudocode plan:
        // 1. Identify all <style> elements in the document and remove them.
        // 2. Remove all "style" attributes from all elements in the document.
        // 3. Optionally, remove <link rel="stylesheet"> elements if you want to remove external CSS as well.
        private void RemoveAllCustomCss(HtmlAgilityPack.HtmlDocument storyDoc)
        {
            // Remove all <style> elements
            var styleNodes = storyDoc.DocumentNode.SelectNodes("//style");
            if (styleNodes != null)
            {
                foreach (var styleNode in styleNodes)
                {
                    styleNode.Remove();
                }
            }

            // Remove all "style" and "class" attributes from all elements
            var nodesWithAttributes = storyDoc.DocumentNode.SelectNodes("//*[@style or @class]");
            if (nodesWithAttributes != null)
            {
                foreach (var node in nodesWithAttributes)
                {
                    node.Attributes.Remove("style");
                    node.Attributes.Remove("class");
                }
            }

            // Optionally, remove <link rel="stylesheet"> elements
            var linkNodes = storyDoc.DocumentNode.SelectNodes("//link[@rel='stylesheet']");
            if (linkNodes != null)
            {
                foreach (var linkNode in linkNodes)
                {
                    linkNode.Remove();
                }
            }
        }

        /// <summary>
        /// Checks the message headers to determine if the message is likely to be spam or phishing.
        /// Returns true if the message is NOT likely to be spam/phish, false otherwise.
        /// </summary>
        /// <param name="message">The Microsoft Graph Message object.</param>
        /// <returns>True if message is not likely spam/phish, false otherwise.</returns>
        private bool IsLikelySpamOrPhish(Message message)
        {
            if (message == null || message.InternetMessageHeaders == null)
            {
                return true;
            }

            // Common anti-spam/anti-phish headers
            var headers = message.InternetMessageHeaders
                .Where(w => string.IsNullOrEmpty(w.Name) == false && string.IsNullOrEmpty(w.Value) == false)
                .ToList();

            if (HeaderContains(headers, "X-Microsoft-Antispam", new string[] { "BCL:8", "BCL:9" }))
            {
                return true; // High Bulk Complaint Level (BCL) indicates spam or phishing.
            }

            if (HeaderContains(headers, "X-Microsoft-Antispam-Message-Info", new string[] { "Phish" }))
            {
                return true; // Message marked as phishing.
            }

            if (HeaderContains(headers, "X-Forefront-Antispam-Report", new string[] { "SFV:SPM", "SFV:SKS", "SFV:SKB" }))
            {
                return true; // Forefront anti-spam report indicates spam or phishing.
            }

            if (HeaderContains(headers, "X-Microsoft-Antiphish", new string[] { "Phish" }))
            {
                return true; // Microsoft anti-phish header indicates phishing.
            }

            if (HeaderContains(headers, "X-Spam-Status", new string[] { "Yes" }))
            {
                return true; // Spam status header indicates spam.
            }

            var authResults = headers.Where(w => w.Name.Equals("Authentication-Results", StringComparison.CurrentCultureIgnoreCase))
                .Select(w => w.Value)
                .FirstOrDefault();

            // Optionally, check for SPF/DKIM/DMARC failures
            if (authResults.Contains("spf=fail") && authResults.Contains("dmarc=fail") && authResults.Contains("dkim=fail"))
            {
                return true;
            }

            // If none of the above indicate spam/phish, consider it not likely spam/phish
            return false;
        }

        private bool HeaderContains(List<InternetMessageHeader> headers, string key, string[] values)
        {
            var antispam = headers.Where(w =>
                w.Name.Equals(key, StringComparison.CurrentCultureIgnoreCase)).ToList();

            return antispam.Any(w => w.Value != null && values.Any(k => w.Value.Contains(k, StringComparison.CurrentCultureIgnoreCase)));
        }

        /// <summary>
        /// Extracts all file attachments from a Microsoft Graph Message.
        /// </summary>
        /// <param name="message">The Microsoft Graph Message object.</param>
        /// <returns>A list of FileAttachment objects.</returns>
        private List<FileAttachment> ExtractFileAttachments(Message message)
        {
            var result = new List<FileAttachment>();
            if (message?.Attachments == null || message.Attachments.Count == 0)
            {
                return result;
            }

            foreach (var attachment in message.Attachments)
            {
                if (attachment is FileAttachment fileAttachment && ValidImageMimeTypes.Contains(attachment.ContentType))
                {
                    result.Add(fileAttachment);
                }
            }

            return result;
        }
    }
}
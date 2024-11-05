// <copyright file="CosmosEmailServiceProvider.cs" company="Moonrise Software, LLC">
// Copyright (c) Moonrise Software, LLC. All rights reserved.
// Licensed under the GNU Public License, Version 3.0 (https://www.gnu.org/licenses/gpl-3.0.html)
// See https://github.com/MoonriseSoftwareCalifornia/CosmosCMS
// for more information concerning the license and the contributors participating to this project.
// </copyright>

namespace Cosmos.EmailServices
{
    using System;
    using System.Linq;
    using System.Threading.Tasks;
    using Azure.Identity;
    using Cosmos.Common.Data;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Options;
    using Newtonsoft.Json;

    /// <summary>
    /// Launches email service from database configuration.
    /// </summary>
    /// <remarks>Do not use this if Email configuration is set in environment variables or secrets.</remarks>
    public class CosmosEmailServiceProvider : ICosmosEmailSender
    {
        private readonly AzureCommunicationEmailSender? azureCommunicationEmailSender;
        private readonly SendGridEmailSender? sendGridEmailSender;
        private readonly CosmosNoOpEmailSender? cosmosNoOpEmailSender;
        private readonly SmtpEmailSender? smtpEmailSender;

        /// <summary>
        /// Initializes a new instance of the <see cref="CosmosEmailServiceProvider"/> class.
        /// </summary>
        /// <param name="dbContext">Database context.</param>
        /// <param name="defaultAzureCredential">Default Azure Credential.</param>
        /// <param name="logger">Log service.</param>
        public CosmosEmailServiceProvider(ApplicationDbContext dbContext, DefaultAzureCredential defaultAzureCredential, ILogger logger)
        {
            SendResult = new SendResult();

            var settings = dbContext.Settings.Where(f => f.Group == "EMAIL").ToListAsync().Result;
            if (settings.Count == 0)
            {
                cosmosNoOpEmailSender = new CosmosNoOpEmailSender();
            }
            else
            {
                if (settings.Exists(f => f.Name == "AZURECOM"))
                {
                    var config = JsonConvert.DeserializeObject<AzureCommunicationEmailProviderOptions>(settings.Find(f => f.Name == "AZURECOM").Value);
                    if (config == null)
                    {
                        throw new ArgumentException("Could not retrieve options from 'Azure Communications Email' setting");
                    }
                    else
                    {
                        var options = Options.Create(config);
                        azureCommunicationEmailSender = new AzureCommunicationEmailSender(options, (ILogger<AzureCommunicationEmailSender>)logger, defaultAzureCredential);
                    }
                }
                else if (settings.Exists(f => f.Name == "SENDGRID"))
                {
                    var config = JsonConvert.DeserializeObject<SendGridEmailProviderOptions>(settings.Find(f => f.Name == "SENDGRID").Value);
                    if (config == null)
                    {
                        throw new ArgumentException("Could not retrieve options from 'SendGrid Email' setting");
                    }
                    else
                    {
                        var options = Options.Create(config);
                        sendGridEmailSender = new SendGridEmailSender(options, (ILogger<SendGridEmailSender>)logger);
                    }
                }
                else if (settings.Exists(f => f.Name == "SMTP"))
                {
                    var config = JsonConvert.DeserializeObject<SmtpEmailProviderOptions>(settings.Find(f => f.Name == "SMTP").Value);
                    if (config == null)
                    {
                        throw new ArgumentException("Could not retrieve options from 'SMTP Email' setting");
                    }
                    else
                    {
                        var options = Options.Create(config);
                        smtpEmailSender = new SmtpEmailSender(options);
                    }
                }
            }
        }

        /// <summary>
        /// Gets the Email send result.
        /// </summary>
        public SendResult SendResult
        {
            get;
            private set;
        }

        /// <summary>
        /// Sends an Email.
        /// </summary>
        /// <param name="emailTo">To email address.</param>
        /// <param name="subject">Email subject.</param>
        /// <param name="textVersion">Text version of the email.</param>
        /// <param name="htmlVersion">HTML version of the email.</param>
        /// <param name="emailFrom">From email address.</param>
        /// <returns>Task.</returns>
        public async Task SendEmailAsync(string emailTo, string subject, string textVersion, string htmlVersion, string? emailFrom = null)
        {
            if (azureCommunicationEmailSender != null)
            {
                if (!string.IsNullOrEmpty(textVersion))
                {
                    await azureCommunicationEmailSender.SendEmailAsync(emailTo, subject, textVersion, htmlVersion, emailFrom);
                }
                else
                {
                    if (string.IsNullOrEmpty(emailFrom))
                    {
                        await azureCommunicationEmailSender.SendEmailAsync(emailTo, subject, htmlVersion);
                    }
                    else
                    {
                        await azureCommunicationEmailSender.SendEmailAsync(emailTo, subject, htmlVersion, emailFrom);
                    }
                }

                SendResult = azureCommunicationEmailSender.SendResult;
            }
            else if (sendGridEmailSender != null)
            {
                if (!string.IsNullOrEmpty(textVersion))
                {
                    await sendGridEmailSender.SendEmailAsync(emailTo, subject, textVersion, htmlVersion, emailFrom);
                }
                else
                {
                    if (string.IsNullOrEmpty(emailFrom))
                    {
                        await sendGridEmailSender.SendEmailAsync(emailTo, subject, htmlVersion);
                    }
                    else
                    {
                        await sendGridEmailSender.SendEmailAsync(emailTo, subject, htmlVersion, emailFrom);
                    }
                }

                SendResult = sendGridEmailSender.SendResult;
            }
            else if (smtpEmailSender != null)
            {
                if (!string.IsNullOrEmpty(textVersion))
                {
                    await smtpEmailSender.SendEmailAsync(emailTo, subject, textVersion, htmlVersion, emailFrom);
                }
                else
                {
                    if (string.IsNullOrEmpty(emailFrom))
                    {
                        await smtpEmailSender.SendEmailAsync(emailTo, subject, htmlVersion);
                    }
                    else
                    {
                        await smtpEmailSender.SendEmailAsync(emailTo, subject, htmlVersion, emailFrom);
                    }
                }

                SendResult = smtpEmailSender.SendResult;
            }
            else if (cosmosNoOpEmailSender != null)
            {
                await cosmosNoOpEmailSender.SendEmailAsync(emailTo, subject, htmlVersion);
                SendResult = cosmosNoOpEmailSender.SendResult;
            }
        }

        /// <summary>
        /// Sends an email message.
        /// </summary>
        /// <param name="email">To email address.</param>
        /// <param name="subject">Subject of email.</param>
        /// <param name="htmlMessage">Email message in HTML format.</param>
        /// <returns>Task.</returns>
        public async Task SendEmailAsync(string email, string subject, string htmlMessage)
        {
            await SendEmailAsync(email, subject, string.Empty, htmlMessage, null);
        }
    }
}

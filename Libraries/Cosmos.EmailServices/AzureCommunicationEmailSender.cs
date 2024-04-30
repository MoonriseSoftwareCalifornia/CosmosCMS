// <copyright file="AzureCommunicationEmailSender.cs" company="Cosmos Website Solutions, Inc.">
// Copyright (c) Cosmos Website Solutions, Inc.. All rights reserved.
// Licensed under the GNU Public License, Version 3.0 (https://www.gnu.org/licenses/gpl-3.0.html)
// See https://github.com/MoonriseSoftwareCalifornia/CosmosCMS
// for more information concerning the license and the contributors participating to this project.
// </copyright>

namespace Cosmos.EmailServices
{
    using System.Diagnostics.CodeAnalysis;
    using System.Net;
    using Azure.Communication.Email;
    using Microsoft.AspNetCore.Identity.UI.Services;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Options;

    /// <summary>
    /// Email sender for Azure Communications.
    /// </summary>
    public class AzureCommunicationEmailSender : IEmailSender
    {
        private readonly IOptions<AzureCommunicationEmailProviderOptions> options;
        private readonly ILogger<AzureCommunicationEmailSender> logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="AzureCommunicationEmailSender"/> class.
        /// </summary>
        /// <param name="options">Provder options.</param>
        /// <param name="logger">ILogger.</param>
        public AzureCommunicationEmailSender([NotNull] IOptions<AzureCommunicationEmailProviderOptions> options, [NotNull] ILogger<AzureCommunicationEmailSender> logger)
        {
            this.options = options;
            this.logger = logger;
        }

        /// <summary>
        /// Gets result of the last email send.
        /// </summary>
        public SendResult? SendResult { get; private set; }

        /// <summary>
        /// Sends an email message.
        /// </summary>
        /// <param name="toEmail">To email address.</param>
        /// <param name="subject">Email subject.</param>
        /// <param name="htmlMessage">Email content in HTML form.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public async Task SendEmailAsync(string toEmail, string subject, string htmlMessage)
        {
            await SendEmailAsync(toEmail, subject, htmlMessage, null);
        }

        /// <summary>
        /// Sends an email and specifies the "from" email address.
        /// </summary>
        /// <param name="toEmail">To email address.</param>
        /// <param name="subject">Email subject.</param>
        /// <param name="htmlMessage">Email message in HTML.</param>
        /// <param name="fromEmail">From email address.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public async Task SendEmailAsync(string toEmail, string subject, string htmlMessage, string? fromEmail = null)
        {
            var emailClient = new EmailClient(options.Value.ConnectionString);

            try
            {
                if (string.IsNullOrEmpty(fromEmail))
                {
                    fromEmail = options.Value.DefaultFromEmailAddress;
                }

                var result = await emailClient.SendAsync(Azure.WaitUntil.Completed, fromEmail, toEmail, subject, htmlMessage);

                var response = result.GetRawResponse();

                SendResult = new SendResult();
                SendResult.StatusCode = (HttpStatusCode)response.Status;

                if (result.Value.Status == EmailSendStatus.Succeeded)
                {
                    SendResult.Message = $"Email successfully sent to: {toEmail}; Subject: {subject};";
                }
                else if (result.Value.Status == EmailSendStatus.Failed)
                {
                    SendResult.Message = $"Email FAILED attempting to send to: {toEmail}, with subject: {subject}, with error: {response.ReasonPhrase}";
                }
                else if (result.Value.Status == EmailSendStatus.Canceled)
                {
                    SendResult.Message = $"Email was CANCELED to: {toEmail}, with subject: {subject}, with reason: {response.ReasonPhrase}";
                }

                logger.LogInformation(SendResult.Message);
            }
            catch (Exception e)
            {
                logger.LogError(e.Message, e);
            }
        }
    }
}

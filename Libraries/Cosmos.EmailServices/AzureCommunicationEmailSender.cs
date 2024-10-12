// <copyright file="AzureCommunicationEmailSender.cs" company="Moonrise Software, LLC">
// Copyright (c) Moonrise Software, LLC. All rights reserved.
// Licensed under the GNU Public License, Version 3.0 (https://www.gnu.org/licenses/gpl-3.0.html)
// See https://github.com/MoonriseSoftwareCalifornia/CosmosCMS
// for more information concerning the license and the contributors participating to this project.
// </copyright>

namespace Cosmos.EmailServices
{
    using System.Diagnostics.CodeAnalysis;
    using System.Net;
    using Azure.Communication.Email;
    using Azure.Core;
    using Azure.Identity;
    using Microsoft.AspNetCore.Identity.UI.Services;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Options;

    /// <summary>
    /// Email sender for Azure Communications.
    /// </summary>
    public class AzureCommunicationEmailSender : ICosmosEmailSender
    {
        private readonly IOptions<AzureCommunicationEmailProviderOptions> options;
        private readonly ILogger<AzureCommunicationEmailSender> logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="AzureCommunicationEmailSender"/> class.
        /// </summary>
        /// <param name="options">Provder options.</param>
        /// <param name="logger">ILogger.</param>
        public AzureCommunicationEmailSender(IOptions<AzureCommunicationEmailProviderOptions> options, ILogger<AzureCommunicationEmailSender> logger)
        {
            this.options = options;
            this.logger = logger;
            SendResult = new SendResult();
        }

        /// <summary>
        /// Gets result of the last email send.
        /// </summary>
        public SendResult SendResult { get; private set; }

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
            await SendEmailAsync(toEmail, subject, string.Empty, htmlMessage, fromEmail);
        }

        /// <summary>
        /// Sends an email and specifies the "from" email address.
        /// </summary>
        /// <param name="emailTo">To email address.</param>
        /// <param name="subject">Email subject.</param>
        /// <param name="textVersion">Email message in text form.</param>
        /// <param name="htmlMessage">Email message in HTML.</param>
        /// <param name="fromEmail">Who the email is from.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public async Task SendEmailAsync(string emailTo, string subject, string textVersion, string htmlMessage, string? fromEmail = null)
        {
            var tempParts = options.Value.ConnectionString.Split(";").Where(w => !string.IsNullOrEmpty(w)).Select(part => part.Split('=')).ToDictionary(sp => sp[0], sp => sp[1]);
            var tempEndPoint = tempParts["endpoint"];

            EmailClient emailClient;

            if (tempParts["AccountKey"] == "AccessToken")
            {
                emailClient = new EmailClient(endpoint: new Uri(tempEndPoint), new DefaultAzureCredential());
            }
            else
            {
                emailClient = new EmailClient(options.Value.ConnectionString);
            }

            if (string.IsNullOrEmpty(fromEmail))
            {
                fromEmail = options.Value.DefaultFromEmailAddress;
            }

            EmailSendOperation result;

            var aschtml = System.Text.Encoding.ASCII.GetString(System.Text.Encoding.ASCII.GetBytes(htmlMessage));

            if (string.IsNullOrEmpty(textVersion))
            {
                result = await emailClient.SendAsync(Azure.WaitUntil.Completed, fromEmail, emailTo, subject, htmlContent: aschtml);
            }
            else
            {
                var asctext = System.Text.Encoding.ASCII.GetString(System.Text.Encoding.ASCII.GetBytes(textVersion));
                result = await emailClient.SendAsync(Azure.WaitUntil.Completed, fromEmail, emailTo, subject, aschtml, asctext);
            }

            var response = result.GetRawResponse();

            SendResult = new SendResult();
            SendResult.StatusCode = (HttpStatusCode)response.Status;

            if (result.Value.Status == EmailSendStatus.Succeeded)
            {
                SendResult.Message = $"Email successfully sent to: {emailTo}; Subject: {subject};";
            }
            else if (result.Value.Status == EmailSendStatus.Failed)
            {
                SendResult.Message = $"Email FAILED attempting to send to: {emailTo}, with subject: {subject}, with error: {response.ReasonPhrase}";
            }
            else if (result.Value.Status == EmailSendStatus.Canceled)
            {
                SendResult.Message = $"Email was CANCELED to: {emailTo}, with subject: {subject}, with reason: {response.ReasonPhrase}";
            }

            logger.LogInformation(SendResult.Message);
        }
    }
}

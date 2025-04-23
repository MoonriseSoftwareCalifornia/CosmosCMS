// <copyright file="SendGridEmailSender.cs" company="Moonrise Software, LLC">
// Copyright (c) Moonrise Software, LLC. All rights reserved.
// Licensed under the GNU Public License, Version 3.0 (https://www.gnu.org/licenses/gpl-3.0.html)
// See https://github.com/MoonriseSoftwareCalifornia/CosmosCMS
// for more information concerning the license and the contributors participating to this project.
// </copyright>

namespace Cosmos.EmailServices
{
    using System.Net;
    using HtmlAgilityPack;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Options;
    using SendGrid;
    using SendGrid.Helpers.Mail;

    /// <summary>
    ///     SendGrid Email sender service.
    /// </summary>
    public class SendGridEmailSender : ICosmosEmailSender
    {
        private readonly IOptions<SendGridEmailProviderOptions> options;
        private readonly ILogger<SendGridEmailSender> logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="SendGridEmailSender"/> class.
        /// </summary>
        /// <param name="options">SendGrid options.</param>
        /// <param name="logger">Logger.</param>
        public SendGridEmailSender(IOptions<SendGridEmailProviderOptions> options, ILogger<SendGridEmailSender> logger)
        {
            this.options = options;
            this.logger = logger;
            this.SendResult = new SendResult();
        }

        /// <summary>
        /// Gets the status code of the last email send result.
        /// </summary>
        public SendResult SendResult { get; private set; }

        /// <summary>
        /// Gets a value indicating whether indicates if client is in SendGrid <see href="https://docs.sendgrid.com/for-developers/sending-email/sandbox-mode">sandbox</see> mode.
        /// </summary>
        public bool SandboxMode
        {
            get { return this.options.Value.SandboxMode; }
        }

        /// <summary>
        /// Gets the <see cref="Response"/> object.
        /// </summary>
        public Response? Response { get; private set; }

        /// <summary>
        ///     Send email method.
        /// </summary>
        /// <param name="emailTo">To email address.</param>
        /// <param name="subject">Email subject.</param>
        /// <param name="htmlMessage">Email message in HTML format.</param>
        /// <param name="emailFrom">From email message.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public Task SendEmailAsync(string emailTo, string subject, string htmlMessage, string? emailFrom = null)
        {
            var doc = new HtmlDocument();
            doc.LoadHtml(htmlMessage);

            var message = new SendGridMessage
            {
                Subject = subject,
                PlainTextContent = doc.DocumentNode.InnerText,
                HtmlContent = htmlMessage
            };

            var task = this.Execute(message, emailTo, emailFrom);
            task.Wait();

            return task;
        }

        /// <summary>
        /// IEmailSender send email method.
        /// </summary>
        /// <param name="emailTo">To email address.</param>
        /// <param name="subject">Email subject.</param>
        /// <param name="htmlMessage">Email message in HTML format.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public Task SendEmailAsync(string emailTo, string subject, string htmlMessage)
        {
            return this.SendEmailAsync(emailTo, subject, htmlMessage, null);
        }

        /// <summary>
        /// Sends and Email using a SendGrid mail template.
        /// </summary>
        /// <param name="emailTo">To email address.</param>
        /// <param name="subject">Email subject.</param>
        /// <param name="textVersion">Text version of the email.</param>
        /// <param name="htmlVersion">HTML version of the email.</param>
        /// <param name="emailFrom">Who the email is from.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public Task SendEmailAsync(string emailTo, string subject, string textVersion, string htmlVersion, string? emailFrom = null)
        {
            var message = new SendGridMessage();

            message.SetFrom(new EmailAddress(emailFrom ?? this.options.Value.DefaultFromEmailAddress));
            message.AddTo(emailTo);
            message.PlainTextContent = textVersion;
            message.HtmlContent = htmlVersion;
            message.Subject = subject;

            var task = this.Execute(message, emailTo, emailFrom);
            task.Wait();

            return task;
        }

        /// <summary>
        ///     Execute send email method.
        /// </summary>
        /// <param name="message">SendGrid message.</param>
        /// <param name="emailTo">EMail to address.</param>
        /// <param name="emailFrom">Email from address.</param>
        private async Task Execute(SendGridMessage message, string emailTo, string? emailFrom)
        {
            message.AddTo(new EmailAddress(emailTo));
            message.SetFrom(new EmailAddress(emailFrom ?? this.options.Value.DefaultFromEmailAddress));

            // Disable click tracking.
            // See https://sendgrid.com/docs/User_Guide/Settings/tracking.html
            message.SetClickTracking(true, true);

            // Set the Sandbox mode if on.
            if (this.options.Value.SandboxMode)
            {
                message.SetSandBoxMode(true);
            }

            try
            {
                var client = new SendGridClient(this.options.Value);

                this.Response = await client.SendEmailAsync(message);

                this.SendResult.StatusCode = this.Response.StatusCode;
                this.SendResult.Message = await this.Response.Body.ReadAsStringAsync();

                if (!this.Response.IsSuccessStatusCode && this.options.Value.LogErrors)
                {
                    this.logger.LogError(new Exception($"SendGrid status code: {this.Response.StatusCode}"), this.Response.Headers.ToString());
                }
            }
            catch (Exception e)
            {
                this.SendResult.StatusCode = HttpStatusCode.BadRequest;
                this.SendResult.Message = e.Message;
                this.logger.LogError(e, e.Message);
            }
        }
    }
}

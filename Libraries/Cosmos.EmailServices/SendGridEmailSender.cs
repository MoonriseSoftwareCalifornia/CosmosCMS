// <copyright file="SendGridEmailSender.cs" company="Moonrise Software, LLC">
// Copyright (c) Moonrise Software, LLC. All rights reserved.
// Licensed under the GNU Public License, Version 3.0 (https://www.gnu.org/licenses/gpl-3.0.html)
// See https://github.com/MoonriseSoftwareCalifornia/CosmosCMS
// for more information concerning the license and the contributors participating to this project.
// </copyright>

namespace Cosmos.EmailServices
{
    using Microsoft.AspNetCore.Identity.UI.Services;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Options;
    using SendGrid;
    using SendGrid.Helpers.Mail;

    /// <summary>
    ///     SendGrid Email sender service.
    /// </summary>
    public class SendGridEmailSender : IEmailSender
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
        }

        /// <summary>
        /// Gets a value indicating whether indicates if client is in SendGrid <see href="https://docs.sendgrid.com/for-developers/sending-email/sandbox-mode">sandbox</see> mode.
        /// </summary>
        public bool SandboxMode
        {
            get { return options.Value.SandboxMode; }
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
        /// <param name="message">Email message.</param>
        /// <param name="emailFrom">From email message.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public Task SendEmailAsync(string emailTo, string subject, string message, string? emailFrom = null)
        {
            return Execute(subject, message, emailTo, emailFrom);
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
            return Execute(subject, htmlMessage, emailTo, null);
        }

        /// <summary>
        ///     Execute send email method.
        /// </summary>
        /// <param name="subject">Email subject.</param>
        /// <param name="message">Email message.</param>
        /// <param name="emailTo">To email address.</param>
        /// <param name="emailFrom">From email address.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        private Task Execute(string subject, string message, string emailTo, string? emailFrom = null)
        {
            var client = new SendGridClient(options.Value);

            var msg = new SendGridMessage
            {
                From = new EmailAddress(emailFrom ?? options.Value.DefaultFromEmailAddress),
                Subject = subject,
                PlainTextContent = message,
                HtmlContent = message
            };
            msg.AddTo(new EmailAddress(emailTo));

            // Disable click tracking.
            // See https://sendgrid.com/docs/User_Guide/Settings/tracking.html
            msg.SetClickTracking(true, true);

            // Set the Sandbox mode if on.
            if (options.Value.SandboxMode)
            {
                msg.SetSandBoxMode(true);
            }

            try
            {
                Response = client.SendEmailAsync(msg).Result;

                if (Response.IsSuccessStatusCode && options.Value.LogSuccesses)
                {
                    logger.LogInformation($"Email successfully sent to: {emailTo}; Subject: {subject};");
                }

                if (!Response.IsSuccessStatusCode && options.Value.LogErrors)
                {
                    logger.LogError(new Exception($"SendGrid status code: {Response.StatusCode}"), Response.Headers.ToString());
                }
            }
            catch (Exception e)
            {
                logger.LogError(e, e.Message);
            }

            return Task.CompletedTask;
        }
    }
}

// <copyright file="SmtpEmailSender.cs" company="Moonrise Software, LLC">
// Copyright (c) Moonrise Software, LLC. All rights reserved.
// Licensed under the GNU Public License, Version 3.0 (https://www.gnu.org/licenses/gpl-3.0.html)
// See https://github.com/MoonriseSoftwareCalifornia/CosmosCMS
// for more information concerning the license and the contributors participating to this project.
// </copyright>

namespace Cosmos.EmailServices
{
    using System.Configuration;
    using System.Net;
    using System.Net.Mail;
    using Microsoft.Extensions.Options;

    /// <summary>
    /// SMTP Email sender.
    /// </summary>
    public class SmtpEmailSender : ICosmosEmailSender
    {
        private readonly SmtpEmailProviderOptions options;

        /// <summary>
        /// Initializes a new instance of the <see cref="SmtpEmailSender"/> class.
        /// </summary>
        /// <param name="options">SMTP email provider options.</param>
        public SmtpEmailSender(IOptions<SmtpEmailProviderOptions> options)
        {
            this.options = options.Value;
            if (options.Value == null)
            {
                throw new ConfigurationErrorsException("No SmtpEmailProviderOptions configuration found.");
            }

            SendResult = new SendResult();
        }

        /// <summary>
        /// Gets the status code of the last email send result.
        /// </summary>
        public SendResult SendResult { get; private set; }

        /// <summary>
        /// IEmailSender send email method.
        /// </summary>
        /// <param name="emailTo">To email address.</param>
        /// <param name="subject">Email subject.</param>
        /// <param name="htmlMessage">Email message in HTML format.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public Task SendEmailAsync(string emailTo, string subject, string htmlMessage)
        {
            return SendEmailAsync(emailTo, subject, htmlMessage, null);
        }

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
            var message = new MailMessage();

            if (string.IsNullOrEmpty(emailFrom))
            {
                emailFrom = options.DefaultFromEmailAddress;
            }

            message.Subject = subject;
#pragma warning disable CS8604 // Possible null reference argument.
            message.From = new MailAddress(emailFrom);
#pragma warning restore CS8604 // Possible null reference argument.
            message.To.Add(new MailAddress(emailTo));
            message.Body = htmlMessage;
            message.IsBodyHtml = true;

            var task = Execute(message);
            task.Wait();
            return task;
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
            var message = new MailMessage();

            if (string.IsNullOrEmpty(emailFrom))
            {
                emailFrom = options.DefaultFromEmailAddress;
            }

            message.Subject = subject;
#pragma warning disable CS8604 // Possible null reference argument.
            message.From = new MailAddress(emailFrom);
#pragma warning restore CS8604 // Possible null reference argument.
            message.To.Add(new MailAddress(emailTo));
            message.Body = textVersion;

            if (!string.IsNullOrEmpty(htmlVersion))
            {
                message.AlternateViews.Add(AlternateView.CreateAlternateViewFromString(htmlVersion, null, "text/html"));
                message.IsBodyHtml = true;
            }
            else
            {
                message.IsBodyHtml = false;
            }

            message.Subject = subject;

            var task = Execute(message);
            task.Wait();
            return task;
        }

        private async Task Execute(MailMessage message)
        {
            var client = new SmtpClient(options.Host, options.Port);

            if (!string.IsNullOrEmpty(options.Password))
            {
                client.Credentials = new NetworkCredential(options.UserName, options.Password);
                if (options.UsesSsl)
                {
                    client.EnableSsl = true;
                }
            }

            try
            {
                await client.SendMailAsync(message);
                SendResult.StatusCode = HttpStatusCode.OK;
                SendResult.Message = "Email sent successfully.";
            }
            catch (Exception ex)
            {
                SendResult.StatusCode = HttpStatusCode.BadRequest;
                SendResult.Message = ex.Message;
            }

            return;
        }
    }
}

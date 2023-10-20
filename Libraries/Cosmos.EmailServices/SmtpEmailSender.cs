// <copyright file="SmtpEmailSender.cs" company="Moonrise Software, LLC">
// Copyright (c) Moonrise Software, LLC. All rights reserved.
// Licensed under the GNU Public License, Version 3.0 (https://www.gnu.org/licenses/gpl-3.0.html)
// See https://github.com/MoonriseSoftwareCalifornia/CosmosCMS
// for more information concerning the license and the contributors participating to this project.
// </copyright>

namespace Cosmos.EmailServices
{
    using System.Net;
    using System.Net.Mail;
    using Microsoft.AspNetCore.Identity.UI.Services;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Options;

    /// <summary>
    /// SMTP Email sender.
    /// </summary>
    public class SmtpEmailSender : IEmailSender
    {
        private readonly IOptions<SmtpEmailProviderOptions> options;
        private readonly ILogger<SendGridEmailSender> logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="SmtpEmailSender"/> class.
        /// </summary>
        /// <param name="options">SMTP email provider options.</param>
        /// <param name="logger">Logger.</param>
        public SmtpEmailSender(IOptions<SmtpEmailProviderOptions> options, ILogger<SendGridEmailSender> logger)
        {
            this.options = options;
            this.logger = logger;
        }

        /// <summary>
        ///     Send email method.
        /// </summary>
        /// <param name="toEmail">To email address.</param>
        /// <param name="subject">Email subject.</param>
        /// <param name="message">Email message.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public Task SendEmailAsync(string toEmail, string subject, string message)
        {
            return Execute(subject, message, toEmail);
        }

        private async Task Execute(string subject, string message, string toEmail)
        {
            var client = new SmtpClient(options.Value.Host, options.Value.Port);

            if (!string.IsNullOrEmpty(options.Value.Password))
            {
                client.Credentials = new NetworkCredential(options.Value.UserName, options.Value.Password);
                if (options.Value.UsesSsl)
                {
                    client.EnableSsl = true;
                }
            }

            var msg = new MailMessage(options.Value.DefaultFromEmailAddress, toEmail, subject, message);

            try
            {
                await client.SendMailAsync(msg);
            }
            catch (Exception e)
            {
                logger.LogError(e, e.Message);
            }

            return;
        }
    }
}

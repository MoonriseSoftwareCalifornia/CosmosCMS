// <copyright file="EmailHandler.cs" company="Moonrise Software, LLC">
// Copyright (c) Moonrise Software, LLC. All rights reserved.
// Licensed under the GNU Public License, Version 3.0 (https://www.gnu.org/licenses/gpl-3.0.html)
// See https://github.com/MoonriseSoftwareCalifornia/CosmosCMS
// for more information concerning the license and the contributors participating to this project.
// </copyright>

namespace Cosmos.EmailServices
{
    using System;
    using System.Threading.Tasks;
    using Cosmos.EmailServices.Templates;
    using Microsoft.Extensions.Logging;

    /// <summary>
    /// Handles certain types of emails for both the Cosmos editor and publisher.
    /// </summary>
    public class EmailHandler
    {
        private readonly ICosmosEmailSender emailSender;
        private readonly ILogger logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="EmailHandler"/> class.
        /// </summary>
        /// <param name="emailSender">Cosmos Email Sender.</param>
        /// <param name="logger">Log service.</param>
        public EmailHandler(ICosmosEmailSender emailSender, ILogger logger)
        {
            this.emailSender = emailSender;
            this.logger = logger;
        }

        /// <summary>
        /// Indicates the callback email template to use.
        /// </summary>
        public enum CallbackTemplate
        {
            /// <summary>
            /// Indicates using the reset password template (HTML and Text)
            /// </summary>
            ResetPasswordTemplate,

            /// <summary>
            /// Indicates using the new account email confirmation template (HTML and Text)
            /// </summary>
            NewAccountConfirmEmail
        }

        /// <summary>
        /// Sends a password reset email using the ResetPasswordTemplate email template.
        /// </summary>
        /// <param name="template">Callback template to use.</param>
        /// <param name="callbackUrl">URL that user opens to reset the password.</param>
        /// <param name="hostname">Host name of website.</param>
        /// <param name="toEmail">Who is receiving the email.</param>
        /// <param name="websiteName">Name of the website.</param>
        /// <param name="fromEmail">From email address (optional).</param>
        /// <returns>A <see cref="Task"/> representing the result of the asynchronous operation.</returns>
        public async Task SendCallbackTemplateEmail(CallbackTemplate template, string callbackUrl, string hostname, string toEmail, string websiteName, string? fromEmail = null)
        {
            var templateName = string.Empty;
            var subject = string.Empty;

            switch (template)
            {
                case CallbackTemplate.ResetPasswordTemplate:
                    templateName = "ResetPasswordTemplate";
                    subject = "Password reset request";
                    break;
                case CallbackTemplate.NewAccountConfirmEmail:
                    templateName = "NewAccountConfirmEmail";
                    subject = "New account, email confirmation";
                    break;
            }

            var parser = this.GetParser(templateName);

            parser.Insert("CallbackUrl", callbackUrl);
            parser.Insert("WebsiteUrl", $"https://{hostname}");
            parser.Insert("WebsiteName", websiteName);

            if (parser.Html != null && parser.Text != null)
            {
                await this.emailSender.SendEmailAsync(toEmail, subject, parser.Text, parser.Html, fromEmail);
                this.logger.LogInformation($"Password reset Email request sent to: '{toEmail}'.");
            }
            else
            {
                this.logger.LogInformation($"Failed to send password reset Email request sent to: '{toEmail}' because email parser failed to build message.");
            }
        }

        /// <summary>
        /// Sends an email using the general information template.
        /// </summary>
        /// <param name="subject">Subject.</param>
        /// <param name="subtitle">Subtitle.</param>
        /// <param name="websiteName">Website name.</param>
        /// <param name="hostname">Website host name.</param>
        /// <param name="body">Email body or content.</param>
        /// <param name="toEmail">Who will receive the email.</param>
        /// <param name="fromEmail">Who the email is from.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public async Task SendGeneralInfoTemplateEmail(string subject, string subtitle, string websiteName, string hostname, string body, string toEmail, string? fromEmail = null)
        {
            var parser = new EmailTemplateParser("GeneralInfo");
            parser.Insert("Subject", subject);
            parser.Insert("Subtitle", subtitle);
            parser.Insert("WebsiteUrl", $"https://{hostname}");
            parser.Insert("WebsiteName", websiteName);
            parser.InsertHtml("Body", body);

            await this.emailSender.SendEmailAsync(toEmail, subject, parser.Text, parser.Html, fromEmail);

            this.logger.LogInformation($"Email with subject '{subject}' was sent to: '{toEmail}'");
        }

        /// <summary>
        /// Sends an email using the general information template.
        /// </summary>
        /// <param name="subject">Subject.</param>
        /// <param name="subtitle">Subtitle.</param>
        /// <param name="websiteName">Website name.</param>
        /// <param name="hostname">Website host name.</param>
        /// <param name="body">Email body or content.</param>
        /// <param name="toEmails">A list of those who will receive the email.</param>
        /// <param name="fromEmail">Who the email is from.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public async Task SendGeneralInfoTemplateEmail(string subject, string subtitle, string websiteName, string hostname, string body, IEnumerable<string> toEmails, string? fromEmail = null)
        {
            var parser = new EmailTemplateParser("GeneralInfo");
            parser.Insert("Subject", subject);
            parser.Insert("Subtitle", subtitle);
            parser.Insert("WebsiteUrl", $"https://{hostname}");
            parser.Insert("WebsiteName", websiteName);
            parser.InsertHtml("Body", body);

            foreach (var email in toEmails)
            {
                await this.emailSender.SendEmailAsync(email, subject, parser.Text, parser.Html, fromEmail);

                this.logger.LogInformation($"Email with subject '{subject}' was sent to: '{email}'");
            }
        }

        private EmailTemplateParser GetParser(string templateName)
        {
            var parser = new EmailTemplateParser(templateName);

            if (string.IsNullOrEmpty(parser.Text) || string.IsNullOrEmpty(parser.Html))
            {
                throw new Exception($"Could not load '{templateName}' email template");
            }

            return parser;
        }
    }
}

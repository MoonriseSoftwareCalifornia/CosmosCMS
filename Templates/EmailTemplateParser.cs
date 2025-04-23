// <copyright file="EmailTemplateParser.cs" company="Moonrise Software, LLC">
// Copyright (c) Moonrise Software, LLC. All rights reserved.
// Licensed under the GNU Public License, Version 3.0 (https://www.gnu.org/licenses/gpl-3.0.html)
// See https://github.com/MoonriseSoftwareCalifornia/CosmosCMS
// for more information concerning the license and the contributors participating to this project.
// </copyright>

namespace Cosmos.EmailServices.Templates
{
    using System.Resources;
    using HtmlAgilityPack;

    /// <summary>
    /// Email template utilities.
    /// </summary>
    public class EmailTemplateParser
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="EmailTemplateParser"/> class.
        /// </summary>
        /// <param name="templateName">Template name.</param>
        public EmailTemplateParser(string templateName)
        {
            var rm = new ResourceManager("Cosmos.EmailServices.Templates.EmailTemplates", typeof(EmailTemplates).Assembly);

            if (rm != null)
            {
                this.Html = rm.GetObject(templateName) as string ?? string.Empty;
                this.Text = rm.GetObject($"{templateName}TXT") as string ?? string.Empty;
            }

            if (string.IsNullOrEmpty(this.Html))
            {
                throw new ArgumentException($"Html template '{templateName}' not found.");
            }

            if (string.IsNullOrEmpty(this.Text))
            {
                throw new ArgumentException($"Text template '{templateName}TXT' not found.");
            }
        }

        /// <summary>
        /// Gets the HTML version of the Email.
        /// </summary>
        public string Html { get; private set; } = string.Empty;

        /// <summary>
        /// Gets the text version of the Email.
        /// </summary>
        public string Text { get; private set; } = string.Empty;

        /// <summary>
        /// Inserts text into the Email.
        /// </summary>
        /// <param name="key">The key being search for.</param>
        /// <param name="value">The text value being inserted.</param>
        /// <example>
        /// <code>
        /// Insert("WebsiteUrl", "https://mysite.com")
        /// </code>
        /// </example>
        public void Insert(string key, string value)
        {
            this.Html = this.Html.Replace("{{" + key + "}}", value);
            this.Text = this.Text.Replace("{{" + key + "}}", value);
        }

        /// <summary>
        /// Inserts HTML into the templates, and converts the HTML to text for the text template.
        /// </summary>
        /// <param name="key">The key being search for.</param>
        /// <param name="value">The text value being inserted.</param>
        public void InsertHtml(string key, string value)
        {
            this.Html = this.Html.Replace("{{" + key + "}}", value);

            var doc = new HtmlDocument();
            doc.LoadHtml(this.Html);

            this.Text = this.Text.Replace("{{" + key + "}}", doc.DocumentNode.InnerText);
        }
    }
}

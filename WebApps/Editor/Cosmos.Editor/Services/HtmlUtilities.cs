// <copyright file="HtmlUtilities.cs" company="Moonrise Software, LLC">
// Copyright (c) Moonrise Software, LLC. All rights reserved.
// Licensed under the GNU Public License, Version 3.0 (https://www.gnu.org/licenses/gpl-3.0.html)
// See https://github.com/MoonriseSoftwareCalifornia/CosmosCMS
// for more information concerning the license and the contributors participating to this project.
// </copyright>

using System;
using HtmlAgilityPack;

namespace Cosmos.Cms.Services
{
    /// <summary>
    /// Html Utilities.
    /// </summary>
    public class HtmlUtilities
    {
        /// <summary>
        /// Is an absolute Uri.
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        public bool IsAbsoluteUri(string url)
        {
            if (string.IsNullOrEmpty(url) || (url == "/"))
            {
                return false;
            }

            try
            {
                var t = new Uri(url);
                return t.IsAbsoluteUri;
            }
            catch (Exception e)
            {
                var t = e; // Debugging purposes
                return false;
            }
        }

        /// <summary>
        /// Changes relative Uri's to absolute.
        /// </summary>
        /// <param name="html"></param>
        /// <param name="absoluteUrl"></param>
        /// <param name="isLayoutBodyElement"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        public string RelativeToAbsoluteUrls(string html, Uri absoluteUrl, bool isLayoutBodyElement)
        {
            if (string.IsNullOrEmpty(html))
            {
                return string.Empty;
            }

            if (absoluteUrl == null)
            {
                return html;
            }

            if (!absoluteUrl.IsAbsoluteUri)
            {
                throw new ArgumentException($"{absoluteUrl.ToString()} is not an absolute Uri.");
            }

            var htmlDoc = new HtmlAgilityPack.HtmlDocument();
            htmlDoc.LoadHtml(html);

            foreach (HtmlNode node in htmlDoc.DocumentNode.ChildNodes)
            {
                if (isLayoutBodyElement && node.NodeType != HtmlNodeType.Comment && node.NodeType != HtmlNodeType.Text && !node.Attributes.Contains("ccms-layout"))
                {
                    node.Attributes.Add("ccms-layout", "true");
                }

                RelativeToAbsoluteNoUrls(node, absoluteUrl);
            }

            return htmlDoc.DocumentNode.OuterHtml;
        }

        private void RelativeToAbsoluteNoUrls(HtmlNode node, Uri absoluteUrl)
        {
            if (node.Attributes.Contains("href"))
            {
                HtmlAttribute att = node.Attributes["href"];

                if (att != null && !IsAbsoluteUri(att.Value))
                {
                    att.Value = new Uri(absoluteUrl, att.Value).ToString();
                }
            }

            if (node.Attributes.Contains("src"))
            {
                HtmlAttribute att = node.Attributes["src"];

                if (att != null && !IsAbsoluteUri(att.Value))
                {
                    att.Value = new Uri(absoluteUrl, att.Value).ToString();
                }
            }

            foreach (var child in node.ChildNodes)
            {
                RelativeToAbsoluteNoUrls(child, absoluteUrl);
            }
        }
    }
}

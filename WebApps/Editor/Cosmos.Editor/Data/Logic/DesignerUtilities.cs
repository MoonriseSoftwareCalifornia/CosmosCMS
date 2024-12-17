// <copyright file="DesignerUtilities.cs" company="Moonrise Software, LLC">
// Copyright (c) Moonrise Software, LLC. All rights reserved.
// Licensed under the GNU Public License, Version 3.0 (https://www.gnu.org/licenses/gpl-3.0.html)
// See https://github.com/MoonriseSoftwareCalifornia/CosmosCMS
// for more information concerning the license and the contributors participating to this project.
// </copyright>

namespace Cosmos.Editor.Data.Logic
{
    using System.Collections.Generic;
    using System.Linq;
    using Cosmos.Editor.Models;
    using HtmlAgilityPack;

    /// <summary>
    /// Contains utility methods for the designer (GraphsJS) editor.
    /// </summary>
    public class DesignerUtilities
    {
        private readonly HtmlDocument htmlEditor = new HtmlDocument();

        /// <summary>
        /// Gets the list of standard tags.
        /// </summary>
        /// <remarks>Currently includes: "div", "header", "footer", "section", "nav".</remarks>
        public static string[] StandardTagsList => new string[]
                {
                    "div", "header", "footer", "section", "nav",
                };
                
        /// <summary>
        /// Marks the StandardTagsList as not editable and movable in the editor.
        /// </summary>
        /// <param name="html">HTML code.</param>
        /// <returns>Updated HTML.</returns>
        public string MarkToBlockEditingAndMoving(string html)
        {
            return MarkToBlockMoving(MarkToBlockEditing(html, StandardTagsList), StandardTagsList);
        }

        /// <summary>
        /// Marks the specified tags as not editable and movable in the editor.
        /// </summary>
        /// <param name="html">HTML code.</param>
        /// <param name="tagNames">Tag names to mark.</param>
        /// <returns>Updated HTML.</returns>
        public string MarkToBlockEditingAndMoving(string html, string[] tagNames)
        {
            return MarkToBlockMoving(MarkToBlockEditing(html, tagNames), tagNames);
        }

        /// <summary>
        /// Marks the StandardTagsList as not editable in the editor.
        /// </summary>
        /// <param name="html">HTML code.</param>
        /// <returns>Updated HTML.</returns>
        public string MarkToBlockEditing(string html)
        {
            return MarkToBlockEditing(html, StandardTagsList);
        }

        /// <summary>
        /// Marks the specified tags as not editable in the editor.
        /// </summary>
        /// <param name="html">HTML code.</param>
        /// <param name="tagNames">Tag names to mark.</param>
        /// <returns>Updated HTML.</returns>
        public string MarkToBlockEditing(string html, string[] tagNames)
        {
            htmlEditor.LoadHtml(html);
            var nodes = htmlEditor.DocumentNode.SelectNodes("//*").Where(n => tagNames.Contains(n.Name.ToLower()));

            foreach (var element in nodes)
            {
                if (element.Attributes.Contains("data-gjs-editable"))
                {
                    element.Attributes["data-gjs-editable"].Value = "false";
                }
                else
                {
                    element.Attributes.Add("data-gjs-editable", "false");
                }
            }

            return htmlEditor.DocumentNode.OuterHtml;
        }

        /// <summary>
        /// Marks the StandardTagsList as not movable in the editor.
        /// </summary>
        /// <param name="html">HTML code.</param>
        /// <returns>Updated HTML.</returns>
        public string MarkToBlockMoving(string html)
        {
            return MarkToBlockMoving(html, StandardTagsList);
        }

        /// <summary>
        /// Marks the specified tags as not movable in the editor.
        /// </summary>
        /// <param name="html">HTML code.</param>
        /// <param name="tagNames">Tag names to mark.</param>
        /// <returns>Updated HTML.</returns>
        public string MarkToBlockMoving(string html, string[] tagNames)
        {
            htmlEditor.LoadHtml(html);
            var nodes = htmlEditor.DocumentNode.SelectNodes("//*").Where(n => tagNames.Contains(n.Name.ToLower()));

            foreach (var element in nodes)
            {
                if (element.Attributes.Contains("data-gjs-draggable"))
                {
                    element.Attributes["data-gjs-draggable"].Value = "false";
                }
                else
                {
                    element.Attributes.Add("data-gjs-draggable", "false");
                }
            }

            return htmlEditor.DocumentNode.OuterHtml;
        }

        /// <summary>
        /// Extracts the inner HTML content from the designer output HTML.
        /// </summary>
        /// <param name="html">Designer HTML content.</param>
        /// <returns>HTML string.</returns>
        public string ExtractHtmlContent(string html)
        {
            htmlEditor.LoadHtml(html);
            var body = htmlEditor.DocumentNode.SelectSingleNode("//body");
            return body?.InnerHtml;
        }

        /// <summary>
        /// Extracts the designer data from the HTML code, separating the CSS content from the HTML content.
        /// </summary>
        /// <param name="html">Input HTML.</param>
        /// <returns>DesignerDataViewModel.</returns>
        public DesignerDataViewModel ExtractDesignerData(string html)
        {
            var model = new DesignerDataViewModel();

            htmlEditor.LoadHtml(html);
            var styleNode = htmlEditor.DocumentNode.SelectSingleNode("//style[@ccms-designer-style]");
            if (styleNode != null)
            {
                model.CssContent = styleNode.InnerHtml;
                styleNode.Remove();
            }
            
            model.HtmlContent = htmlEditor.DocumentNode.OuterHtml;

            return model;
        }

        /// <summary>
        /// Assembles the designer output HTML from the designer data view model, putting in document styles at front.
        /// </summary>
        /// <param name="model">DesignerDataViewModel.</param>
        /// <returns>HTML.</returns>
        public string AssembleDesignerOutput(DesignerDataViewModel model)
        {
            htmlEditor.LoadHtml(ExtractHtmlContent(model.HtmlContent));
            var styleNode = htmlEditor.DocumentNode.SelectSingleNode("//style[@ccms-designer-style]");

            styleNode?.Remove(); // Clean things up.

            // Add the CSS content to the document if it exists.
            if (!string.IsNullOrEmpty(model.CssContent))
            {
                var newStyleNode = htmlEditor.CreateElement("style");
                newStyleNode.Attributes.Add("ccms-designer-style", "true");
                newStyleNode.InnerHtml = model.CssContent;
                htmlEditor.DocumentNode.AppendChild(newStyleNode);
            }

            return htmlEditor.DocumentNode.OuterHtml;
        }

        /// <summary>
        /// Extracts the list of style references from the HTML code.
        /// </summary>
        /// <param name="html">Input HTML.</param>
        /// <returns>List of stylesheet URLs.</returns>
        public List<string> ExtractStyleReferences(string html)
        {
            htmlEditor.LoadHtml(html);
            var nodes = htmlEditor.DocumentNode.SelectNodes("//link[@rel='stylesheet' and @href]");

            if (nodes == null)
            {
                return new List<string>();
            }

            return nodes.Select(n => n.Attributes["href"].Value).ToList();
        }

        /// <summary>
        /// Extracts the list of script references from the HTML code.
        /// </summary>
        /// <param name="html">HTML code.</param>
        /// <returns>List of script references.</returns>
        public List<string> ExtractScriptReferences(string html)
        {
            htmlEditor.LoadHtml(html);
            var nodes = htmlEditor.DocumentNode.SelectNodes("//script[@src]");
            if (nodes == null)
            {
                return new List<string>();
            }

            return nodes.Select(n => n.Attributes["src"].Value).ToList();
        }
    }
}

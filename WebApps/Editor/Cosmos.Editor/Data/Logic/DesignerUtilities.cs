using HtmlAgilityPack;
using System.Linq;
using System.Xml.Linq;

namespace Cosmos.Editor.Data.Logic
{
    /// <summary>
    /// Contains utility methods for the designer (GraphsJS) editor.
    /// </summary>
    public class DesignerUtilities
    {
        private readonly HtmlDocument htmlEditor = new HtmlDocument();

        /// <summary>
        /// Gets the list of standard tags.
        /// </summary>
        /// <remarks>Currently includes: "div", "header", "footer", "section", "nav"</remarks>
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
    }
}

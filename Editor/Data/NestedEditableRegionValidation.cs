// <copyright file="NestedEditableRegionValidation.cs" company="Moonrise Software, LLC">
// Copyright (c) Moonrise Software, LLC. All rights reserved.
// Licensed under the GNU Public License, Version 3.0 (https://www.gnu.org/licenses/gpl-3.0.html)
// See https://github.com/MoonriseSoftwareCalifornia/CosmosCMS
// for more information concerning the license and the contributors participating to this project.
// </copyright>

namespace Sky.Editor.Data
{
    using System.Linq;

    /// <summary>
    /// Checks for nested Editable Regions.
    /// </summary>
    public static class NestedEditableRegionValidation
    {
        /// <summary>
        /// Validates if content has a nested editable region, which will mess up CKEditor.
        /// </summary>
        /// <param name="html">HTML content.</param>
        /// <returns>Validation success.</returns>
        public static bool Validate(string html)
        {
            if (string.IsNullOrEmpty(html))
            {
                return true;
            }

            // Check for nested editable regions as this will mess up CKEditor.
            var htmlDoc = new HtmlAgilityPack.HtmlDocument();
            htmlDoc.LoadHtml(html);

            var elements = htmlDoc.DocumentNode.SelectNodes("//*[@contenteditable]|//*[@crx]|//*[@data-ccms-ceid]");

            if (elements != null && elements.Any())
            {
                foreach (var element in elements)
                {
                    var children = element.Descendants().Where(w => w.Attributes.Contains("data-ccms-ceid") || w.Attributes.Contains("contenteditable")).ToList();
                    if (children.Any())
                    {
                        return false;
                    }
                }
            }

            return true;
        }
    }
}

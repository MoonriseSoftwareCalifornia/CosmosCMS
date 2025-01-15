// <copyright file="page.cs" company="Moonrise Software, LLC">
// Copyright (c) Moonrise Software, LLC. All rights reserved.
// Licensed under the GNU Public License, Version 3.0 (https://www.gnu.org/licenses/gpl-3.0.html)
// See https://github.com/MoonriseSoftwareCalifornia/CosmosCMS
// for more information concerning the license and the contributors participating to this project.
// </copyright>

using System.Collections.Generic;

namespace Cosmos.Editor.Models.GrapesJs
{
    /// <summary>
    /// Represents a project in GrapesJs.
    /// </summary>
#pragma warning disable CS8981 // The type name only contains lower-cased ascii characters. Such names may become reserved for the language.
    public class project
#pragma warning restore CS8981 // The type name only contains lower-cased ascii characters. Such names may become reserved for the language.
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="project"/> class.
        /// </summary>
        public project()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="project"/> class with HTML content.
        /// </summary>
        /// <param name="html">HTML content.</param>
        public project(string html)
        {
            pages.Add(new page(html));
        }

        /// <summary>
        /// Gets or sets the project pages.
        /// </summary>
        public List<page> pages { get; set; } = new List<page>();
    }

    /// <summary>
    /// Represents a page in GrapesJs.
    /// </summary>
#pragma warning disable CS8981 // The type name only contains lower-cased ascii characters. Such names may become reserved for the language.
    public class page
#pragma warning restore CS8981 // The type name only contains lower-cased ascii characters. Such names may become reserved for the language.
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="page"/> class.
        /// </summary>
        public page()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="page"/> class with content.
        /// </summary>
        /// <param name="html">HTML content.</param>
        public page(string html)
        {
            component = html;
        }

        /// <summary>
        /// Gets or sets the page component (HTML content).
        /// </summary>
        public string component { get; set; } = string.Empty;
    }
}
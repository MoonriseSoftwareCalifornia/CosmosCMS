using System.Collections.Generic;

namespace Cosmos.Editor.Models.GrapesJs
{
    /// <summary>
    /// Represents a project in GrapesJs.
    /// </summary>
    public class project
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
    public class page
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
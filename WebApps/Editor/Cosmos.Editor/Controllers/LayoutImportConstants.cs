// <copyright file="FileManagerController.cs" company="Moonrise Software, LLC">
// Copyright (c) Moonrise Software, LLC. All rights reserved.
// Licensed under the GNU Public License, Version 3.0 (https://www.gnu.org/licenses/gpl-3.0.html)
// See https://github.com/MoonriseSoftwareCalifornia/CosmosCMS
// for more information concerning the license and the contributors participating to this project.
// </copyright>

namespace Cosmos.Cms.Controllers
{
    /// <summary>
    /// Layout import marker constants.
    /// </summary>
    public static class LayoutImportConstants
    {
        /// <summary>
        /// Marks the start of the head injection.
        /// </summary>
        public const string COSMOS_HEAD_START = "<!--  BEGIN: Cosmos Layout HEAD content. -->";

        /// <summary>
        /// Marks the end of the head injection.
        /// </summary>
        public const string COSMOS_HEAD_END = "<!--  END: Cosmos Layout HEAD content. -->";

        /// <summary>
        /// Marks the beginning of the header injection.
        /// </summary>
        public const string COSMOS_BODY_HEADER_START = "<!-- BEGIN: Cosmos Layout BODY HEADER content -->";

        /// <summary>
        /// Marks the end of the header injection.
        /// </summary>
        public const string COSMOS_BODY_HEADER_END = "<!-- END: Cosmos Layout BODY HEADER content -->";

        /// <summary>
        /// Marks the start of the footer injection.
        /// </summary>
        public const string COSMOS_BODY_FOOTER_START = "<!-- BEGIN: Cosmos Layout BODY FOOTER content -->";

        /// <summary>
        /// Marks the end of the footer injection.
        /// </summary>
        public const string COSMOS_BODY_FOOTER_END = "<!-- END: Cosmos Layout BODY FOOTER content -->";
    }
}

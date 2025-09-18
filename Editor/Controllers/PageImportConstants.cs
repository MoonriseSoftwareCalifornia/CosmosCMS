// <copyright file="FileManagerController.cs" company="Moonrise Software, LLC">
// Copyright (c) Moonrise Software, LLC. All rights reserved.
// Licensed under the GNU Public License, Version 3.0 (https://www.gnu.org/licenses/gpl-3.0.html)
// See https://github.com/MoonriseSoftwareCalifornia/CosmosCMS
// for more information concerning the license and the contributors participating to this project.
// </copyright>

namespace Sky.Cms.Controllers
{
    /// <summary>
    /// Page import constants.
    /// </summary>
    public static class PageImportConstants
    {
        /// <summary>
        /// Marks the start of the head injection.
        /// </summary>
        public const string COSMOS_HEAD_START = "<!--  BEGIN: Cosmos Layout HEAD content inject (not editable). -->";

        /// <summary>
        /// Marks the end of the head injection.
        /// </summary>
        public const string COSMOS_HEAD_END = "<!--  END: Cosmos HEAD inject (not editable). -->";

        /// <summary>
        /// Marks the beginning of the optional head script injection.
        /// </summary>
        public const string COSMOS_HEAD_SCRIPTS_START = "<!-- BEGIN: Optional Cosmos script section injected (not editable). -->";

        /// <summary>
        /// Marks the end of the optional head script injection.
        /// </summary>
        public const string COSMOS_HEAD_SCRIPTS_END = "<!-- END: Optional Cosmos script section injected  (not editable). -->";

        /// <summary>
        /// Marks the beginning of the header injection.
        /// </summary>
        public const string COSMOS_BODY_HEADER_START = "<!-- BEGIN: Cosmos Layout BODY HEADER content (not editable) -->";

        /// <summary>
        /// Marks the end of the header injection.
        /// </summary>
        public const string COSMOS_BODY_HEADER_END = "<!-- END: Cosmos Layout BODY HEADER content (not editable) -->";

        /// <summary>
        /// Marks the start of the footer injection.
        /// </summary>
        public const string COSMOS_BODY_FOOTER_START = "<!-- BEGIN: Cosmos Layout BODY FOOTER (not editable) -->";

        /// <summary>
        /// Marks the end of the footer injection.
        /// </summary>
        public const string COSMOS_BODY_FOOTER_END = "<!-- END: Cosmos Layout BODY FOOTER (not editable) -->";

        /// <summary>
        /// Marks the start of Google Translate injection.
        /// </summary>
        public const string COSMOS_GOOGLE_TRANSLATE_START = "<!-- BEGIN: Google Translate v3 (not editable) -->";

        /// <summary>
        /// Marks the endo of Google Translate injection.
        /// </summary>
        public const string COSMOS_GOOGLE_TRANSLATE_END = "<!-- END: Google Translate v3 (not editable) -->";

        /// <summary>
        /// Marks the start of the end-of-body script injection.
        /// </summary>
        public const string COSMOS_BODY_END_SCRIPTS_START = "<!-- BEGIN: Optional Cosmos script section injected (not editable). -->";

        /// <summary>
        /// Marks the end of the end-of-body script injection.
        /// </summary>
        public const string COSMOS_BODY_END_SCRIPTS_END = "<!-- END: Optional Cosmos script section (not editable). -->";
    }
}

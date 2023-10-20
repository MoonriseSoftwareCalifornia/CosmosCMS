// <copyright file="ManageNavPages.cs" company="Moonrise Software, LLC">
// Copyright (c) Moonrise Software, LLC. All rights reserved.
// Licensed under the GNU Public License, Version 3.0 (https://www.gnu.org/licenses/gpl-3.0.html)
// See https://github.com/MoonriseSoftwareCalifornia/CosmosCMS
// for more information concerning the license and the contributors participating to this project.
// </copyright>

namespace Cosmos.Cms.Areas.Identity.Pages.Account.Manage
{
    using System;
    using System.IO;
    using Microsoft.AspNetCore.Mvc.Rendering;

    /// <summary>
    /// Manage navigation pages class.
    /// </summary>
    public static class ManageNavPages
    {
        /// <summary>
        /// Gets index.
        /// </summary>
        public static string Index => "Index";

        /// <summary>
        /// Gets email.
        /// </summary>
        public static string Email => "Email";

        /// <summary>
        /// Gets change password.
        /// </summary>
        public static string ChangePassword => "ChangePassword";

        /// <summary>
        /// Gets download personal data.
        /// </summary>
        public static string DownloadPersonalData => "DownloadPersonalData";

        /// <summary>
        /// Gets delete personal data.
        /// </summary>
        public static string DeletePersonalData => "DeletePersonalData";

        /// <summary>
        /// Gets external logins.
        /// </summary>
        public static string ExternalLogins => "ExternalLogins";

        /// <summary>
        /// Gets personal data.
        /// </summary>
        public static string PersonalData => "PersonalData";

        /// <summary>
        /// Gets two factor authentication.
        /// </summary>
        public static string TwoFactorAuthentication => "TwoFactorAuthentication";

        /// <summary>
        /// Index.
        /// </summary>
        /// <param name="viewContext">View content.</param>
        /// <returns>Returns a <see cref="string"/>.</returns>
        public static string IndexNavClass(ViewContext viewContext)
        {
            return PageNavClass(viewContext, Index);
        }

        /// <summary>
        /// Email navigation.
        /// </summary>
        /// <param name="viewContext">View content.</param>
        /// <returns>Returns a <see cref="string"/>.</returns>
        public static string EmailNavClass(ViewContext viewContext)
        {
            return PageNavClass(viewContext, Email);
        }

        /// <summary>
        /// Change password nav class.
        /// </summary>
        /// <param name="viewContext">View content.</param>
        /// <returns>Returns a <see cref="string"/>.</returns>
        public static string ChangePasswordNavClass(ViewContext viewContext)
        {
            return PageNavClass(viewContext, ChangePassword);
        }

        /// <summary>
        /// Download personal data nav class.
        /// </summary>
        /// <param name="viewContext">View content.</param>
        /// <returns>Returns a <see cref="string"/>.</returns>
        public static string DownloadPersonalDataNavClass(ViewContext viewContext)
        {
            return PageNavClass(viewContext, DownloadPersonalData);
        }

        /// <summary>
        /// Delete personal data nav class.
        /// </summary>
        /// <param name="viewContext">View content.</param>
        /// <returns>Returns a <see cref="string"/>.</returns>
        public static string DeletePersonalDataNavClass(ViewContext viewContext)
        {
            return PageNavClass(viewContext, DeletePersonalData);
        }

        /// <summary>
        /// External logins nav class.
        /// </summary>
        /// <param name="viewContext">View content.</param>
        /// <returns>Returns a <see cref="string"/>.</returns>
        public static string ExternalLoginsNavClass(ViewContext viewContext)
        {
            return PageNavClass(viewContext, ExternalLogins);
        }

        /// <summary>
        /// Personal data nav class.
        /// </summary>
        /// <param name="viewContext">View content.</param>
        /// <returns>Returns a <see cref="string"/>.</returns>
        public static string PersonalDataNavClass(ViewContext viewContext)
        {
            return PageNavClass(viewContext, PersonalData);
        }

        /// <summary>
        /// Two factor authentication nav class.
        /// </summary>
        /// <param name="viewContext">View content.</param>
        /// <returns>Returns a <see cref="string"/>.</returns>
        public static string TwoFactorAuthenticationNavClass(ViewContext viewContext)
        {
            return PageNavClass(viewContext, TwoFactorAuthentication);
        }

        /// <summary>
        /// Page nav class.
        /// </summary>
        /// <param name="viewContext">View content.</param>
        /// <param name="page">Page name.</param>
        /// <returns>Returns a <see cref="string"/>.</returns>
        private static string PageNavClass(ViewContext viewContext, string page)
        {
            var activePage = viewContext.ViewData["ActivePage"] as string
                             ?? Path.GetFileNameWithoutExtension(viewContext.ActionDescriptor.DisplayName);
            return string.Equals(activePage, page, StringComparison.OrdinalIgnoreCase) ? "active" : null;
        }
    }
}
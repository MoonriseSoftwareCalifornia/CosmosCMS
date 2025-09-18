// <copyright file="ResendEmailConfirmResult.cs" company="Moonrise Software, LLC">
// Copyright (c) Moonrise Software, LLC. All rights reserved.
// Licensed under the GNU Public License, Version 3.0 (https://www.gnu.org/licenses/gpl-3.0.html)
// See https://github.com/MoonriseSoftwareCalifornia/CosmosCMS
// for more information concerning the license and the contributors participating to this project.
// </copyright>

namespace Sky.Cms.Models
{
    /// <summary>
    /// Resend email confirmation result.
    /// </summary>
    public class ResendEmailConfirmResult
    {
        /// <summary>
        /// Gets or sets a value indicating whether email sent successfully.
        /// </summary>
        public bool Success { get; set; } = false;

        /// <summary>
        /// Gets or sets error message (if any).
        /// </summary>
        public string Error { get; set; }
    }
}

// <copyright file="SendGridTemplateData.cs" company="Moonrise Software, LLC">
// Copyright (c) Moonrise Software, LLC. All rights reserved.
// Licensed under the GNU Public License, Version 3.0 (https://www.gnu.org/licenses/gpl-3.0.html)
// See https://github.com/MoonriseSoftwareCalifornia/CosmosCMS
// for more information concerning the license and the contributors participating to this project.
// </copyright>

namespace Cosmos.EmailServices
{
    /// <summary>
    /// SendGrid Template Data.
    /// </summary>
    internal class SendGridTemplateData
    {
        /// <summary>
        /// Gets or sets the email body.
        /// </summary>
        public string EmailBody { get; set; } = string.Empty;
    }
}

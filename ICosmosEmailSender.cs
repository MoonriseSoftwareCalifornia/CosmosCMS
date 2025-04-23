// <copyright file="ICosmosEmailSender.cs" company="Moonrise Software, LLC">
// Copyright (c) Moonrise Software, LLC. All rights reserved.
// Licensed under the GNU Public License, Version 3.0 (https://www.gnu.org/licenses/gpl-3.0.html)
// See https://github.com/MoonriseSoftwareCalifornia/CosmosCMS
// for more information concerning the license and the contributors participating to this project.
// </copyright>

namespace Cosmos.EmailServices
{
    using Microsoft.AspNetCore.Identity.UI.Services;

    /// <summary>
    /// Cosos Email Sender Interface.
    /// </summary>
    /// <remarks>Includes the result property.</remarks>
    public interface ICosmosEmailSender : IEmailSender
    {
        /// <summary>
        /// Gets the status code of the last email send result.
        /// </summary>
        SendResult SendResult { get; }

        /// <summary>
        /// Sends a password reset email.
        /// </summary>
        /// <param name="emailTo">To address.</param>
        /// <param name="subject">Email subject.</param>
        /// <param name="textVersion">Message in plain text.</param>
        /// <param name="htmlVersion">HTML message version.</param>
        /// <param name="emailFrom">From email address.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        Task SendEmailAsync(string emailTo, string subject, string textVersion, string htmlVersion, string? emailFrom = null);
    }
}

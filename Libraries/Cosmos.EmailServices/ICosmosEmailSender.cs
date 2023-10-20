// <copyright file="ICosmosEmailSender.cs" company="Moonrise Software, LLC">
// Copyright (c) Moonrise Software, LLC. All rights reserved.
// Licensed under the GNU Public License, Version 3.0 (https://www.gnu.org/licenses/gpl-3.0.html)
// See https://github.com/MoonriseSoftwareCalifornia/CosmosCMS
// for more information concerning the license and the contributors participating to this project.
// </copyright>

namespace Cosmos.EmailServices
{
    using Azure.Communication.Email;
    using Microsoft.AspNetCore.Identity.UI.Services;

    /// <summary>
    /// Cosos Email Sender Interface.
    /// </summary>
    /// <remarks>Includes the result property.</remarks>
    public interface ICosmosEmailSender : IEmailSender
    {
        /// <summary>
        /// Gets or sets email send status.
        /// </summary>
        EmailSendStatus SendStatus { get; set; }
    }
}

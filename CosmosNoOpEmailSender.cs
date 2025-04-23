// <copyright file="CosmosNoOpEmailSender.cs" company="Moonrise Software, LLC">
// Copyright (c) Moonrise Software, LLC. All rights reserved.
// Licensed under the GNU Public License, Version 3.0 (https://www.gnu.org/licenses/gpl-3.0.html)
// See https://github.com/MoonriseSoftwareCalifornia/CosmosCMS
// for more information concerning the license and the contributors participating to this project.
// </copyright>

namespace Cosmos.EmailServices
{
    /// <summary>
    /// NoOp Email Sender.
    /// </summary>
    internal class CosmosNoOpEmailSender : ICosmosEmailSender
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CosmosNoOpEmailSender"/> class.
        /// </summary>
        public CosmosNoOpEmailSender()
        {
        }

        /// <inheritdoc/>
        public SendResult SendResult => new () { Message = "NoOpEmailSender", StatusCode = System.Net.HttpStatusCode.OK };

        /// <inheritdoc/>
        public Task SendEmailAsync(string emailTo, string subject, string textVersion, string htmlVersion, string? emailFrom = null)
        {
            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public Task SendEmailAsync(string email, string subject, string htmlMessage)
        {
            return Task.CompletedTask;
        }
    }
}

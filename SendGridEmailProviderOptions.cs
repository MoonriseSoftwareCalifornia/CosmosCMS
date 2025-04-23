// <copyright file="SendGridEmailProviderOptions.cs" company="Moonrise Software, LLC">
// Copyright (c) Moonrise Software, LLC. All rights reserved.
// Licensed under the GNU Public License, Version 3.0 (https://www.gnu.org/licenses/gpl-3.0.html)
// See https://github.com/MoonriseSoftwareCalifornia/CosmosCMS
// for more information concerning the license and the contributors participating to this project.
// </copyright>

namespace Cosmos.EmailServices
{
    using SendGrid;

    /// <inheritdoc/>
    /// <remarks>This object adds properties to indicate default sender email address and if is in debug mode.</remarks>
    public class SendGridEmailProviderOptions : SendGridClientOptions
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SendGridEmailProviderOptions"/> class.
        /// </summary>
        public SendGridEmailProviderOptions()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SendGridEmailProviderOptions"/> class.
        /// </summary>
        /// <param name="apiKey">SendGrid API Key.</param>
        /// <param name="defaultFromEmailAddress">Default from email address.</param>
        /// <param name="sandboxMode">SendGrid sandbox mode (does not actually send email).</param>
        /// <param name="logSuccesses">Log when SendGrid indicates success.</param>
        /// <param name="logErrors">Log when SendGrid indicates error.</param>
        /// <remarks>Logging is sent to the ILogger interface.</remarks>
        public SendGridEmailProviderOptions(string apiKey, string? defaultFromEmailAddress, bool sandboxMode = false, bool logSuccesses = false, bool logErrors = true)
        {
            this.ApiKey = apiKey;
            this.DefaultFromEmailAddress = defaultFromEmailAddress;
            this.SandboxMode = sandboxMode;
            this.LogSuccesses = logSuccesses;
            this.LogErrors = logErrors;
        }

        /// <summary>
        /// Gets or sets default 'from' email address if none given at send time.
        /// </summary>
        public string? DefaultFromEmailAddress { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether set to 'true' if you want'<see href="https://docs.sendgrid.com/for-developers/sending-email/sandbox-mode">Sandbox Mode</see>' turned on.
        /// </summary>
        public bool SandboxMode { get; set; } = false;

        /// <summary>
        /// Gets or sets a value indicating whether logs errors to the ILogger.
        /// </summary>
        public bool LogErrors { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether logs successful sends to the ILogger.
        /// </summary>
        public bool LogSuccesses { get; set; } = false;
    }
}

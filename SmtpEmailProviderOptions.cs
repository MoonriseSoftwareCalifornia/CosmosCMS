// <copyright file="SmtpEmailProviderOptions.cs" company="Moonrise Software, LLC">
// Copyright (c) Moonrise Software, LLC. All rights reserved.
// Licensed under the GNU Public License, Version 3.0 (https://www.gnu.org/licenses/gpl-3.0.html)
// See https://github.com/MoonriseSoftwareCalifornia/CosmosCMS
// for more information concerning the license and the contributors participating to this project.
// </copyright>

namespace Cosmos.EmailServices
{
    /// <summary>
    /// SMTP Email client properties.
    /// </summary>
    public class SmtpEmailProviderOptions
    {
        /// <summary>
        /// Gets or sets the efault "from" email address.
        /// </summary>
        public string? DefaultFromEmailAddress { get; set; }

        /// <summary>
        /// Gets or sets sMTP Host name.
        /// </summary>
        public string? Host { get; set; }

        /// <summary>
        /// Gets or sets account user name.
        /// </summary>
        public string? UserName { get; set; }

        /// <summary>
        /// Gets or sets sMTP Host password.
        /// </summary>
        public string? Password { get; set; }

        /// <summary>
        /// Gets or sets SMTP Port.
        /// </summary>
        public int Port { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether use SSL for communications?.
        /// </summary>
        /// <remarks>Default is false because TLS is the default mode.</remarks>
        public bool UsesSsl { get; set; } = false; // Uses TLS by default
    }
}
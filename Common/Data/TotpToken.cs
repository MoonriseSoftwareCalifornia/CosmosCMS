// <copyright file="TotpToken.cs" company="Moonrise Software, LLC">
// Copyright (c) Moonrise Software, LLC. All rights reserved.
// Licensed under the GNU Public License, Version 3.0 (https://www.gnu.org/licenses/gpl-3.0.html)
// See https://github.com/MoonriseSoftwareCalifornia/CosmosCMS
// for more information concerning the license and the contributors participating to this project.
// </copyright>

namespace Cosmos.Common.Data
{
    using System;
    using System.ComponentModel.DataAnnotations;
    using Newtonsoft.Json;

    /// <summary>
    /// Represents a Time-based One-Time Password (TOTP) token for user authentication.
    /// </summary>
    public class TotpToken
    {
        /// <summary>
        /// Gets or sets unique identifier for the TOTP token.
        /// </summary>
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        /// <summary>
        /// Gets or sets the user ID associated with the TOTP token.
        /// </summary>
        public string UserId { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the email address associated with the TOTP token.
        /// </summary>
        public string Email { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the token string that represents the TOTP.
        /// </summary>
        public string Token { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the date and time when the TOTP token was created.
        /// </summary>
        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

        /// <summary>
        /// Gets or sets the date and time when the TOTP token expires.
        /// </summary>
        public DateTimeOffset ExpiresAt { get; set; } = DateTimeOffset.UtcNow.AddMinutes(20); // Default expiration time of 15 minutes

        /// <summary>
        /// Gets or sets the time-to-live (TTL) for the TOTP token in seconds.
        /// </summary>
        /// <remarks>Default is 900 seconds or 15 minutes.</remarks>
        public int ttl { get; set; } = 900; // Time to live in seconds (15 minutes)
    }
}

// <copyright file="EmbedToken.cs" company="Cosmos Website Solutions, Inc.">
// Copyright (c) Cosmos Website Solutions, Inc.. All rights reserved.
// Licensed under the GNU Public License, Version 3.0 (https://www.gnu.org/licenses/gpl-3.0.html)
// See https://github.com/MoonriseSoftwareCalifornia/CosmosCMS
// for more information concerning the license and the contributors participating to this project.
// </copyright>

namespace Cosmos.Common.Services.PowerBI
{
    using System;
    using Microsoft.Rest;

    /// <summary>
    /// Embed Token.
    /// </summary>
    public class EmbedToken
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="EmbedToken"/> class.
        /// </summary>
        public EmbedToken()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="EmbedToken"/> class.
        /// </summary>
        /// <param name="token">Embed token from Power BI.</param>
        /// <param name="tokenId">Token ID.</param>
        /// <param name="expiration">Expiration date and time of token.</param>
        public EmbedToken(string token, Guid tokenId, DateTime expiration)
        {
            Token = token;
            TokenId = tokenId;
            Expiration = expiration;
        }

        /// <summary>
        /// Gets or sets the embed token from Power BI.
        /// </summary>
        public string Token { get; set; }

        /// <summary>
        /// Gets or sets the token ID.
        /// </summary>
        public Guid TokenId { get; set; }

        /// <summary>
        /// Gets or sets the expiration date and time of token.
        /// </summary>
        public DateTimeOffset Expiration { get; set; }

        /// <summary>
        /// Validates if the token is present.
        /// </summary>
        /// <exception cref="ValidationException">Fails if the token is not present.</exception>
        public virtual void Validate()
        {
            if (Token == null)
            {
                throw new ValidationException(ValidationRules.CannotBeNull, "Token");
            }
        }
    }
}

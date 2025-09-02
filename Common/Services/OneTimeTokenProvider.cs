// <copyright file="OneTimeTokenProvider.cs" company="Moonrise Software, LLC">
// Copyright (c) Moonrise Software, LLC. All rights reserved.
// Licensed under the GNU Public License, Version 3.0 (https://www.gnu.org/licenses/gpl-3.0.html)
// See https://github.com/MoonriseSoftwareCalifornia/CosmosCMS
// for more information concerning the license and the contributors participating to this project.
// </copyright>

namespace Cosmos.Common.Services
{
    using System;
    using System.Threading.Tasks;
    using Cosmos.Common.Data;
    using Microsoft.AspNetCore.Identity;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.Logging;

    /// <summary>
    /// One time token provider with default set to 15 minutes.
    /// </summary>
    /// <typeparam name="TUser">Identity user type.</typeparam>
    public class OneTimeTokenProvider<TUser>
        where TUser : IdentityUser
    {
        private readonly ApplicationDbContext dbContext;
        private readonly ILogger logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="OneTimeTokenProvider{TUser}"/> class.
        /// </summary>
        /// <param name="dbContext">Database context.</param>
        /// <param name="logger">Log service.</param>
        public OneTimeTokenProvider(
            ApplicationDbContext dbContext,
            ILogger logger)
        {
            this.dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        ///  Indicates the verification window for a one-time token.
        /// </summary>
        public enum VerificationResult
        {
            /// <summary>
            /// Token is valid.
            /// </summary>
            Valid,

            /// <summary>
            /// Token is invalid.
            /// </summary>
            Invalid,

            /// <summary>
            /// Token is expired.
            /// </summary>
            Expired
        }

        /// <summary>
        ///  Generates a one-time token for a given user.
        /// </summary>
        /// <param name="user">Identity user.</param>
        /// <returns>Token value.</returns>
        public async Task<string> GenerateAsync(TUser user)
        {
            var userEntity = (IdentityUser)user;
            var token = RandomKeyGenerator();

            var entity = new TotpToken()
            {
                CreatedAt = DateTimeOffset.UtcNow,
                Email = userEntity.NormalizedEmail,
                UserId = userEntity.Id,
                Token = token
            };

            dbContext.TotpTokens.Add(entity);
            _ = await dbContext.SaveChangesAsync();
            logger.LogInformation("Generated one-time token for user {UserId} with token {Token}", userEntity.Id, token);
            return token;
        }

        /// <summary>
        ///  Validates a login token for a given user.
        /// </summary>
        /// <param name="token">Token value to validate.</param>
        /// <param name="user">IdentityUser.</param>
        /// <param name="removeToken">Remove token if present.</param>
        /// <returns>Results.</returns>
        public async Task<VerificationResult> ValidateAsync(string token, TUser user, bool removeToken = true)
        {
            if (dbContext == null)
            {
                throw new ArgumentNullException(nameof(dbContext));
            }

            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }

            if (string.IsNullOrWhiteSpace(token))
            {
                return VerificationResult.Invalid;
            }

            var identityUser = await dbContext.Users.FirstOrDefaultAsync(u => u.NormalizedEmail == user.NormalizedEmail);

            if (!identityUser.EmailConfirmed)
            {
                logger.LogWarning("User {UserId} email is not confirmed, cannot verify token.", identityUser.Id);

                // User email is not confirmed, so we cannot verify the token.
                return VerificationResult.Invalid;
            }

            if (identityUser.LockoutEnabled && identityUser.LockoutEnd.HasValue && identityUser.LockoutEnd > DateTimeOffset.UtcNow)
            {
                logger.LogWarning("User {UserId} is locked out, cannot verify token.", identityUser.Id);

                // User is locked out, so we cannot verify the token.
                return VerificationResult.Invalid;
            }

            var totpEntity = await dbContext.TotpTokens.FirstOrDefaultAsync(f => f.Token == token);
            if (totpEntity == null)
            {
                logger.LogWarning("Token {Token} does not exist in the database.", token);

                // Token does not exist in the database
                return VerificationResult.Invalid;
            }

            if (totpEntity.UserId != identityUser.Id || totpEntity.Email != identityUser.NormalizedEmail)
            {
                logger.LogWarning("Token {Token} does not match user {UserId}.", token, identityUser.Id);

                // Token does not match the user
                return VerificationResult.Invalid;
            }

            if (totpEntity.ExpiresAt < DateTimeOffset.UtcNow)
            {
                logger.LogWarning("Token {Token} has expired for user {UserId}.", token, identityUser.Id);

                // Token has expired
                return VerificationResult.Expired;
            }

            if (removeToken)
            {
                dbContext.TotpTokens.Remove(totpEntity);
                await dbContext.SaveChangesAsync();
            }

            logger.LogInformation("Token {Token} is valid for user {UserId}.", token, identityUser.Id);
            return VerificationResult.Valid;
        }

        private string RandomKeyGenerator(int length = 32)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
            var data = new byte[length];
            System.Security.Cryptography.RandomNumberGenerator.Fill(data);
            var result = new char[length];

            for (int i = 0; i < length; i++)
            {
                result[i] = chars[data[i] % chars.Length];
            }

            return new string(result);
        }
    }
}

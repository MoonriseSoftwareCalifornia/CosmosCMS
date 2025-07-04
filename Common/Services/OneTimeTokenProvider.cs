// <copyright file="OneTimeTokenProvider.cs" company="Moonrise Software, LLC">
// Copyright (c) Moonrise Software, LLC. All rights reserved.
// Licensed under the GNU Public License, Version 3.0 (https://www.gnu.org/licenses/gpl-3.0.html)
// See https://github.com/MoonriseSoftwareCalifornia/CosmosCMS
// for more information concerning the license and the contributors participating to this project.
// </copyright>

namespace Cosmos.Common.Services
{
    using System;
    using System.Linq;
    using System.Threading.Tasks;
    using Cosmos.Common.Data;
    using Microsoft.AspNetCore.Identity;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.Logging;
    using OtpNet;

    /// <summary>
    /// One time token provider with default set to 15 minutes.
    /// </summary>
    /// <typeparam name="TUser">Identity user type.</typeparam>
    public class OneTimeTokenProvider<TUser>
        where TUser : IdentityUser
    {
        private readonly ApplicationDbContext dbContext;
        private readonly string key;
        private readonly Totp totp;

        /// <summary>
        /// Initializes a new instance of the <see cref="OneTimeTokenProvider{TUser}"/> class.
        /// </summary>
        /// <param name="dbContext">Database context.</param>
        /// <param name="logger">Log service.</param>
        public OneTimeTokenProvider(
            ApplicationDbContext dbContext,
            ILogger<DataProtectorTokenProvider<TUser>> logger)
        {
            this.dbContext = dbContext;
            var otpKey = dbContext.Settings
                .FirstOrDefaultAsync(s => s.Name == "OneTimeTokenProviderKey").Result;

            if (otpKey == null)
            {
                var value = KeyGeneration.GenerateRandomKey(20);
                otpKey = new Setting()
                {
                    Name = "OneTimeTokenProviderKey",
                    Value = Base32Encoding.ToString(value),
                    Description = "Key used to generate one-time tokens for users.",
                    Group = "Security"
                };
                dbContext.Settings.Add(otpKey);
                _ = dbContext.SaveChangesAsync().Result;
            }

            totp = new Totp(Base32Encoding.ToBytes(otpKey.Value), step: 600, totpSize: 8, mode: OtpHashMode.Sha256);
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
        /// <param name="manager">User manager.</param>
        /// <param name="user">Identity user.</param>
        /// <returns>Token value.</returns>
        public async Task<string> GenerateAsync(UserManager<TUser> manager, TUser user)
        {
            var userEntity = (IdentityUser)user;
            var token = totp.ComputeTotp();

            var entity = new TotpToken()
            {
                CreatedAt = DateTimeOffset.UtcNow,
                Email = userEntity.NormalizedEmail,
                UserId = userEntity.Id,
                Token = token
            };

            dbContext.TotpTokens.Add(entity);
            _ = await dbContext.SaveChangesAsync();
            return token;
        }

        /// <summary>
        ///  Validates a login token for a given user.
        /// </summary>
        /// <param name="token">Token value to validate.</param>
        /// <param name="manager">User manager.</param>
        /// <param name="user">IdentityUser.</param>
        /// <param name="removeToken">Remove token if present.</param>
        /// <returns>Results.</returns>
        public async Task<VerificationResult> ValidateAsync(string token, UserManager<TUser> manager, TUser user, bool removeToken = true)
        {
            var identityUser = await manager.FindByIdAsync(user.Id);

            if (identityUser == null)
            {
                return VerificationResult.Invalid;
            }

            if (!identityUser.EmailConfirmed)
            {
                // User email is not confirmed, so we cannot verify the token.
                return VerificationResult.Invalid;
            }

            if (identityUser.LockoutEnabled && identityUser.LockoutEnd.HasValue && identityUser.LockoutEnd > DateTimeOffset.UtcNow)
            {
                // User is locked out, so we cannot verify the token.
                return VerificationResult.Invalid;
            }

            if (string.IsNullOrWhiteSpace(token))
            {
                return VerificationResult.Invalid;
            }

            // Add one step verification window to allow for clock skew and network delay.
            var window = new VerificationWindow(previous: 1, future: 1);

            var result = totp.VerifyTotp(token, out long timeStepMatched, window);
            if (!result)
            {
                return VerificationResult.Invalid;
            }

            if (timeStepMatched < 0)
            {
                // Token is too old
                return VerificationResult.Expired;
            }

            var totpEntity = await dbContext.TotpTokens.FirstOrDefaultAsync(f => f.Token == token);
            if (totpEntity == null)
            {
                // Token does not exist in the database
                return VerificationResult.Invalid;
            }

            if (totpEntity.UserId != identityUser.Id || totpEntity.Email != identityUser.NormalizedEmail)
            {
                // Token does not match the user
                return VerificationResult.Invalid;
            }

            if (removeToken)
            {
                dbContext.TotpTokens.Remove(totpEntity);
                await dbContext.SaveChangesAsync();
            }

            return VerificationResult.Valid;
        }
    }
}

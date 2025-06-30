// <copyright file="OneTimeTokenProvider.cs" company="Moonrise Software, LLC">
// Copyright (c) Moonrise Software, LLC. All rights reserved.
// Licensed under the GNU Public License, Version 3.0 (https://www.gnu.org/licenses/gpl-3.0.html)
// See https://github.com/MoonriseSoftwareCalifornia/CosmosCMS
// for more information concerning the license and the contributors participating to this project.
// </copyright>

namespace Cosmos.Common.Services
{
    using System;
    using System.ComponentModel;
    using System.Threading.Tasks;
    using Cosmos.Common.Data;
    using Microsoft.AspNetCore.DataProtection;
    using Microsoft.AspNetCore.Identity;
    using Microsoft.Azure.Cosmos;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Options;

    /// <summary>
    /// One time token provider with default set to 15 minutes.
    /// </summary>
    /// <typeparam name="TUser">Identity user type.</typeparam>
    public class OneTimeTokenProvider<TUser> : DataProtectorTokenProvider<TUser>
        where TUser : IdentityUser
    {
        private readonly ApplicationDbContext dbContext;

        /// <summary>
        /// Initializes a new instance of the <see cref="OneTimeTokenProvider{TUser}"/> class.
        /// </summary>
        /// <param name="dbContext">Database context.</param>
        /// <param name="dataProtectionProvider">IDataProtectionProvider.</param>
        /// <param name="options">Options.</param>
        /// <param name="logger">Log service.</param>
        public OneTimeTokenProvider(
            ApplicationDbContext dbContext,
            IDataProtectionProvider dataProtectionProvider,
            IOptions<OneTimeTokenProviderOptions> options,
            ILogger<DataProtectorTokenProvider<TUser>> logger)
            : base(dataProtectionProvider, options, logger)
        {
            this.dbContext = dbContext;
        }

        /// <inheritdoc />
        public override async Task<string> GenerateAsync(string purpose, UserManager<TUser> manager, TUser user)
        {
            var userEntity = (IdentityUser)user;
            var token = await base.GenerateAsync(purpose, manager, user);

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

        /// <inheritdoc />
        public override async Task<bool> ValidateAsync(string purpose, string token, UserManager<TUser> manager, TUser user)
        {
            return await ValidateAsync(purpose, token, manager, user, true);
        }

        /// <summary>
        ///  Validates a login token for a given user.
        /// </summary>
        /// <param name="purpose">Reason for token.</param>
        /// <param name="token">Token value to validate.</param>
        /// <param name="manager">User manager.</param>
        /// <param name="user">IdentityUser.</param>
        /// <param name="removeToken">Remove token if present.</param>
        /// <returns>Results.</returns>
        public async Task<bool> ValidateAsync(string purpose, string token, UserManager<TUser> manager, TUser user, bool removeToken = true)
        {
            var result = await base.ValidateAsync(purpose, token, manager, user);
            var userObject = await manager.FindByIdAsync(user.Id);
            if (userObject != null)
            {
                var totpEntity = await dbContext.TotpTokens.FirstOrDefaultAsync(f => f.Token == token);
                if (totpEntity != null)
                {
                    if (removeToken)
                    {
                        dbContext.TotpTokens.Remove(totpEntity);
                        await dbContext.SaveChangesAsync();
                    }

                    return true;
                }
            }

            return false;
        }
    }
}

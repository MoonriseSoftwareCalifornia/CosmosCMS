// <copyright file="OneTimeTokenProvider.cs" company="Moonrise Software, LLC">
// Copyright (c) Moonrise Software, LLC. All rights reserved.
// Licensed under the GNU Public License, Version 3.0 (https://www.gnu.org/licenses/gpl-3.0.html)
// See https://github.com/MoonriseSoftwareCalifornia/CosmosCMS
// for more information concerning the license and the contributors participating to this project.
// </copyright>

namespace Cosmos.Common.Services
{
    using System;
    using Microsoft.AspNetCore.Identity;

    /// <summary>
    /// One time token provider options.
    /// </summary>
    public class OneTimeTokenProviderOptions : DataProtectionTokenProviderOptions
    {
        /// <summary>
        /// Creates a new instance of <see cref="OneTimeTokenProviderOptions"/>.
        /// </summary>
        public OneTimeTokenProviderOptions()
        {
            this.TokenLifespan = TimeSpan.FromMinutes(15);
            this.Name = "OneTimeToken";
        }
    }
}

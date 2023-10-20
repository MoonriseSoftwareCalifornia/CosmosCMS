// <copyright file="IdentityHostingStartup.cs" company="Moonrise Software, LLC">
// Copyright (c) Moonrise Software, LLC. All rights reserved.
// Licensed under the GNU Public License, Version 3.0 (https://www.gnu.org/licenses/gpl-3.0.html)
// See https://github.com/MoonriseSoftwareCalifornia/CosmosCMS
// for more information concerning the license and the contributors participating to this project.
// </copyright>

using Cosmos.Cms.Areas.Identity;

[assembly: HostingStartup(typeof(IdentityHostingStartup))]

namespace Cosmos.Cms.Areas.Identity
{
    /// <summary>
    /// Identity hosting startup class.
    /// </summary>
    public class IdentityHostingStartup : IHostingStartup
    {
        /// <summary>
        /// Configure method.
        /// </summary>
        /// <param name="builder">Web host builder.</param>
        public void Configure(IWebHostBuilder builder)
        {
            builder.ConfigureServices((context, services) => { });
        }
    }
}
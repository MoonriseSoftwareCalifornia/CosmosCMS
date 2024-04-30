// <copyright file="ServiceCollectionExtensions.cs" company="Cosmos Website Solutions, Inc.">
// Copyright (c) Cosmos Website Solutions, Inc.. All rights reserved.
// Licensed under the GNU Public License, Version 3.0 (https://www.gnu.org/licenses/gpl-3.0.html)
// See https://github.com/MoonriseSoftwareCalifornia/CosmosCMS
// for more information concerning the license and the contributors participating to this project.
// </copyright>

namespace Cosmos.EmailServices
{
    using Microsoft.AspNetCore.Identity.UI.Services;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Options;

    /// <summary>
    /// extension methods for <see cref="IServiceCollection"/>.
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Adds the SendGrid Email Provider to the services collection.
        /// </summary>
        /// <param name="services">Startup services collection.</param>
        /// <param name="options">SendGrid provider options.</param>
        public static void AddSendGridEmailProvider(this IServiceCollection services, SendGridEmailProviderOptions options)
        {
            services.AddSingleton(Options.Create(options));
            services.AddTransient<IEmailSender, SendGridEmailSender>();
        }

        /// <summary>
        /// Adds the default Azure Email Communication Services.
        /// </summary>
        /// <param name="services">Startup services collection.</param>
        /// <param name="options">Azure Communications email provider options.</param>
        public static void AddAzureCommunicationEmailSenderProvider(this IServiceCollection services, AzureCommunicationEmailProviderOptions options)
        {
            services.AddSingleton(Options.Create(options));
            services.AddTransient<IEmailSender, AzureCommunicationEmailSender>();
        }

        /// <summary>
        /// Add SMTP EMail Provider.
        /// </summary>
        /// <param name="services">Startup services collection.</param>
        /// <param name="options">SMTP Email provider options.</param>
        public static void AddSmtpEmailProvider(this IServiceCollection services, SmtpEmailProviderOptions options)
        {
            services.AddSingleton(Options.Create(options));
            services.AddTransient<IEmailSender, SmtpEmailSender>();
        }
    }
}
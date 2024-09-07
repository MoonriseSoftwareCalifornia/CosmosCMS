// <copyright file="ServiceCollectionExtensions.cs" company="Moonrise Software, LLC">
// Copyright (c) Moonrise Software, LLC. All rights reserved.
// Licensed under the GNU Public License, Version 3.0 (https://www.gnu.org/licenses/gpl-3.0.html)
// See https://github.com/MoonriseSoftwareCalifornia/CosmosCMS
// for more information concerning the license and the contributors participating to this project.
// </copyright>

namespace Cosmos.EmailServices
{
    using System.Configuration;
    using Microsoft.AspNetCore.Identity.UI.Services;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Options;

    /// <summary>
    /// extension methods for <see cref="IServiceCollection"/>.
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Adds the Cosmos Email Services to the services collection.
        /// </summary>
        /// <param name="services">Startup services collection.</param>
        /// <param name="configuration">System configuration.</param>
        /// <remarks>
        /// Tries to add an email service in this order:  SMTP, Azure Communication, SendGrid.
        /// </remarks>
        public static void AddCosmosEmailServices(this IServiceCollection services, IConfiguration configuration)
        {
            var adminEmail = configuration.GetValue<string>("AdminEmail") ?? throw new ConfigurationErrorsException("No AdminEmail configuration found.");

            // Attempt to add SMTP Email Provider.
            try
            {
                var smtpConfig = configuration.GetSection("SmtpEmailProviderOptions").Get<SmtpEmailProviderOptions>();
                if (smtpConfig != null
                    && string.IsNullOrEmpty(smtpConfig.Host) == false
                    && string.IsNullOrEmpty(smtpConfig.UserName) == false
                    && string.IsNullOrEmpty(smtpConfig.Password) == false
                    && smtpConfig.Port > 0)
                {
                    smtpConfig.DefaultFromEmailAddress = adminEmail;
                    services.AddSmtpEmailProvider(smtpConfig);
                    return;
                }
            }
            catch
            {
                // Ignore and try the next provider.
            }

            // Attempt to add Azure Communication Email Provider.
            var azureCommunicationConnection = configuration.GetConnectionString("AzureCommunicationConnection");
            if (!string.IsNullOrEmpty(azureCommunicationConnection))
            {
                services.AddAzureCommunicationEmailSenderProvider(new AzureCommunicationEmailProviderOptions()
                {
                    ConnectionString = azureCommunicationConnection,
                    DefaultFromEmailAddress = adminEmail
                });

                return;
            }

            // Attempt to add SendGrid Email Provider.
            var sendGridApiKey = configuration.GetValue<string>("CosmosSendGridApiKey");
            if (!string.IsNullOrEmpty(sendGridApiKey))
            {
                var sendGridOptions = new SendGridEmailProviderOptions(sendGridApiKey, adminEmail);
                services.AddSendGridEmailProvider(sendGridOptions);
                return;
            }

            // Add a NoOp Email Sender.
            services.AddNoOpEmailSender();
        }

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

        /// <summary>
        /// Adds a NoOp Email Sender to the services collection.
        /// </summary>
        /// <param name="services">Startup services collection.</param>
        public static void AddNoOpEmailSender(this IServiceCollection services)
        {
            services.AddTransient<IEmailSender, CosmosNoOpEmailSender>();
        }
    }
}
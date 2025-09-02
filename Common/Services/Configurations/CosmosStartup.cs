﻿// <copyright file="CosmosStartup.cs" company="Moonrise Software, LLC">
// Copyright (c) Moonrise Software, LLC. All rights reserved.
// Licensed under the GNU Public License, Version 3.0 (https://www.gnu.org/licenses/gpl-3.0.html)
// See https://github.com/MoonriseSoftwareCalifornia/CosmosCMS
// for more information concerning the license and the contributors participating to this project.
// </copyright>

namespace Cosmos.Cms.Common.Services.Configurations
{
    using System;
    using Azure.Identity;
    using Cosmos.Common.Services.Configurations;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.Options;

    /// <summary>
    ///     Cosmos startup configuration.
    /// </summary>
    public class CosmosStartup
    {
        private readonly IConfiguration configuration;

        /// <summary>
        /// Initializes a new instance of the <see cref="CosmosStartup"/> class with no validation.
        /// </summary>
        public CosmosStartup()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CosmosStartup"/> class.
        /// Builds boot configuration and performs validation.
        /// </summary>
        /// <param name="configuration">Startup configuration.</param>
        /// <remarks>
        /// Validates the boot configuration.
        /// </remarks>
        public CosmosStartup(IConfiguration configuration)
        {
            this.configuration = configuration;
        }

        /// <summary>
        /// Attempts to run Cosmos Startup.
        /// </summary>
        /// <remarks>
        /// <para>
        /// This method tries to run Cosmos and collects diagnostics in the process.
        /// </para>
        /// <para>
        /// Check HasErrors to see if there are any errors detected.
        /// </para>
        /// <para>
        /// If boot time value CosmosAllowSetup is set to true, then diagnostic tests are
        /// run to determine if cloud resource can be connected to. This can significantly delay boot
        /// up time for Cosmos.  Once Cosmos is setup, set CosmosAllowSetup to false.
        /// </para>
        /// </remarks>
        /// <returns>Returns <see cref="CosmosConfig"/> <see cref="IOptions{TOptions}"/>.</returns>
        public IOptions<CosmosConfig> Build()
        {
            var cosmosConfig = new CosmosConfig();

            // Disable AllowSetup if the database is using token authentication.
            cosmosConfig.SiteSettings.MultiTenantEditor = GetValue<bool?>("MultiTenantEditor") ?? false;

            // SETUP VALUES
            var allowSetup = GetValue<bool?>("CosmosAllowSetup");
            cosmosConfig.SiteSettings.AllowSetup = allowSetup ?? false;

            cosmosConfig.SiteSettings.AllowConfigEdit = GetValue<bool>("CosmosAllowConfigEdit");
            cosmosConfig.SiteSettings.AllowReset = GetValue<bool>("CosmosAllowReset");

            cosmosConfig.SiteSettings.CosmosRequiresAuthentication = GetValue<bool?>("CosmosRequiresAuthentication") ?? false;

            cosmosConfig.SiteSettings.AllowLocalAccounts = GetValue<bool?>("AllowLocalAccounts") ?? true;

            // Microsoft App ID
            cosmosConfig.SecretName = GetValue<string>("CosmosSecretName");

            var microsoftAuth = configuration.GetSection("MicrosoftOAuth").Get<AzureAD>()
                                ?? configuration.GetSection("AzureAD").Get<AzureAD>();

            if (microsoftAuth != null)
            {
                cosmosConfig.MicrosoftAppId = microsoftAuth.ClientId;
            }

            cosmosConfig.SendGridConfig.EmailFrom = "no-reply@moonrise.net";
            cosmosConfig.SendGridConfig.SendGridKey = GetValue<string>("CosmosSendGridApiKey");

            var editorUrl = GetValue<string>("CosmosEditorUrl");

            if (!string.IsNullOrEmpty(editorUrl))
            {
                cosmosConfig.EditorUrls.Add(new EditorUrl() { CloudName = "Azure", Url = editorUrl });
            }

            return Options.Create(cosmosConfig);
        }

        /// <summary>
        /// Gets a value from the configuration, and records what was found.
        /// </summary>
        /// <typeparam name="T">Type to conver value to.</typeparam>
        /// <param name="valueName">Parameter value.</param>
        /// <returns>Returns type.</returns>
        private T GetValue<T>(string valueName)
        {
            var val = configuration[valueName];

            object outputValue;

            if (typeof(T) == typeof(bool))
            {
                // default to false
                if (string.IsNullOrEmpty(val))
                {
                    outputValue = false;
                }
                else if (bool.TryParse(val, out bool parsedValue))
                {
                    outputValue = parsedValue;
                }
                else
                {
                    outputValue = false;
                }
            }
            else if (typeof(T) == typeof(bool?))
            {
                if (bool.TryParse(val, out bool parsedValue))
                {
                    outputValue = parsedValue;
                }
                else
                {
                    outputValue = null;
                }
            }
            else if (typeof(T) == typeof(Uri))
            {
                if (Uri.TryCreate(val, UriKind.Absolute, out Uri uri))
                {
                    outputValue = uri;
                }
                else
                {
                    outputValue = null;
                }
            }
            else
            {
                outputValue = val;
            }

            return (T)outputValue;
        }
    }
}
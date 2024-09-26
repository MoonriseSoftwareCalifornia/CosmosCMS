// <copyright file="Settings.cs" company="Moonrise Software, LLC">
// Copyright (c) Moonrise Software, LLC. All rights reserved.
// Licensed under the GNU Public License, Version 3.0 (https://www.gnu.org/licenses/gpl-3.0.html)
// See https://github.com/MoonriseSoftwareCalifornia/CosmosCMS
// for more information concerning the license and the contributors participating to this project.
// </copyright>

namespace Cosmos.MicrosoftGraph
{
    using System.Reflection;
    using Microsoft.Extensions.Configuration;

    /// <summary>
    /// Configuration settings.
    /// </summary>
    public class Settings
    {
        /// <summary>
        /// Gets or sets the Entra ID client ID.
        /// </summary>
        public string? ClientId { get; set; }
        /// <summary>
        /// Gets or sets the Azure Tenant ID.
        /// </summary>
        public string? TenantId { get; set; }
        /// <summary>
        /// Gets or sets the application client secret.
        /// </summary>
        public string? ClientSecret { get; set; }

        /// <summary>
        /// Gets the Graph user scopes.
        /// </summary>
        public string[]? GraphUserScopes { get; private set; } = { "https://graph.microsoft.com/.default" };

        /// <summary>
        /// Loads the settings from default sources..
        /// </summary>
        /// <returns>Settings.</returns>
        public static Settings LoadSettings()
        {
            IConfiguration config = new ConfigurationBuilder()
                .AddUserSecrets(Assembly.GetExecutingAssembly())
                // appsettings.json is required
                .AddJsonFile("local.settings.json", optional: true)
                // appsettings.Development.json" is optional, values override appsettings.json
                .AddJsonFile($"appsettings.Development.json", optional: true)
                .AddEnvironmentVariables()
                .Build();
            return LoadSettings(config);
        }

        /// <summary>
        /// Loads the settings from the given configuration..
        /// </summary>
        /// <param name="config">Application configuration.</param>
        /// <returns>Settings</returns>
        public static Settings LoadSettings(IConfiguration config)
        {
            var settings = new Settings();

            settings.ClientId = GetValue(config, nameof(settings.ClientId));

            settings.TenantId = GetValue(config, nameof(settings.TenantId));

            settings.ClientSecret = GetValue(config, nameof(settings.ClientSecret));

            return settings ??
                throw new Exception("Could not load app settings. See README for configuration instructions.");
        }

        private static string GetValue(IConfiguration config, string key)
        {
            if (config != null)
            {
                string value = config[key];

                if (string.IsNullOrEmpty(value))
                {
                    value = config.GetValue<string>(key);
                }
                if (string.IsNullOrEmpty(value))
                {
                    value = config.GetValue<string>(key.ToUpper());
                }
                if (string.IsNullOrEmpty(value))
                {
                    value = config.GetValue<string>(key.ToLower());
                }
                if (string.IsNullOrEmpty(value))
                {
                    throw new Exception($"Could not find a value for {key} in the configuration file.");
                }
                return value;
            }
            else
            {
                throw new ArgumentNullException(nameof(config));
            }
        }
    }
}

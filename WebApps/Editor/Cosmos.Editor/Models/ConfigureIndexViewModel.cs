// <copyright file="ConfigureIndexViewModel.cs" company="Moonrise Software, LLC">
// Copyright (c) Moonrise Software, LLC. All rights reserved.
// Licensed under the GNU Public License, Version 3.0 (https://www.gnu.org/licenses/gpl-3.0.html)
// See https://github.com/MoonriseSoftwareCalifornia/CosmosCMS
// for more information concerning the license and the contributors participating to this project.
// </copyright>

using System.ComponentModel.DataAnnotations;
using Cosmos.Cms.Common.Services.Configurations;

namespace Cosmos.Cms.Models
{
    /// <summary>
    ///     Configuration index view model.
    /// </summary>
    public class ConfigureIndexViewModel : CosmosConfig
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ConfigureIndexViewModel"/> class.
        /// </summary>
        public ConfigureIndexViewModel()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ConfigureIndexViewModel"/> class.
        /// </summary>
        /// <param name="secretName">Secret name.</param>
        public ConfigureIndexViewModel(string secretName)
        {
            SecretName = secretName;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ConfigureIndexViewModel"/> class.
        /// </summary>
        /// <param name="config">Cosmos configuration.</param>
        public ConfigureIndexViewModel(CosmosConfig config)
        {
            if (config != null)
            {
                Init(config);
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ConfigureIndexViewModel"/> class.
        /// </summary>
        /// <param name="secretName">Secret name.</param>
        /// <param name="config">Cosmos configuration.</param>
        public ConfigureIndexViewModel(string secretName, CosmosConfig config)
        {
            SecretName = secretName;
            if (config != null)
            {
                Init(config);
            }
        }

        /// <summary>
        /// Gets or sets default Microsoft client ID (comes from boot time environment variables).
        /// </summary>
        public string DefaultMicrosoftClientId { get; set; }

        /// <summary>
        /// Gets or sets default Microsoft secret (comes from boot time environment variables).
        /// </summary>
        public string DefaultMicrosoftSecret { get; set; }

        /// <summary>
        ///     Gets or sets this is the object used to load and deserialize a json object.
        /// </summary>
        [Display(Name = "Import JSON")]
        public string ImportJson { get; set; }

        /// <summary>
        ///     Gets or sets aWS storage connections in JSON format.
        /// </summary>
        public string AwsS3ConnectionsJson { get; set; }

        /// <summary>
        ///     Gets or sets azure storage connections in JSON format.
        /// </summary>
        public string AzureBlobConnectionsJson { get; set; }

        /// <summary>
        ///     Gets or sets blob connection information in JSON format.
        /// </summary>
        public string BlobConnectionsJson { get; set; }

        /// <summary>
        ///     Gets or sets google storage connections in JSON format.
        /// </summary>
        public string GoogleBlobConnectionsJson { get; set; }

        /// <summary>
        ///     Gets or sets redis connections in JSON format.
        /// </summary>
        public string RedisConnectionsJson { get; set; }

        /// <summary>
        ///     Gets or sets database connection information in JSON format.
        /// </summary>
        public string SqlConnectionsJson { get; set; }

        /// <summary>
        ///     Gets or sets list of editor URLs in JSON format.
        /// </summary>
        public string EditorUrlsJson { get; set; }

        /// <summary>
        ///     Gets or sets a value indicating whether connection test success or not.
        /// </summary>
        public bool TestSuccess { get; set; }

        /// <summary>
        /// Gets a value indicating whether can save to secrets if secrets manager is configured.
        /// </summary>
        public bool CanSaveSecrets { get; private set; }

        /// <summary>
        ///     Gets the Cosmos Config.
        /// </summary>
        /// <returns>Returns a Cosmos <see cref="CosmosConfig"/>.</returns>
        public CosmosConfig GetConfig()
        {
            return new ()
            {
                SendGridConfig = SendGridConfig,
                SiteSettings = SiteSettings,
                StorageConfig = StorageConfig
            };
        }

        /// <summary>
        ///     Initializes the model.
        /// </summary>
        /// <param name="config">Cosmos configuration.</param>
        private void Init(CosmosConfig config)
        {
            SiteSettings = config.SiteSettings;
            ImportJson = string.Empty;
            SendGridConfig = config.SendGridConfig;
            StorageConfig = config.StorageConfig;
            SecretName = config.SecretName;
            EditorUrls = config.EditorUrls;
            CanSaveSecrets = config.SiteSettings.AllowConfigEdit;
            SecretName = config.SecretName;
        }
    }
}
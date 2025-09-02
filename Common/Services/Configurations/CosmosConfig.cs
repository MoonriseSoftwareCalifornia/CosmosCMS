// <copyright file="CosmosConfig.cs" company="Moonrise Software, LLC">
// Copyright (c) Moonrise Software, LLC. All rights reserved.
// Licensed under the GNU Public License, Version 3.0 (https://www.gnu.org/licenses/gpl-3.0.html)
// See https://github.com/MoonriseSoftwareCalifornia/CosmosCMS
// for more information concerning the license and the contributors participating to this project.
// </copyright>

namespace Cosmos.Cms.Common.Services.Configurations
{
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;

    /// <summary>
    ///     Cosmos configuration model.
    /// </summary>
    public class CosmosConfig
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CosmosConfig"/> class.
        /// </summary>
        public CosmosConfig()
        {
            EditorUrls = new List<EditorUrl>();
            SiteSettings = new SiteSettings();
            StorageConfig = new StorageConfig();
            SendGridConfig = new SendGridConfig();
            MicrosoftAppId = string.Empty;
        }

        /// <summary>
        ///     Gets or sets editor Urls.
        /// </summary>
        public List<EditorUrl> EditorUrls { get; set; }

        /// <summary>
        /// Gets or sets microsoft application ID used for application verification.
        /// </summary>
        public string MicrosoftAppId { get; set; } = string.Empty;

        /// <summary>
        ///     Gets or sets sendGrid configuration.
        /// </summary>
        public SendGridConfig SendGridConfig { get; set; }

        /// <summary>
        ///     Gets or sets site-wide settings.
        /// </summary>
        public SiteSettings SiteSettings { get; set; }

        /// <summary>
        ///     Gets or sets blob service configuration.
        /// </summary>
        public StorageConfig StorageConfig { get; set; }

        /// <summary>
        ///     Gets or sets environment Variable Name.
        /// </summary>
        [RegularExpression(@"^[0-9, a-z, A-Z]{1,40}$", ErrorMessage = "Secret name can only contain numbers and letters.")]
        public string SecretName { get; set; } = string.Empty;
    }
}
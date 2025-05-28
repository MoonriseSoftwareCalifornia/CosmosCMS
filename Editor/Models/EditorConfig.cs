// <copyright file="EditorConfig.cs" company="Moonrise Software, LLC">
// Copyright (c) Moonrise Software, LLC. All rights reserved.
// Licensed under the GNU Public License, Version 3.0 (https://www.gnu.org/licenses/gpl-3.0.html)
// See https://github.com/MoonriseSoftwareCalifornia/CosmosCMS
// for more information concerning the license and the contributors participating to this project.
// </copyright>

namespace Cosmos.Editor.Models
{
    using System.ComponentModel.DataAnnotations;
    using Cosmos.Editor.Data.Logic;

    /// <summary>
    /// Editor instance configuration saved in the database..
    /// </summary>
    public class EditorConfig
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="EditorConfig"/> class.
        /// </summary>
        public EditorConfig()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="EditorConfig"/> class.
        /// </summary>
        /// <param name="serializedJson">Serialized json string.</param>
        public EditorConfig(string serializedJson)
        {
            if (string.IsNullOrEmpty(serializedJson))
            {
                return;
            }

            var config = Newtonsoft.Json.JsonConvert.DeserializeObject<EditorConfig>(serializedJson);
            if (config != null)
            {
                this.AllowSetup = config.AllowSetup;
                this.BlobPublicUrl = config.BlobPublicUrl;
                this.CosmosRequiresAuthentication = config.CosmosRequiresAuthentication;
                this.IsMultiTenantEditor = config.IsMultiTenantEditor;
                this.MicrosoftAppId = config.MicrosoftAppId;
                this.PublisherUrl = config.PublisherUrl;
                this.StaticWebPages = config.StaticWebPages;
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="EditorConfig"/> class.
        /// </summary>
        /// <param name="settings">Editor settings.</param>
        public EditorConfig(IEditorSettings settings)
        {
            this.AllowSetup = settings.AllowSetup;
            this.BlobPublicUrl = settings.BlobPublicUrl;
            this.CosmosRequiresAuthentication = settings.CosmosRequiresAuthentication;
            this.IsMultiTenantEditor = settings.IsMultiTenantEditor;
            this.MicrosoftAppId = settings.MicrosoftAppId;
            this.PublisherUrl = settings.PublisherUrl;
            this.StaticWebPages = settings.StaticWebPages;
        }

        /// <summary>
        /// Gets allowed file types for the file uploader.
        /// </summary>
        public string AllowedFileTypes
        {
            get
            {
                return ".js,.css,.htm,.html,.htm,.mov,.webm,.avi,.mp4,.mpeg,.ts,.svg,.json";
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether it is possible to run setup routines for this editor instance.
        /// </summary>
        [Display(Name = "Allow setup routines")]
        public bool AllowSetup { get; set; } = false;

        /// <summary>
        /// Gets or sets URL of the blob public website (can be same as publisher URL).
        /// </summary>
        /// <remarks>
        /// Publisher URL can be the same as blob public url, but this requires
        /// request rules to be setup that route requests to blob storage. See documentation
        /// for more information.
        /// </remarks>
        [Display(Name = "Static assets URL")]
        [Required(AllowEmptyStrings = false)]
        public string BlobPublicUrl { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets a value indicating whether publisher requires authentication.
        /// </summary>
        [Display(Name = "Website requires authentication")]
        public bool CosmosRequiresAuthentication { get; set; } = false;

        /// <summary>
        /// Gets or sets a value indicating whether the editor is a multi-tenant editor.
        /// </summary>
        public bool IsMultiTenantEditor { get; set; }

        /// <summary>
        /// Gets or sets a value for the Azure Registered App ID.
        /// </summary>
        public string MicrosoftAppId { get; set; }

        /// <summary>
        ///     Gets or sets uRI of the publisher website.
        /// </summary>
        [Display(Name = "Website URL")]
        [Required(AllowEmptyStrings = false)]
        [Url]
        public string PublisherUrl { get; set; } = string.Empty;

        /// <summary>
        ///    Gets or sets a value indicating whether publish to static website.
        /// </summary>
        [Display(Name = "Static mode website")]
        public bool StaticWebPages { get; set; } = false;
    }
}

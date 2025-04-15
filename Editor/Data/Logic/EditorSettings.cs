// <copyright file="EditorSettings.cs" company="Moonrise Software, LLC">
// Copyright (c) Moonrise Software, LLC. All rights reserved.
// Licensed under the GNU Public License, Version 3.0 (https://www.gnu.org/licenses/gpl-3.0.html)
// See https://github.com/MoonriseSoftwareCalifornia/CosmosCMS
// for more information concerning the license and the contributors participating to this project.
// </copyright>

namespace Cosmos.Editor.Data.Logic
{
    using System;
    using Cosmos.Cms.Services;
    using Cosmos.Common.Data;
    using Cosmos.Editor.Models;
    using Microsoft.AspNetCore.Http;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.Caching.Memory;
    using Microsoft.Extensions.Configuration;
    using Newtonsoft.Json;

    /// <summary>
    ///   Logic for managing settings in the application.
    /// </summary>
    public class EditorSettings : IEditorSettings
    {
        /// <summary>
        /// Editor settings group name.
        /// </summary>
        public static readonly string EDITORSETGROUPNAME = "EDITORSETTINGS";

        private readonly ApplicationDbContext dbContext;
        private readonly IHttpContextAccessor httpContextAccessor;
        private readonly IMemoryCache memoryCache;
        private readonly IConfiguration configuration;
        private readonly bool isMultiTenantEditor;

        /// <summary>
        /// Initializes a new instance of the <see cref="EditorSettings"/> class.
        /// </summary>
        /// <param name="configuration">Web app configuration.</param>
        /// <param name="dbContext">Database context.</param>
        /// <param name="httpContextAccessor">Http context accessor.</param>
        /// <param name="memoryCache">Memory cache.</param>
        public EditorSettings(IConfiguration configuration, ApplicationDbContext dbContext, IHttpContextAccessor httpContextAccessor, IMemoryCache memoryCache)
        {
            this.dbContext = dbContext;
            this.httpContextAccessor = httpContextAccessor;
            this.memoryCache = memoryCache;
            this.configuration = configuration;
            this.isMultiTenantEditor = this.configuration.GetValue<bool?>("MultiTenantEditor") ?? false;
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
        /// Gets a value indicating whether the website is allowed to perform setup tasks.
        /// </summary>
        public bool AllowSetup
        {
            get
            {
                if (isMultiTenantEditor)
                {
                    return GetEditorConfig().AllowSetup;
                }

                return configuration.GetValue<bool?>("AllowSetup") ?? false;
            }
        }

        /// <summary>
        /// Gets the static website URL.
        /// </summary>
        public string BlobPublicUrl
        {
            get
            {
                if (isMultiTenantEditor)
                {
                    return GetEditorConfig().BlobPublicUrl;
                }

                return configuration.GetValue<string>("AzureBlobStorageEndPoint");
            }
        }

        /// <summary>
        /// Gets a value indicating whether the publisher requires authentication.
        /// </summary>
        public bool CosmosRequiresAuthentication
        {
            get
            {
                if (isMultiTenantEditor)
                {
                    return GetEditorConfig().CosmosRequiresAuthentication;
                }

                return configuration.GetValue<bool?>("CosmosRequiresAuthentication") ?? false;
            }
        }

        /// <summary>
        /// Gets a value indicating whether the editor is a multi-tenant editor.
        /// </summary>
        public bool IsMultiTenantEditor 
        {
            get
            {
                return isMultiTenantEditor;
            }
        }

        /// <summary>
        /// Gets the Microsoft application ID used for application verification.
        /// </summary>
        public string MicrosoftAppId
        {
            get
            {
                if (isMultiTenantEditor)
                {
                    return GetEditorConfig().MicrosoftAppId;
                }

                return configuration.GetValue<string>("MicrosoftAppId") ?? string.Empty;
            }
        }

        /// <summary>
        /// Gets the publisher or website URL.
        /// </summary>
        public string PublisherUrl
        {
            get
            {
                if (isMultiTenantEditor)
                {
                    return GetEditorConfig().BlobPublicUrl;
                }

                return configuration.GetValue<string>("CosmosPublisherUrl");
            }
        }

        /// <summary>
        /// Gets a value indicating whether the publisher is a static website.
        /// </summary>
        public bool StaticWebPages
        {
            get
            {
                if (isMultiTenantEditor)
                {
                    return GetEditorConfig().StaticWebPages;
                }

                return configuration.GetValue<bool?>("CosmosStaticWebPages") ?? false;
            }
        }

        /// <summary>
        /// Gets the blob absolute URL.
        /// </summary>
        /// <returns>Uri.</returns>
        public Uri GetBlobAbsoluteUrl()
        {
            var htmlUtilities = new HtmlUtilities();
            var config = GetEditorConfig();

            if (htmlUtilities.IsAbsoluteUri(BlobPublicUrl))
            {
                return new Uri(BlobPublicUrl);
            }
            else
            {
                return new Uri(PublisherUrl.TrimEnd('/') + "/" + BlobPublicUrl.TrimStart('/'));
            }
        }

        /// <summary>
        /// Gets the editor configuration settings.
        /// </summary>
        /// <returns>Editor configuration.</returns>
        public EditorConfig GetEditorConfig()
        {
            var setting = dbContext.Settings.FirstOrDefaultAsync(f => f.Group == EDITORSETGROUPNAME).Result;
            if (setting == null || string.IsNullOrWhiteSpace(setting.Value))
            {
                return new EditorConfig()
                {
                    AllowSetup = true,
                    IsMultiTenantEditor = this.isMultiTenantEditor,
                };
            }

            var config = new EditorConfig(setting.Value);
            return config;
        }
    }
}

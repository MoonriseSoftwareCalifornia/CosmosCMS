// <copyright file="EditorSettings.cs" company="Moonrise Software, LLC">
// Copyright (c) Moonrise Software, LLC. All rights reserved.
// Licensed under the GNU Public License, Version 3.0 (https://www.gnu.org/licenses/gpl-3.0.html)
// See https://github.com/MoonriseSoftwareCalifornia/CosmosCMS
// for more information concerning the license and the contributors participating to this project.
// </copyright>

namespace Sky.Editor.Data.Logic
{
    using System;
    using Sky.Cms.Services;
    using Cosmos.Common.Data;
    using Sky.Editor.Models;
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
        private readonly EditorConfig editorConfig;

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
            this.editorConfig = GetEditorConfig();
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
                return this.editorConfig.AllowSetup;
            }
        }

        /// <summary>
        /// Gets the static website URL.
        /// </summary>
        public string BlobPublicUrl
        {
            get
            {
                return this.editorConfig.BlobPublicUrl;
            }
        }

        /// <summary>
        /// Gets a value indicating whether the publisher requires authentication.
        /// </summary>
        public bool CosmosRequiresAuthentication
        {
            get
            {
                return this.editorConfig.CosmosRequiresAuthentication;
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
                return this.editorConfig.MicrosoftAppId;
            }
        }

        /// <summary>
        /// Gets the publisher or website URL.
        /// </summary>
        public string PublisherUrl
        {
            get
            {
                return this.editorConfig.PublisherUrl;
            }
        }

        /// <summary>
        /// Gets a value indicating whether the publisher is a static website.
        /// </summary>
        public bool StaticWebPages
        {
            get
            {
                return this.editorConfig.StaticWebPages;
            }
        }

        /// <summary>
        /// Gets the blob absolute URL.
        /// </summary>
        /// <returns>Uri.</returns>
        public Uri GetBlobAbsoluteUrl()
        {
            var htmlUtilities = new HtmlUtilities();

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
            if (memoryCache.TryGetValue($"{this.httpContextAccessor.HttpContext.Request.Host.Host}-conf", out EditorConfig config))
            {
                return config;
            }

            if (this.isMultiTenantEditor)
            {
                try
                {
                    var setting = dbContext.Settings.FirstOrDefaultAsync(f => f.Group == EDITORSETGROUPNAME);
                    setting.Wait();
                    var settingResult = setting.Result;

                    if (settingResult == null || string.IsNullOrWhiteSpace(settingResult.Value))
                    {
                        return new EditorConfig()
                        {
                            AllowSetup = true,
                            IsMultiTenantEditor = this.isMultiTenantEditor,
                        };
                    }

                    config = new EditorConfig(settingResult.Value);
                }
                catch
                {
                    return new EditorConfig()
                    {
                        AllowSetup = true,
                        IsMultiTenantEditor = this.isMultiTenantEditor,
                    };
                }
            }
            else
            {
                config = new EditorConfig()
                {
                    AllowSetup = this.configuration.GetValue<bool?>("AllowSetup") ?? false,
                    IsMultiTenantEditor = this.isMultiTenantEditor,
                    BlobPublicUrl = this.configuration.GetValue<string>("AzureBlobStorageEndPoint") ?? "/",
                    CosmosRequiresAuthentication = this.configuration.GetValue<bool?>("CosmosRequiresAuthentication") ?? false,
                    MicrosoftAppId = this.configuration.GetValue<string>("MicrosoftAppId") ?? string.Empty,
                    PublisherUrl = this.configuration.GetValue<string>("CosmosPublisherUrl"),
                    StaticWebPages = this.configuration.GetValue<bool?>("CosmosStaticWebPages") ?? true,
                };

                if (string.IsNullOrWhiteSpace(config.BlobPublicUrl))
                {
                    config.BlobPublicUrl = "/";
                }

                if (string.IsNullOrWhiteSpace(config.PublisherUrl))
                {
                    throw new ArgumentNullException("'CosmosPublisherUrl' is not set.");
                }
            }

            memoryCache.Set($"{this.httpContextAccessor.HttpContext.Request.Host.Host}-conf", config, TimeSpan.FromMinutes(5));
            return config;
        }
    }
}

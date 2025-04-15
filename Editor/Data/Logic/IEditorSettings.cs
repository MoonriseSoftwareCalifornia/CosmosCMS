// <copyright file="IEditorSettings.cs" company="Moonrise Software, LLC">
// Copyright (c) Moonrise Software, LLC. All rights reserved.
// Licensed under the GNU Public License, Version 3.0 (https://www.gnu.org/licenses/gpl-3.0.html)
// See https://github.com/MoonriseSoftwareCalifornia/CosmosCMS
// for more information concerning the license and the contributors participating to this project.
// </copyright>

namespace Cosmos.Editor.Data.Logic
{
    using System;

    /// <summary>
    /// Interface for editor settings.
    /// </summary>
    public interface IEditorSettings
    {
        /// <summary>
        ///  Gets a value indicating the allowed file types.
        /// </summary>
        string AllowedFileTypes { get; }

        /// <summary>
        /// Gets a value indicating whether the setup is allowed.
        /// </summary>
        bool AllowSetup { get; }

        /// <summary>
        /// Gets the blob storage (static web) URL.
        /// </summary>
        string BlobPublicUrl { get; }

        /// <summary>
        /// Gets a value indicating whether the publisher requires authentication.
        /// </summary>
        bool CosmosRequiresAuthentication { get; }

        /// <summary>
        /// Gets the Microsoft App ID value.
        /// </summary>
        string MicrosoftAppId { get; }

        /// <summary>
        /// Gets the Publisher or website URL.
        /// </summary>
        string PublisherUrl { get; }

        /// <summary>
        /// Gets a value indicating if this is a multi-tenant editor.
        /// </summary>
        bool IsMultiTenantEditor { get; }

        /// <summary>
        /// Gets an indication if the website uses static web page mode.
        /// </summary>
        bool StaticWebPages { get; }

        /// <summary>
        /// Gets the blob storage (static web) URL.
        /// </summary>
        /// <returns>Uri.</returns>
        Uri GetBlobAbsoluteUrl();
    }
}
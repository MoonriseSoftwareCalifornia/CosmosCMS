// <copyright file="IPlugin.cs" company="Moonrise Software, LLC">
// Copyright (c) Moonrise Software, LLC. All rights reserved.
// Licensed under the GNU Public License, Version 3.0 (https://www.gnu.org/licenses/gpl-3.0.html)
// See https://github.com/MoonriseSoftwareCalifornia/CosmosCMS
// for more information concerning the license and the contributors participating to this project.
// </copyright>

namespace Cosmos.Common.Plugins.Interfaces
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Interface for Cosmos Plugins.
    /// </summary>
    public interface IPlugin
    {
        /// <summary>
        /// Gets a unique GUID that identifies this plugin.
        /// </summary>
        Guid Id { get; }

        /// <summary>
        /// Gets author of plugin.
        /// </summary>
        string Author { get; }

        /// <summary>
        /// Gets author website.
        /// </summary>
        Uri AuthorUrl { get; }

        /// <summary>
        /// Gets a JSON string that describes the fields in the configuration interface.
        /// </summary>
        string ConfigJson { get; }

        /// <summary>
        /// Gets description of what the plugin does.
        /// </summary>
        string Description { get; }

        /// <summary>
        /// Gets a value indicating whether indicates if this plugin is only for the editor web app.
        /// </summary>
        bool EditorOnly { get; }

        /// <summary>
        /// Gets a value indicating whether indicates if a menu pick should be provided in the editor menu.
        /// </summary>
        bool EditorMenu { get; }

        /// <summary>
        /// Gets name of the plugin.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Gets version of plugin.
        /// </summary>
        string Version { get; }

        /// <summary>
        /// Gets paths to templates.
        /// </summary>
        IEnumerable<string> TemplatePaths { get; }

        /// <summary>
        /// Configuration that gets loaded from settings.
        /// </summary>
        /// <param name="config">Configuration.</param>
        void Config(string config);

        /// <summary>
        /// Execution method.
        /// </summary>
        /// <param name="args">Arguments.</param>
        /// <returns>Execution result as a string.</returns>
        string Execute(string[] args);
    }
}

// <copyright file="PluginDriver.cs" company="Moonrise Software, LLC">
// Copyright (c) Moonrise Software, LLC. All rights reserved.
// Licensed under the GNU Public License, Version 3.0 (https://www.gnu.org/licenses/gpl-3.0.html)
// See https://github.com/MoonriseSoftwareCalifornia/CosmosCMS
// for more information concerning the license and the contributors participating to this project.
// </copyright>

namespace Cosmos.Common.Plugins
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using Cosmos.Common.Plugins.Interfaces;

    /// <summary>
    /// Gets plugins for execution.
    /// </summary>
    /// <remarks><see href="https://learn.microsoft.com/en-us/dotnet/core/tutorials/creating-app-with-plugin-support"/>.</remarks>
    public class PluginDriver
    {
        private readonly string sharePath;

        /// <summary>
        /// Initializes a new instance of the <see cref="PluginDriver"/> class.
        /// </summary>
        /// <param name="sharePath">Share path.</param>
        public PluginDriver(string sharePath)
        {
            this.sharePath = sharePath;
        }

        /// <summary>
        /// Gets all the plugins loaded on this instance.
        /// </summary>
        /// <typeparam name="T">Generic type of the plugin.</typeparam>
        /// <returns>Returns an <see cref="IPlugin"/> interface.</returns>
        /// <remarks>
        /// For example either <typeparamref name="T"/> or Cosmos.Cms.Publisher.
        /// </remarks>
        public IEnumerable<IPlugin> GetPlugins<T>()
        {
            var pluginPaths = Directory.GetDirectories(GetPluginRootPath());

            IEnumerable<IPlugin> plugins = pluginPaths.SelectMany(pluginPath =>
            {
                Assembly pluginAssembly = LoadPlugin<T>(pluginPath);
                return GetPluginsFromAssembly(pluginAssembly);
            }).ToList();

            return plugins;
        }

        /// <summary>
        /// Gets all the plugins for an assembly.
        /// </summary>
        /// <param name="assembly">Assembly from which to get plugins.</param>
        /// <returns>Returns an <see cref="IEnumerable{T}"/> of <see cref="IPlugin"/>s.</returns>
        private static IEnumerable<IPlugin> GetPluginsFromAssembly(Assembly assembly)
        {
            int count = 0;
            foreach (var type in assembly.GetTypes().Where(type => typeof(IPlugin).IsAssignableFrom(type)))
            {
                IPlugin result = Activator.CreateInstance(type) as IPlugin;
                if (result != null)
                {
                    count++;
                    yield return result;
                }
            }
        }

        /// <summary>
        /// Gets the root path of the plugins.
        /// </summary>
        /// <returns>Returns the path as a <see cref="string"/>.</returns>
        private string GetPluginRootPath()
        {
            return Path.Combine(sharePath, "Plugins");
        }

        /// <summary>
        /// Loads a plugin for either publisher or editor.
        /// </summary>
        /// <typeparam name="T">Plugin type.</typeparam>
        /// <param name="relativePath">Path to where plugin is located.</param>
        /// <returns>Returns a plugin.</returns>
        /// <remarks>
        /// For example either <typeparamref name="T"/> or Cosmos.Cms.Publisher.
        /// </remarks>
        private Assembly LoadPlugin<T>(string relativePath)
        {
            // Navigate up to the solution root
            string root = GetPluginRootPath();

            string pluginLocation = Path.GetFullPath(Path.Combine(root, relativePath.Replace('\\', Path.DirectorySeparatorChar)));
            Console.WriteLine($"Loading commands from: {pluginLocation}");
            Loader loadContext = new Loader(pluginLocation);
            return loadContext.LoadFromAssemblyName(new AssemblyName(Path.GetFileNameWithoutExtension(pluginLocation)));
        }
    }
}

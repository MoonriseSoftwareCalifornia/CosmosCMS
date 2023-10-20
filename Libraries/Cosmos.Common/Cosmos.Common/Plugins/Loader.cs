// <copyright file="Loader.cs" company="Moonrise Software, LLC">
// Copyright (c) Moonrise Software, LLC. All rights reserved.
// Licensed under the GNU Public License, Version 3.0 (https://www.gnu.org/licenses/gpl-3.0.html)
// See https://github.com/MoonriseSoftwareCalifornia/CosmosCMS
// for more information concerning the license and the contributors participating to this project.
// </copyright>

namespace Cosmos.Common.Plugins
{
    using System;
    using System.Reflection;
    using System.Runtime.Loader;

    /// <summary>
    /// Loads an individual plugin.
    /// </summary>
    /// <remarks><see href="https://learn.microsoft.com/en-us/dotnet/core/tutorials/creating-app-with-plugin-support"/>.</remarks>
    internal class Loader : AssemblyLoadContext
    {
        private readonly AssemblyDependencyResolver resolver;

        /// <summary>
        /// Initializes a new instance of the <see cref="Loader"/> class.
        /// </summary>
        /// <param name="pluginPath">Install path of plugin.</param>
        public Loader(string pluginPath)
        {
            resolver = new AssemblyDependencyResolver(pluginPath);
        }

        /// <inheritdoc/>
        protected override Assembly Load(AssemblyName assemblyName)
        {
            string assemblyPath = resolver.ResolveAssemblyToPath(assemblyName);
            if (assemblyPath != null)
            {
                return LoadFromAssemblyPath(assemblyPath);
            }

            return null;
        }

        /// <inheritdoc/>
        protected override IntPtr LoadUnmanagedDll(string unmanagedDllName)
        {
            string libraryPath = resolver.ResolveUnmanagedDllToPath(unmanagedDllName);
            if (libraryPath != null)
            {
                return LoadUnmanagedDllFromPath(libraryPath);
            }

            return IntPtr.Zero;
        }
    }
}

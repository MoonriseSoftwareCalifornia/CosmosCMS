// <copyright file="IStartupTaskService.cs" company="Moonrise Software, LLC">
// Copyright (c) Moonrise Software, LLC. All rights reserved.
// Licensed under the GNU Public License, Version 3.0 (https://www.gnu.org/licenses/gpl-3.0.html)
// See https://github.com/MoonriseSoftwareCalifornia/CosmosCMS
// for more information concerning the license and the contributors participating to this project.
// </copyright>

namespace Cosmos.Editor.Services
{
    using System.Threading.Tasks;

    /// <summary>
    /// Service interface for running startup tasks asynchronously.
    /// </summary>
    internal interface IStartupTaskService
    {
        /// <summary>
        /// Runs the startup tasks asynchronously.
        /// </summary>
        /// <returns>Task.</returns>
        Task RunAsync();
    }
}

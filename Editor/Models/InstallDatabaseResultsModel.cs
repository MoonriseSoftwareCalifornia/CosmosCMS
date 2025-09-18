// <copyright file="InstallDatabaseResultsModel.cs" company="Moonrise Software, LLC">
// Copyright (c) Moonrise Software, LLC. All rights reserved.
// Licensed under the GNU Public License, Version 3.0 (https://www.gnu.org/licenses/gpl-3.0.html)
// See https://github.com/MoonriseSoftwareCalifornia/CosmosCMS
// for more information concerning the license and the contributors participating to this project.
// </copyright>

namespace Sky.Cms.Models
{
    /// <summary>
    /// Database installation view model.
    /// </summary>
    public class InstallDatabaseViewModel
    {
        /// <summary>
        /// Gets or sets a value indicating whether indicates a successful install.
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// Gets or sets data source.
        /// </summary>
        public string DataSource { get; set; }

        /// <summary>
        /// Gets or sets install message(s).
        /// </summary>
        public string Message { get; set; }
    }
}
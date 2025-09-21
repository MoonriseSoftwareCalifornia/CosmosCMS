// <copyright file="SignalResult.cs" company="Moonrise Software, LLC">
// Copyright (c) Moonrise Software, LLC. All rights reserved.
// Licensed under the GNU Public License, Version 3.0 (https://www.gnu.org/licenses/gpl-3.0.html)
// See https://github.com/MoonriseSoftwareCalifornia/CosmosCMS
// for more information concerning the license and the contributors participating to this project.
// </copyright>

namespace Sky.Cms.Models
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Signal result.
    /// </summary>
    public class SignalResult
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SignalResult"/> class.
        /// Constructor.
        /// </summary>
        public SignalResult()
        {
            Exceptions = new List<Exception>();
        }

        /// <summary>
        /// Gets or sets a value indicating whether result has one or more errors.
        /// </summary>
        public bool HasErrors { get; set; } = false;

        /// <summary>
        /// Gets exception list.
        /// </summary>
        public List<Exception> Exceptions { get; }

        /// <summary>
        /// Gets or sets result JSON.
        /// </summary>
        public string JsonValue { get; set; }
    }
}

// <copyright file="ApiResult.cs" company="Moonrise Software, LLC">
// Copyright (c) Moonrise Software, LLC. All rights reserved.
// Licensed under the GNU Public License, Version 3.0 (https://www.gnu.org/licenses/gpl-3.0.html)
// See https://github.com/MoonriseSoftwareCalifornia/CosmosCMS
// for more information concerning the license and the contributors participating to this project.
// </copyright>

namespace Cosmos.Cms.Publisher.Models
{
    /// <summary>
    /// API Result.
    /// </summary>
    public class ApiResult
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ApiResult"/> class.
        /// </summary>
        /// <param name="returnData">JSON data to return.</param>
        public ApiResult(string returnData)
        {
            ReturnData = returnData;
        }

        /// <summary>
        /// Gets date/Time Stamp.
        /// </summary>
        public DateTimeOffset DateTimeStamp { get; } = DateTimeOffset.UtcNow;

        /// <summary>
        /// Gets or sets return data.
        /// </summary>
        public string ReturnData { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether indicates success.
        /// </summary>
        public bool IsSuccess { get; set; }

        /// <summary>
        /// Gets or sets errors.
        /// </summary>
        public Dictionary<string, string> Errors { get; set; } = new Dictionary<string, string>();
    }
}

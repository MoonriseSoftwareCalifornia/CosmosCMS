// <copyright file="SendResult.cs" company="Moonrise Software, LLC">
// Copyright (c) Moonrise Software, LLC. All rights reserved.
// Licensed under the GNU Public License, Version 3.0 (https://www.gnu.org/licenses/gpl-3.0.html)
// See https://github.com/MoonriseSoftwareCalifornia/CosmosCMS
// for more information concerning the license and the contributors participating to this project.
// </copyright>

namespace Cosmos.EmailServices
{
    using System.Net;

    /// <summary>
    /// Email send result class.
    /// </summary>
    public class SendResult
    {
        /// <summary>
        /// Gets or sets the status code returned from email handler.
        /// </summary>
        public HttpStatusCode StatusCode { get; set; }

        /// <summary>
        /// Gets a value indicating whether Status Code of this response indicates success.
        /// </summary>
        public bool IsSuccessStatusCode
        {
            get
            {
                if (this.StatusCode >= HttpStatusCode.OK)
                {
                    return this.StatusCode <= (HttpStatusCode)299;
                }

                return false;
            }
        }

        /// <summary>
        /// Gets or sets message returned by email system.
        /// </summary>
        public string Message { get; set; } = string.Empty;
    }
}

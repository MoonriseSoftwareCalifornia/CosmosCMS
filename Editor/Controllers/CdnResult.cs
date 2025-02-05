// <copyright file="CdnResult.cs" company="Moonrise Software, LLC">
// Copyright (c) Moonrise Software, LLC. All rights reserved.
// Licensed under the GNU Public License, Version 3.0 (https://www.gnu.org/licenses/gpl-3.0.html)
// See https://github.com/MoonriseSoftwareCalifornia/CosmosCMS
// for more information concerning the license and the contributors participating to this project.
// </copyright>

namespace Cosmos.Editor.Controllers
{
    using System;
    using System.Net;
    using Azure;

    /// <summary>
    /// Cdn purge result.
    /// </summary>
    public class CdnResult
    {
        /// <summary>
        /// Gets or sets the HTTP status code.
        /// </summary>
        public HttpStatusCode Status { get; set; }

        /// <summary>
        /// Gets or sets the HTTP reason phrase.
        /// </summary>     
        public string ReasonPhrase { get; set; }

        /// <summary>
        /// Gets or sets the client request id that was sent to the server as x-ms-client-request-id headers.
        /// </summary>
        public string ClientRequestId { get; set; }

        /// <summary>
        /// Gets or sets the operation id.
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the operation has completed.
        /// </summary>
        public bool IsSuccessStatusCode { get; set; }

        /// <summary>
        /// Gets or sets estimated content refresh date time.
        /// </summary>
        public DateTimeOffset EstimatedFlushDateTime { get; set; } = DateTimeOffset.UtcNow;

        /// <summary>
        /// Gets or sets the response object from the Azure CDN operation.
        /// </summary>
        public Response Response { get; set; }

        /// <summary>
        /// Gets or sets the message from the CDN service.
        /// </summary>
        public string Message { get; set; }

        /// <summary>
        /// Returns the string representation of this Azure.Response.
        /// </summary>
        /// <returns>The string representation of this Azure.Response.</returns>
        public override string ToString()
        {
            return $"Status: {Status}, ReasonPhrase: {ReasonPhrase}";
        }
    }
}

// <copyright file="OtpValues.cs" company="Moonrise Software, LLC">
// Copyright (c) Moonrise Software, LLC. All rights reserved.
// Licensed under the GNU Public License, Version 3.0 (https://www.gnu.org/licenses/gpl-3.0.html)
// See https://github.com/MoonriseSoftwareCalifornia/CosmosCMS
// for more information concerning the license and the contributors participating to this project.
// </copyright>

namespace Cosmos.Common.Models
{
    using System;

    /// <summary>
    ///  Represents a one-time password (OTP) object.
    /// </summary>
    public class OtpValues
    {
        /// <summary>
        /// Gets or sets the unique identifier for the OTP.
        /// </summary>
        public Guid Id { get; set; } = Guid.Empty;

        /// <summary>
        /// Gets or sets the email address from which the OTP is sent.
        /// </summary>
        public Guid UserId { get; set; } = Guid.Empty;
    }
}

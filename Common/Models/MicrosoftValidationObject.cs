// <copyright file="MicrosoftValidationObject.cs" company="Moonrise Software, LLC">
// Copyright (c) Moonrise Software, LLC. All rights reserved.
// Licensed under the GNU Public License, Version 3.0 (https://www.gnu.org/licenses/gpl-3.0.html)
// See https://github.com/MoonriseSoftwareCalifornia/CosmosCMS
// for more information concerning the license and the contributors participating to this project.
// </copyright>

namespace Cosmos.Common.Models
{
    using System.Collections.Generic;

    /// <summary>
    /// Microsoft Validation Object.
    /// </summary>
    public class MicrosoftValidationObject
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MicrosoftValidationObject"/> class.
        /// </summary>
        public MicrosoftValidationObject()
        {
            associatedApplications = new List<AssociatedApplication>();
        }

        /// <summary>
        /// Gets or sets list of applications.
        /// </summary>
        public List<AssociatedApplication> associatedApplications { get; set; }
    }
}

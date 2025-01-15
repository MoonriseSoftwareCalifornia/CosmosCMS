// <copyright file="RedirectUrlAttribute.cs" company="Moonrise Software, LLC">
// Copyright (c) Moonrise Software, LLC. All rights reserved.
// Licensed under the GNU Public License, Version 3.0 (https://www.gnu.org/licenses/gpl-3.0.html)
// See https://github.com/MoonriseSoftwareCalifornia/CosmosCMS
// for more information concerning the license and the contributors participating to this project.
// </copyright>

using System.ComponentModel.DataAnnotations;

namespace Cosmos.Cms.Models
{
    /// <summary>
    /// Validates if a URL is a valid redirect URL.
    /// </summary>
    public class RedirectUrlAttribute : ValidationAttribute
    {
        /// <summary>
        ///     Determines if value is valid.
        /// </summary>
        /// <param name="value"></param>
        /// <param name="validationContext"></param>
        /// <returns></returns>
        protected override ValidationResult IsValid(object value, ValidationContext validationContext)
        {
            var url = (string)value;

            if (string.IsNullOrEmpty(url))
            {
                return new ValidationResult("Url is required.");
            }

            if (!url.StartsWith("/") && !url.StartsWith("http://") && !url.StartsWith("https://"))
            {
                return new ValidationResult("Url must start with '/' or 'https://' or 'http://'.");
            }

            return ValidationResult.Success;
        }
    }
}

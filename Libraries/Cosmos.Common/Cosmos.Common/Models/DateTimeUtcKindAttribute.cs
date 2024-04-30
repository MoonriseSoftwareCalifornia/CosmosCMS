// <copyright file="DateTimeUtcKindAttribute.cs" company="Cosmos Website Solutions, Inc.">
// Copyright (c) Cosmos Website Solutions, Inc.. All rights reserved.
// Licensed under the GNU Public License, Version 3.0 (https://www.gnu.org/licenses/gpl-3.0.html)
// See https://github.com/MoonriseSoftwareCalifornia/CosmosCMS
// for more information concerning the license and the contributors participating to this project.
// </copyright>

namespace Cosmos.Common.Models
{
    using System;
    using System.ComponentModel.DataAnnotations;

    /// <summary>
    ///     Ensures that a DateTime object is of kind UTC.
    /// </summary>
    public class DateTimeUtcKindAttribute : ValidationAttribute
    {
        /// <summary>
        ///     Determines if value is valid.
        /// </summary>
        /// <param name="value">Value to test.</param>
        /// <param name="validationContext">Validation context.</param>
        /// <returns>Test results returned as a <see cref="ValidationResult"/>.</returns>
        protected override ValidationResult IsValid(object value, ValidationContext validationContext)
        {
            if (value == null)
            {
                return ValidationResult.Success;
            }

            var t = value.GetType();

            if (t == typeof(DateTimeOffset))
            {
                return ValidationResult.Success;
            }

            if (t == typeof(DateTime?) || t == typeof(DateTime))
            {
                var dateTime = (DateTime?)value;

                if (dateTime.HasValue && dateTime.Value.Kind != DateTimeKind.Utc)
                {
                    return new ValidationResult($"Must be DateTimeKind.Utc, not {dateTime.Value.Kind}.");
                }
            }

            return ValidationResult.Success;
        }
    }
}
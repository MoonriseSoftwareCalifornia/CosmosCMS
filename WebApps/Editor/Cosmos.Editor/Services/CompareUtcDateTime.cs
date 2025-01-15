// <copyright file="CompareUtcDateTime.cs" company="Moonrise Software, LLC">
// Copyright (c) Moonrise Software, LLC. All rights reserved.
// Licensed under the GNU Public License, Version 3.0 (https://www.gnu.org/licenses/gpl-3.0.html)
// See https://github.com/MoonriseSoftwareCalifornia/CosmosCMS
// for more information concerning the license and the contributors participating to this project.
// </copyright>

using System;

namespace Cosmos.Cms.Services
{
    /// <summary>
    /// Compares two date/time values.
    /// </summary>
    public static class CompareUtcDateTime
    {
        /// <summary>
        /// Do the compare.
        /// </summary>
        /// <param name="date1"></param>
        /// <param name="date2"></param>
        /// <returns></returns>
        public static bool Compare(DateTimeOffset date1, DateTimeOffset date2)
        {
            return date1.Year == date2.Year && date1.Month == date2.Month && date1.Day == date2.Day &&
                date1.Hour == date2.Hour && date1.Minute == date2.Minute && date1.Second == date2.Second;
        }
    }
}

// <copyright file="DateTimeOffsetToUtcDateTimeTicksConverter.cs" company="Moonrise Software, LLC">
// Copyright (c) Moonrise Software, LLC. All rights reserved.
// Licensed under the GNU Public License, Version 3.0 (https://www.gnu.org/licenses/gpl-3.0.html)
// See https://github.com/MoonriseSoftwareCalifornia/CosmosCMS
// for more information concerning the license and the contributors participating to this project.
// </copyright>

namespace Cosmos.Common
{
    using System;
    using Microsoft.EntityFrameworkCore.Storage;
    using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

    /// <summary>
    /// Converts a <see cref="DateTimeOffset"/> value to the number of ticks representing the corresponding UTC <see
    /// cref="DateTime"/> and vice versa.
    /// </summary>
    /// <remarks>This converter transforms a <see cref="DateTimeOffset"/> into a <see cref="long"/>
    /// representing the number of ticks of the equivalent UTC <see cref="DateTime"/>. It also converts the ticks back
    /// into a <see cref="DateTimeOffset"/> with a UTC offset. This is useful for persisting <see
    /// cref="DateTimeOffset"/> values in databases that store date-time information as ticks. 
    /// Please see <see href="https://nitratine.net/blog/post/a-warning-for-ef-cores-datetimeoffsettobinaryconverter/">this article</see>
    /// for more information.</remarks>
    public class DateTimeOffsetToUtcDateTimeTicksConverter : ValueConverter<DateTimeOffset, long>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DateTimeOffsetToUtcDateTimeTicksConverter"/> class.
        /// </summary>
        /// <param name="mappingHints">
        ///     Hints that can be used by the <see cref="ITypeMappingSource" /> to create data types with appropriate
        ///     facets for the converted data.
        /// </param>
        public DateTimeOffsetToUtcDateTimeTicksConverter(ConverterMappingHints? mappingHints = null)
            : base(
                v => v.UtcDateTime.Ticks,
                v => new DateTimeOffset(v, new TimeSpan(0, 0, 0)),
                mappingHints)
        {
        }

        /// <summary>
        ///     Gets a <see cref="ValueConverterInfo" /> for the default use of this converter.
        /// </summary>
        public static ValueConverterInfo DefaultInfo { get; }
            = new (typeof(DateTimeOffset), typeof(long), i => new DateTimeOffsetToUtcDateTimeTicksConverter(i.MappingHints));
    }
}

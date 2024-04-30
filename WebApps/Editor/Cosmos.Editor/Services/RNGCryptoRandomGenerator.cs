// <copyright file="RNGCryptoRandomGenerator.cs" company="Cosmos Website Solutions, Inc.">
// Copyright (c) Cosmos Website Solutions, Inc.. All rights reserved.
// Licensed under the GNU Public License, Version 3.0 (https://www.gnu.org/licenses/gpl-3.0.html)
// See https://github.com/MoonriseSoftwareCalifornia/CosmosCMS
// for more information concerning the license and the contributors participating to this project.
// </copyright>

namespace Cosmos.Cms.Services
{
    using System;
    using System.Security.Cryptography;

    /// <summary>
    /// Random number generator.
    /// </summary>
    public class RNGCryptoRandomGenerator
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="RNGCryptoRandomGenerator"/> class.
        /// Constructor.
        /// </summary>
        public RNGCryptoRandomGenerator()
        {
        }

        /// <summary>
        /// Get next random number.
        /// </summary>
        /// <param name="minValue"></param>
        /// <param name="maxExclusiveValue"></param>
        /// <returns></returns>
        public int Next(int minValue, int maxExclusiveValue)
        {
            if (minValue >= maxExclusiveValue)
            {
                throw new ArgumentOutOfRangeException("minValue must be lower than maxExclusiveValue");
            }

            long diff = (long)maxExclusiveValue - minValue;
            long upperBound = uint.MaxValue / diff * diff;

            uint ui;
            do
            {
                ui = GetRandomUInt();
            } while (ui >= upperBound);
            return (int)(minValue + (ui % diff));
        }

        private uint GetRandomUInt()
        {
            var randomBytes = GenerateRandomBytes(sizeof(uint));
            return BitConverter.ToUInt32(randomBytes, 0);
        }

        private byte[] GenerateRandomBytes(int bytesNumber)
        {
            byte[] buffer = RandomNumberGenerator.GetBytes(bytesNumber);
            return buffer;
        }
    }
}

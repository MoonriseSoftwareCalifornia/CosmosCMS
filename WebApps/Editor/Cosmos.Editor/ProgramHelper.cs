// <copyright file="ProgramHelper.cs" company="Moonrise Software, LLC">
// Copyright (c) Moonrise Software, LLC. All rights reserved.
// Licensed under the GNU Public License, Version 3.0 (https://www.gnu.org/licenses/gpl-3.0.html)
// See https://github.com/MoonriseSoftwareCalifornia/CosmosCMS
// for more information concerning the license and the contributors participating to this project.
// </copyright>

using System.Security.Cryptography;
using System.Text;

namespace Cosmos.Editor
{
    /// <summary>
    /// Little functions to help the program.cs build.
    /// </summary>
    public static class ProgramHelper
    {
        /// <summary>
        /// Get a hash string from a string.
        /// </summary>
        /// <param name="inputString">Input string.</param>
        /// <returns>Hash.</returns>
        internal static string GetHashString(string inputString)
        {
            var sb = new StringBuilder();
            foreach (byte b in GetHash(inputString))
            {
                sb.Append(b.ToString("X2"));
            }

            return sb.ToString();
        }

        private static byte[] GetHash(string inputString)
        {
            using HashAlgorithm algorithm = SHA256.Create();
            return algorithm.ComputeHash(Encoding.UTF8.GetBytes(inputString));
        }
    }
}

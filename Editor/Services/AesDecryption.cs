// <copyright file="AesDecryption.cs" company="Moonrise Software, LLC">
// Copyright (c) Moonrise Software, LLC. All rights reserved.
// Licensed under the GNU Public License, Version 3.0 (https://www.gnu.org/licenses/gpl-3.0.html)
// See https://github.com/MoonriseSoftwareCalifornia/CosmosCMS
// for more information concerning the license and the contributors participating to this project.
// </copyright>

namespace Cosmos.Editor.Services
{
    using System;
    using System.Security.Cryptography;
    using System.Text;

    /// <summary>
    /// AesDecryption utility for CryptoJS.
    /// </summary>
    public class AesDecryption
    {
        /// <summary>
        /// Decrypts a string encrypted by CryptoJS.
        /// </summary>
        /// <param name="encryptedText">payload.</param>
        /// <param name="secretKey">Encrypted key.</param>
        /// <returns>Decrypted string.</returns>
        public static string Decrypt(string encryptedText, string secretKey)
        {
            // Secret Bytes.
            byte[] secretBytes = Encoding.UTF32.GetBytes(secretKey);

            // Encrypted Bytes.
            byte[] encryptedBytes = Convert.FromBase64String(encryptedText);

            // Decrypt with AES Algorithm using Secret Key.
            using (Aes aes = Aes.Create())
            {
                aes.Mode = CipherMode.ECB;
                aes.Padding = PaddingMode.PKCS7;
                aes.Key = secretBytes;

                byte[] decryptedBytes = null;
                using (ICryptoTransform decryptor = aes.CreateDecryptor())
                {
                    decryptedBytes = decryptor.TransformFinalBlock(encryptedBytes, 0, encryptedBytes.Length);
                }

                var decrypted = Encoding.UTF8.GetString(decryptedBytes);
                return decrypted;
            }
        }
    }
}

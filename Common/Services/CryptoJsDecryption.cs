// <copyright file="CryptoJsDecryption.cs" company="Moonrise Software, LLC">
// Copyright (c) Moonrise Software, LLC. All rights reserved.
// Licensed under the GNU Public License, Version 3.0 (https://www.gnu.org/licenses/gpl-3.0.html)
// See https://github.com/MoonriseSoftwareCalifornia/CosmosCMS
// for more information concerning the license and the contributors participating to this project.
// </copyright>

namespace Cosmos.Common.Services
{
    using System;
    using System.IO;
    using System.Security.Cryptography;
    using System.Text;

    /// <summary>
    /// AesDecryption utility for CryptoJS.
    /// </summary>
    public class CryptoJsDecryption
    {
        /// <summary>
        /// Decrypts the encrypted text if not null or empty.
        /// </summary>
        /// <param name="encryptedText">Encrypted text.</param>
        /// <param name="keyText">Key text.</param>
        /// <returns>Decripted text.</returns>
        public static string Decrypt(string encryptedText, string keyText = "")
        {
            if (string.IsNullOrWhiteSpace(encryptedText))
            {
                return string.Empty;
            }

            if (string.IsNullOrWhiteSpace(keyText))
            {
                keyText = "1234567890123456";
            }

            // Decode the base64 encoded string
            byte[] encryptedBytes = Convert.FromBase64String(encryptedText);

            // Generate the key and IV using the passphrase and salt
            byte[] key = Encoding.UTF8.GetBytes(keyText);
            byte[] iv = Encoding.UTF8.GetBytes(keyText);

            // Decrypt the data
            using (var aes = Aes.Create())
            {
                aes.Key = key;
                aes.IV = iv;
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;

                using (var decryptor = aes.CreateDecryptor(aes.Key, aes.IV))
                using (var ms = new MemoryStream(encryptedBytes))
                using (var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read))
                using (var sr = new StreamReader(cs))
                {
                    return sr.ReadToEnd();
                }
            }
        }

        /// <summary>
        /// Encrypts the plain text using AES encryption with a specified key.
        /// </summary>
        /// <param name="plainText">Unencrypted text.</param>
        /// <param name="keyText">Key text.</param>
        /// <returns>Encrypted text.</returns>
        public static string Encrypt(string plainText, string keyText = "")
        {
            if (string.IsNullOrWhiteSpace(plainText))
            {
                return string.Empty;
            }

            if (string.IsNullOrWhiteSpace(keyText))
            {
                keyText = "1234567890123456";
            }

            // Generate the key and IV using the passphrase
            byte[] key = Encoding.UTF8.GetBytes(keyText);
            byte[] iv = Encoding.UTF8.GetBytes(keyText);

            // Encrypt the data
            using (var aes = Aes.Create())
            {
                aes.Key = key;
                aes.IV = iv;
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;
                using (var encryptor = aes.CreateEncryptor(aes.Key, aes.IV))
                using (var ms = new MemoryStream())
                using (var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
                using (var sw = new StreamWriter(cs))
                {
                    sw.Write(plainText);
                    sw.Flush();
                    cs.FlushFinalBlock();
                    return Convert.ToBase64String(ms.ToArray());
                }
            }
        }
    }
}

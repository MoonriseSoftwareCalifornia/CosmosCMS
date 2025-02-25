// <copyright file="Class.cs" company="Moonrise Software, LLC">
// Copyright (c) Moonrise Software, LLC. All rights reserved.
// Licensed under the GNU Public License, Version 3.0 (https://www.gnu.org/licenses/gpl-3.0.html)
// See https://github.com/MoonriseSoftwareCalifornia/CosmosCMS
// for more information concerning the license and the contributors participating to this project.
// </copyright>

namespace Cosmos.Editor.Services
{
    using System;
    using System.IO;
    using System.Security.Cryptography;
    using System.Text;

    /// <summary>
    /// SimpleCrypto utility for Cosmos.
    /// </summary>
    public class SimpleCrypto
    {
        private const int IvBitLength = 128;
        private const int KeyBitLength = 256;

        private const int Iterations = 1000;

        private readonly Encoding encoder = Encoding.Default;
        private readonly SymmetricAlgorithm algorithm;

        /// <summary>
        /// Initializes a new instance of the <see cref="SimpleCrypto"/> class.
        /// </summary>
        /// <param name="password"></param>
        /// <param name="salt"></param>
        public SimpleCrypto(byte[] password, byte[] salt)
        {
            algorithm = Aes.Create();
            algorithm.Padding = PaddingMode.PKCS7;
            algorithm.Mode = CipherMode.CBC;
            algorithm.BlockSize = 128;
            algorithm.KeySize = 256;

            using (var rfc2898DeriveBytes = new Rfc2898DeriveBytes(password, salt, Iterations))
            {
                algorithm.IV = rfc2898DeriveBytes.GetBytes(IvBitLength / 8);
                algorithm.Key = rfc2898DeriveBytes.GetBytes(KeyBitLength / 8);
            }
        }

        public byte[] IV
        {
            get { return algorithm.IV; }
        }

        public byte[] Key
        {
            get { return algorithm.Key; }
        }

        private static byte[] Transform(byte[] bytes, Func<ICryptoTransform> selectCryptoTransform)
        {
            using (var memoryStream = new MemoryStream())
            {
                using (var cryptoStream = new CryptoStream(memoryStream, selectCryptoTransform(), CryptoStreamMode.Write))
                {
                    cryptoStream.Write(bytes, 0, bytes.Length);
                }
                return memoryStream.ToArray();
            }
        }

        public string Encrypt(string unencrypted)
        {
            return Convert.ToBase64String(Encrypt(encoder.GetBytes(unencrypted)));
        }

        public string Decrypt(string encrypted)
        {
            return encoder.GetString(Decrypt(Convert.FromBase64String(encrypted)));
        }

        public byte[] Encrypt(byte[] buffer)
        {
            return Transform(buffer, algorithm.CreateEncryptor);
        }

        public byte[] Decrypt(byte[] buffer)
        {
            return Transform(buffer, algorithm.CreateDecryptor);
        }

    }
}

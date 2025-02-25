// <copyright file="AesDecryption.cs" company="Moonrise Software, LLC">
// Copyright (c) Moonrise Software, LLC. All rights reserved.
// Licensed under the GNU Public License, Version 3.0 (https://www.gnu.org/licenses/gpl-3.0.html)
// See https://github.com/MoonriseSoftwareCalifornia/CosmosCMS
// for more information concerning the license and the contributors participating to this project.
// </copyright>

namespace Cosmos.Editor.Services
{
    using System;
    using System.Text;
    using Cosmos.Editor.Models;

    /// <summary>
    /// AesDecryption utility for CryptoJS.
    /// </summary>
    public class CryptoJsDecryption
    {
        /// <summary>
        /// Decrypts a string encrypted by CryptoJS.
        /// </summary>
        /// <param name="model">payload.</param>
        /// <returns>Decrypted string.</returns>
        public static string Decrypt(CryptoJsDataModel model)
        {
            var cipherText = Convert.FromBase64String(model.CipherText);
            var password = Encoding.UTF8.GetBytes(model.Password);
            var salt = Convert.FromBase64String(model.Salt);
            var crypto = new SimpleCrypto(password, salt);

            var plainText = crypto.Decrypt(cipherText);

            return Encoding.UTF8.GetString(plainText);
        }
    }
}

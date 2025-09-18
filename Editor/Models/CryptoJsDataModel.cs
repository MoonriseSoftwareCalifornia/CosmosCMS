// <copyright file="CryptoJsDataModel.cs" company="Moonrise Software, LLC">
// Copyright (c) Moonrise Software, LLC. All rights reserved.
// Licensed under the GNU Public License, Version 3.0 (https://www.gnu.org/licenses/gpl-3.0.html)
// See https://github.com/MoonriseSoftwareCalifornia/CosmosCMS
// for more information concerning the license and the contributors participating to this project.
// </copyright>

namespace Sky.Editor.Models
{
    /// <summary>
    /// The model for the data that is passed to the CryptoJS encryption/decryption functions.
    /// </summary>
    public class CryptoJsDataModel
    {
        /// <summary>
        /// Gets or sets the cipher text.
        /// </summary>
        public string CipherText { get; set; }

        /// <summary>
        /// Gets or sets the password.
        /// </summary>
        public string Password { get; set; }

        /// <summary>
        /// Gets or sets the salt.
        /// </summary>
        public string Salt { get; set; }

        /// <summary>
        /// Gets or sets the key.
        /// </summary>
        public string Key { get; set; }

        /// <summary>
        /// Gets or sets the initialization vector.
        /// </summary>
        public string IV { get; set; }
    }
}

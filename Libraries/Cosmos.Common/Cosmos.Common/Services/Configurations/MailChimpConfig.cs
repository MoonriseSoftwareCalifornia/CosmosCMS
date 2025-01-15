// <copyright file="MailChimpConfig.cs" company="Moonrise Software, LLC">
// Copyright (c) Moonrise Software, LLC. All rights reserved.
// Licensed under the GNU Public License, Version 3.0 (https://www.gnu.org/licenses/gpl-3.0.html)
// See https://github.com/MoonriseSoftwareCalifornia/CosmosCMS
// for more information concerning the license and the contributors participating to this project.
// </copyright>

using System.ComponentModel.DataAnnotations;

namespace Cosmos.Common.Services.Configurations
{
    /// <summary>
    /// MailChimp configuration.
    /// </summary>
    public class MailChimpConfig
    {
        /// <summary>
        /// Gets or sets the MailChimp API key.
        /// </summary>
        /// <remarks>Get an <see href="https://us21.admin.mailchimp.com/account/api/">API Key from MailChimp</see>.</remarks>
        [Display(Name = "MailChimp API Key")]
        [Required(AllowEmptyStrings = false)]
        public string ApiKey { get; set; } = " ";

        /// <summary>
        ///  Gets or sets the name of the list that contacts are added to.
        /// </summary>
        [Display(Name = "Email list name")]
        [Required(AllowEmptyStrings = false)]
        public string ContactListName { get; set; } = " ";
    }
}

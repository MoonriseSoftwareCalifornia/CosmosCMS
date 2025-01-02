// <copyright file="ArticleTitleValidation.cs" company="Moonrise Software, LLC">
// Copyright (c) Moonrise Software, LLC. All rights reserved.
// Licensed under the GNU Public License, Version 3.0 (https://www.gnu.org/licenses/gpl-3.0.html)
// See https://github.com/MoonriseSoftwareCalifornia/CosmosCMS
// for more information concerning the license and the contributors participating to this project.
// </copyright>

namespace Cosmos.Cms.Data
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.Linq;
    using Cosmos.Cms.Models;
    using Cosmos.Common.Data;
    using Cosmos.Common.Data.Logic;
    using Microsoft.EntityFrameworkCore;
    using Newtonsoft.Json;

    /// <summary>
    ///     Validates that a title is valid.
    /// </summary>
    /// <remarks>
    ///     <para>This validator checks for the following:</para>
    ///     <list type="bullet">
    ///         <item>That the title is not null or empty space.</item>
    ///         <item>Ensures the title must be unique.</item>
    ///         <item>Prevents titles from being named "root," which is a key word.</item>
    ///     </list>
    ///     <para>Note: This validator will return invalid if it cannot connect to the <see cref="ApplicationDbContext" />.</para>
    /// </remarks>
    public class ArticleTitleValidation : ValidationAttribute
    {
        /// <summary>
        ///     Validates the current value.
        /// </summary>
        /// <param name="value">Value to test.</param>
        /// <param name="validationContext">Validation context.</param>
        /// <returns>Validation result.</returns>
        protected override ValidationResult IsValid(object value, ValidationContext validationContext)
        {
            // Make sure it doesn't conflict with the public blob path
            if (value == null)
            {
                return new ValidationResult("Title cannot be null or empty.");
            }

            if (validationContext == null)
            {
                return new ValidationResult("ValidationResult cannot be null or empty.");
            }

            var title = value.ToString()?.ToLower().Trim();

            if (string.IsNullOrEmpty(title) || string.IsNullOrWhiteSpace(title))
            {
                return new ValidationResult("Title cannot be an empty string.");
            }

            if (title == "root")
            {
                return new ValidationResult("Cannot name an article with the name \"root.\"");
            }

            var dbContext = (ApplicationDbContext)validationContext
                .GetService(typeof(ApplicationDbContext));

            if (dbContext == null)
            {
                throw new Exception("Validator could not connect to ApplicationDbContext.");
            }

            var setting = dbContext.Settings.FirstOrDefaultAsync(f => f.Name == "ReservedPaths").Result;

            if (setting != null)
            {
                var paths = JsonConvert.DeserializeObject<List<ReservedPath>>(setting.Value);

                foreach (var item in paths)
                {
                    var path = item.Path.TrimEnd('*').ToLower();
                    if (item.Path.EndsWith("*") ? title.StartsWith(path) : title == path)
                    {
                        return new ValidationResult($"'{value.ToString()}' conflicts with a reserved path.");
                    }
                }
            }

            var property = validationContext.ObjectType.GetProperty("ArticleNumber");

            if (property == null)
            {
                throw new Exception("Validator could not connect to ArticleNumber property.");
            }

            var articleNumber = (int)property.GetValue(validationContext.ObjectInstance, null);

            var result = dbContext.Articles.Where(a =>
                a.Title.ToLower() == title &&
                a.ArticleNumber != articleNumber &&
                a.StatusCode != (int)StatusCodeEnum.Deleted &&
                a.StatusCode != (int)StatusCodeEnum.Redirect).ToListAsync().Result;

            foreach (var item in result)
            {
                item.StatusCode = (int)StatusCodeEnum.Deleted;
            }

            if (result.Count > 0)
            {
                return new ValidationResult($"'{value.ToString()}' is already taken.");
            }

            return ValidationResult.Success;
        }
    }
}
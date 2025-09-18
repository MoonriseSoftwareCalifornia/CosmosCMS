// <copyright file="SQLiteUtils.cs" company="Moonrise Software, LLC">
// Copyright (c) Moonrise Software, LLC. All rights reserved.
// Licensed under the GNU Public License, Version 3.0 (https://www.gnu.org/licenses/gpl-3.0.html)
// See https://github.com/MoonriseSoftwareCalifornia/CosmosCMS
// for more information concerning the license and the contributors participating to this project.
// </copyright>

namespace Cosmos.Common.Data.SQlite
{
    using System;
    using System.Linq;
    using Microsoft.EntityFrameworkCore;

    /// <summary>
    /// SQLite utilities.
    /// </summary>
    public class SQLiteUtils
    {
        /// <summary>
        /// Handles the model creating for SQLite.
        /// </summary>
        /// <param name="modelBuilder">Model builder service.</param>
        public static void OnModelCreating(ModelBuilder modelBuilder)
        {
            // SQLite needs all indexes to be explicitly defined here.
            // Note that some of these indexes may already be defined via data annotations in the entity classes
            modelBuilder.Entity<Article>().HasIndex(a => a.Id).IsUnique();
            modelBuilder.Entity<ArticleLock>().HasIndex(a => a.Id).IsUnique();
            modelBuilder.Entity<ArticleLog>().HasIndex(a => a.Id).IsUnique();
            modelBuilder.Entity<ArticleNumber>().HasIndex(a => a.Id).IsUnique();
            modelBuilder.Entity<ArticlePermission>().HasIndex(a => a.ArticleId).IsUnique();
            modelBuilder.Entity<AuthorInfo>().HasIndex(a => a.Id).IsUnique();
            modelBuilder.Entity<CatalogEntry>().HasIndex(a => a.ArticleNumber).IsUnique();
            modelBuilder.Entity<Contact>().HasIndex(a => a.Id).IsUnique();
            modelBuilder.Entity<Layout>().HasIndex(a => a.Id).IsUnique();
            modelBuilder.Entity<Metric>().HasIndex(a => a.Id).IsUnique();
            modelBuilder.Entity<PublishedPage>().HasIndex(a => a.Id).IsUnique();
            modelBuilder.Entity<Setting>().HasIndex(a => a.Id).IsUnique();
            modelBuilder.Entity<Template>().HasIndex(a => a.Id).IsUnique();
            modelBuilder.Entity<TotpToken>().HasIndex(a => a.Id).IsUnique();
            modelBuilder.Entity<WebsiteAuthor>().HasIndex(a => a.Id).IsUnique();

            // Identity framework.
            modelBuilder.Entity<Microsoft.AspNetCore.Identity.IdentityRole>().HasIndex(a => a.Id).IsUnique();
            modelBuilder.Entity<Microsoft.AspNetCore.Identity.IdentityUser>().HasIndex(a => a.Id).IsUnique();
            modelBuilder.Entity<Microsoft.AspNetCore.Identity.IdentityUserLogin<string>>().HasIndex(a => a.UserId);
            modelBuilder.Entity<Microsoft.AspNetCore.Identity.IdentityUserRole<string>>().HasIndex(a => new { a.UserId, a.RoleId });
            modelBuilder.Entity<Microsoft.AspNetCore.Identity.IdentityUserToken<string>>().HasIndex(a => new { a.UserId, a.LoginProvider, a.Name });
            modelBuilder.Entity<Microsoft.AspNetCore.Identity.IdentityUserClaim<string>>().HasIndex(a => a.Id);
            modelBuilder.Entity<Microsoft.AspNetCore.Identity.IdentityRoleClaim<string>>().HasIndex(a => a.Id);
            modelBuilder.Entity<Microsoft.AspNetCore.Identity.IdentityUserLogin<string>>().HasIndex(a => new { a.LoginProvider, a.ProviderKey });
            modelBuilder.Entity<Microsoft.AspNetCore.Identity.IdentityUserLogin<string>>().HasIndex(a => new { a.LoginProvider, a.ProviderKey });

            // SQLite does not support DateTimeOffset natively, so we need to convert them to ticks.
            foreach (var entityType in modelBuilder.Model.GetEntityTypes())
            {
                var properties = entityType.ClrType.GetProperties().Where(p => p.PropertyType == typeof(DateTimeOffset)
                                                                            || p.PropertyType == typeof(DateTimeOffset?));
                foreach (var property in properties)
                {
                    modelBuilder
                        .Entity(entityType.Name)
                        .Property(property.Name)
                        .HasConversion(new DateTimeOffsetToUtcDateTimeTicksConverter());
                }
            }
        }
    }
}

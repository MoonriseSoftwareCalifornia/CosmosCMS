// <copyright file="ApplicationDbContext.cs" company="Moonrise Software, LLC">
// Copyright (c) Moonrise Software, LLC. All rights reserved.
// Licensed under the GNU Public License, Version 3.0 (https://www.gnu.org/licenses/gpl-3.0.html)
// See https://github.com/MoonriseSoftwareCalifornia/CosmosCMS
// for more information concerning the license and the contributors participating to this project.
// </copyright>

namespace Cosmos.Common.Data
{
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;
    using Microsoft.AspNetCore.Identity;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.EntityFrameworkCore.Diagnostics;

    /// <summary>
    ///     Database Context for Cosmos CMS.
    /// </summary>
    public class ApplicationDbContext : AspNetCore.Identity.CosmosDb.CosmosIdentityDbContext<IdentityUser, IdentityRole, string>, IDataProtectionKeyContext
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ApplicationDbContext"/> class.
        /// </summary>
        /// <param name="options">Database context options.</param>
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options, true)
        {
            // Adding true to base constructor to enable CosmosDB provider is backward compatible with the previous version.
        }

        /// <summary>
        /// Gets or sets catalog of Articles.
        /// </summary>
        public DbSet<CatalogEntry> ArticleCatalog { get; set; }

        /// <summary>
        /// Gets or sets article locks.
        /// </summary>
        public DbSet<ArticleLock> ArticleLocks { get; set; }

        /// <summary>
        ///     Gets or sets article activity logs.
        /// </summary>
        public DbSet<ArticleLog> ArticleLogs { get; set; }

        /// <summary>
        ///     Gets or sets article Numbers.
        /// </summary>
        public DbSet<ArticleNumber> ArticleNumbers { get; set; }

        /// <summary>
        ///     Gets or sets articles.
        /// </summary>
        public DbSet<Article> Articles { get; set; }

        /// <summary>
        /// Gets or sets public information about article authors and editors.
        /// </summary>
        public DbSet<AuthorInfo> AuthorInfos { get; set; }

        /// <summary>
        /// Gets or sets the contacts list.
        /// </summary>
        public DbSet<Contact> Contacts { get; set; }

        /// <summary>
        ///     Gets or sets website layouts.
        /// </summary>
        public DbSet<Layout> Layouts { get; set; }

        /// <summary>
        /// Gets or sets node Scripts.
        /// </summary>
        public DbSet<NodeScript> NodeScripts { get; set; }

        /// <summary>
        /// Gets or sets published pages viewable via the publisher.
        /// </summary>
        public DbSet<PublishedPage> Pages { get; set; }

        /// <summary>
        /// Gets or sets site settings.
        /// </summary>
        public DbSet<Setting> Settings { get; set; }

        /// <summary>
        ///     Gets or sets web page templates.
        /// </summary>
        public DbSet<Template> Templates { get; set; }

        /// <summary>
        /// Gets or sets data protection keys.
        /// </summary>
        public DbSet<DataProtectionKey> DataProtectionKeys { get; set; } = null!;

        /// <summary>
        ///     Determine if this service is configured.
        /// </summary>
        /// <returns>Indicates if context is configured and can connect.</returns>
        public async Task<bool> IsConfigured()
        {
            return await this.Database.CanConnectAsync();
        }

        /// <summary>
        /// Modify logging to simple logging service.
        /// </summary>
        /// <param name="optionsBuilder">DbContextOptionsBuilder.</param>
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            // Synchronous blocking on asynchronous methods can result in deadlock, and the
            // Azure Cosmos DB SDK only supports async methods.
            // https://docs.microsoft.com/en-us/ef/core/providers/cosmos/limitations#synchronous-and-blocking-calls
            // TODO: Remove all synchronous calls to the database.
            optionsBuilder.ConfigureWarnings(w => w.Ignore(CosmosEventId.SyncNotSupported));

            // https://github.com/dotnet/efcore/issues/33328
            // Note this is done because using the default azure credential causes problems here.
            base.OnConfiguring(optionsBuilder);
        }

        /// <summary>
        ///     On model creating.
        /// </summary>
        /// <param name="modelBuilder">DB Context model builder.</param>
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.HasDefaultContainer("CosmosCms");

            // Need to make a convertion so article number can be used as a partition key
            modelBuilder.Entity<ArticleNumber>()
                .ToContainer("ArticleNumber")
                .HasPartitionKey(k => k.Id)
                .HasKey(k => k.Id);

            modelBuilder.Entity<Article>()
                .Property(e => e.ArticleNumber)
                .HasConversion<string>();

            modelBuilder.Entity<Article>()
                .ToContainer("Articles")
                .HasPartitionKey(a => a.ArticleNumber)
                .HasKey(article => article.Id);

            modelBuilder.Entity<CatalogEntry>().OwnsMany(o => o.ArticlePermissions);

            modelBuilder.Entity<ArticleLock>()
                .ToContainer("ArticleLocks")
                .HasPartitionKey(a => a.Id)
                .HasKey(article => article.Id);

            modelBuilder.Entity<ArticleLog>()
                .ToContainer("ArticleLogs")
                .HasPartitionKey(k => k.Id)
                .HasKey(log => log.Id);

            modelBuilder.Entity<CatalogEntry>()
                .Property(e => e.ArticleNumber)
                .HasConversion<string>();

            modelBuilder.Entity<CatalogEntry>()
                .ToContainer("ArticleCatalog")
                .HasPartitionKey(k => k.ArticleNumber)
                .HasKey(log => log.ArticleNumber);

            modelBuilder.Entity<Layout>()
                .ToContainer("Layouts")
                .HasPartitionKey(a => a.Id)
                .HasKey(article => article.Id);

            modelBuilder.Entity<PublishedPage>()
                .ToContainer("Pages")
                .HasPartitionKey(a => a.UrlPath)
                .HasKey(article => article.Id);

            modelBuilder.Entity<Setting>()
                .ToContainer("Settings")
                .HasPartitionKey(a => a.Id)
                .HasKey(article => article.Id);

            modelBuilder.Entity<Template>()
                .ToContainer("Templates")
                .HasPartitionKey(k => k.Id)
                .HasKey(node => node.Id);

            base.OnModelCreating(modelBuilder);
        }
    }
}
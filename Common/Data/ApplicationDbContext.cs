// <copyright file="ApplicationDbContext.cs" company="Moonrise Software, LLC">
// Copyright (c) Moonrise Software, LLC. All rights reserved.
// Licensed under the GNU Public License, Version 3.0 (https://www.gnu.org/licenses/gpl-3.0.html)
// See https://github.com/MoonriseSoftwareCalifornia/CosmosCMS
// for more information concerning the license and the contributors participating to this project.
// </copyright>

namespace Cosmos.Common.Data
{
    using System;
    using System.Linq;
    using System.Threading.Tasks;
    using Azure.Identity;
    using Cosmos.DynamicConfig;
    using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;
    using Microsoft.AspNetCore.Identity;
    using Microsoft.Azure.Cosmos;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.EntityFrameworkCore.Diagnostics;
    using Microsoft.Extensions.DependencyInjection;

    /// <summary>
    ///     Database Context for Cosmos CMS.
    /// </summary>
    public class ApplicationDbContext : AspNetCore.Identity.CosmosDb.CosmosIdentityDbContext<IdentityUser, IdentityRole, string>, IDataProtectionKeyContext
    {
        private readonly IServiceProvider services;

        /// <summary>
        /// Initializes a new instance of the <see cref="ApplicationDbContext"/> class.
        /// </summary>
        /// <param name="options">Database context options.</param>
        /// <param name="services">Service provider.</param>
        public ApplicationDbContext(
            DbContextOptions<ApplicationDbContext> options,
            IServiceProvider services)
            : base(options, true)
        {
            this.services = services;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ApplicationDbContext"/> class.
        /// </summary>
        /// <param name="services">Service provider.</param>
        public ApplicationDbContext(
            IServiceProvider services)
            : base(GetDynamicOptions(services), true)
        {
            this.services = services;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ApplicationDbContext"/> class.
        /// </summary>
        /// <param name="options">Database context options.</param>
        public ApplicationDbContext(
            DbContextOptions<ApplicationDbContext> options)
            : base(options, true)
        {
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
        /// Gets or sets metrics for the site.
        /// </summary>
        public DbSet<Metric> Metrics { get; set; }

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
        /// Gets or sets the TOTP (Time-based One-Time Password) tokens for users.
        /// </summary>
        public DbSet<TotpToken> TotpTokens { get; set; } = null!;

        /// <summary>
        /// Gets or sets data protection keys.
        /// </summary>
        public DbSet<DataProtectionKey> DataProtectionKeys { get; set; } = null!;

        /// <summary>
        /// Ensure database exists and returns status.
        /// </summary>
        /// <param name="connectionString">Connection string.</param>
        /// <param name="databaseName">Database name.</param>
        /// <param name="setup">Setup database as well as test connection.</param>
        /// <returns>Success or not.</returns>
        public static DbStatus EnsureDatabaseExists(string connectionString, string databaseName, bool setup)
        {
            var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
            optionsBuilder.UseCosmos(connectionString: connectionString, databaseName: databaseName);
            using var dbContext = new ApplicationDbContext(optionsBuilder.Options);

            var result = dbContext.Database.EnsureCreatedAsync().GetAwaiter().GetResult();

            if (result)
            {
                return EnsureDatabaseExists(dbContext, setup, databaseName);
            }

            return DbStatus.CreationFailed;
        }

        /// <summary>
        /// Ensure database exists and returns status.
        /// </summary>
        /// <param name="dbContext">Database context.</param>
        /// <param name="setup">Setup database as well as test connection.</param>
        /// <param name="databaseName">Set the database name.</param>
        /// <returns>Success or not.</returns>
        public static DbStatus EnsureDatabaseExists(ApplicationDbContext dbContext, bool setup, string databaseName)
        {
            var cosmosClient = dbContext.Database.GetCosmosClient();

            DbStatus dbStatus = DbStatus.DoesNotExist;

            try
            {
                DatabaseResponse response = cosmosClient.GetDatabase(databaseName).ReadAsync().Result;
                if (response.StatusCode == System.Net.HttpStatusCode.OK)
                {
                    // Check to see if the identity containers exist.
                    var identityContainerResult = cosmosClient.GetContainer(databaseName, "Identity").ReadContainerAsync().Result;

                    // Check to see if the CMS containers exists.
                    var articleContainerResult = cosmosClient.GetContainer(databaseName, "Articles").ReadContainerAsync().Result;

                    if (identityContainerResult.StatusCode == System.Net.HttpStatusCode.OK &&
                        articleContainerResult.StatusCode == System.Net.HttpStatusCode.OK)
                    {
                        // Check to see if the database is empty.
                        var query = identityContainerResult.Container.GetItemLinqQueryable<IdentityUser>(allowSynchronousQueryExecution: true);
                        var count = query.Count();
                        if (count > 0)
                        {
                            dbStatus = DbStatus.ExistsWithUsers; // Database exists and is not empty.
                        }
                        else
                        {
                            dbStatus = DbStatus.ExistsWithNoUsers; // Database exists but is empty.
                        }
                    }
                    else
                    {
                        dbStatus = DbStatus.ExistsWithMissingContainers; // Container does not exist.
                    }
                }
            }
            catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                dbStatus = DbStatus.DoesNotExist;
            }
            catch (Exception)
            {
                throw; // Log or handle unexpected exceptions as needed
            }

            // If setup is allowed, and database either does not exist, or, has missing containers,
            // setup the database now.
            if (setup && (dbStatus == DbStatus.DoesNotExist || dbStatus == DbStatus.ExistsWithMissingContainers))
            {
                var task = dbContext.Database.EnsureCreatedAsync();
                task.Wait();
                if (task.IsCompletedSuccessfully)
                {
                    dbStatus = DbStatus.ExistsWithNoUsers; // Database exists but has no users.
                }
                else if (task.IsFaulted)
                {
                    throw task.Exception; // Database creation failed.
                }
                else
                {
                    throw new Exception("EnsureCreatedAsync() failed."); // Database creation failed.
                }
            }

            return dbStatus;
        }

        /// <summary>
        /// Gets the database connection string.
        /// </summary>
        /// <returns>Database name.</returns>
        public string GetDatabaseName()
        {
            var connectionStringProvider = this.services.GetRequiredService<IDynamicConfigurationProvider>();
            return connectionStringProvider.GetDatabaseName();
        }

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

            base.OnConfiguring(optionsBuilder);
        }

        /// <summary>
        ///     On model creating.
        /// </summary>
        /// <param name="modelBuilder">DB Context model builder.</param>
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // DEFAULT CONTAINER ENTITIES
            modelBuilder.HasDefaultContainer("CosmosCms");
            modelBuilder.Entity<Contact>()
                .HasKey(k => k.Id);
            modelBuilder.Entity<NodeScript>()
                .HasKey(k => k.Id);
            modelBuilder.Entity<TotpToken>()
                .HasKey(k => k.Id);

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

            modelBuilder.Entity<ArticleLock>()
                .ToContainer("ArticleLocks")
                .HasPartitionKey(a => a.Id)
                .HasKey(article => article.Id);

            modelBuilder.Entity<ArticleLog>()
                .ToContainer("ArticleLogs")
                .HasPartitionKey(k => k.Id)
                .HasKey(log => log.Id);

            modelBuilder.Entity<CatalogEntry>().OwnsMany(o => o.ArticlePermissions);

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

            modelBuilder.Entity<AuthorInfo>()
                .ToContainer("AuthorInfo")
                .HasPartitionKey(k => k.Id)
                .HasKey(k => k.Id);

            modelBuilder.Entity<Metric>()
                .ToContainer("Metrics")
                .HasPartitionKey(k => k.Id)
                .HasKey(k => k.Id);

            modelBuilder.Entity<DataProtectionKey>()
                .ToContainer("DataProtection")
                .HasPartitionKey(k => k.Id)
                .HasKey(k => k.Id);

            base.OnModelCreating(modelBuilder);
        }

        private static DbContextOptions<ApplicationDbContext> GetDynamicOptions(IServiceProvider services)
        {
            var connectionStringProvider = services.GetRequiredService<IDynamicConfigurationProvider>();
            var connectionString = connectionStringProvider.GetDatabaseConnectionString();

            // Note: This may be null if the cookie or website URL has not yet been set.
            if (string.IsNullOrEmpty(connectionString))
            {
                return new DbContextOptions<ApplicationDbContext>();
            }

            var databaseName = connectionStringProvider.GetDatabaseName();

            var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();

            if (connectionString.Contains("AccountKey=AccessToken", StringComparison.CurrentCultureIgnoreCase))
            {
                var conpartsDict =
                    connectionString.Split(";").Where(w =>
                    !string.IsNullOrEmpty(w)).Select(part => part.Split('='))
                    .ToDictionary(sp => sp[0], sp => sp[1], StringComparer.OrdinalIgnoreCase);

                var defaultAzureCredential = services.GetRequiredService<DefaultAzureCredential>();
                var endpoint = conpartsDict["AccountEndpoint"];
                optionsBuilder.UseCosmos(accountEndpoint: endpoint, defaultAzureCredential, databaseName);
            }
            else
            {
                optionsBuilder.UseCosmos(connectionString, databaseName: databaseName);
            }

            return optionsBuilder.Options;
        }
    }
}
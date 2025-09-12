// <copyright file="ApplicationDbContext.cs" company="Moonrise Software, LLC">
// Copyright (c) Moonrise Software, LLC. All rights reserved.
// Licensed under the GNU Public License, Version 3.0 (https://www.gnu.org/licenses/gpl-3.0.html)
// See https://github.com/MoonriseSoftwareCalifornia/CosmosCMS
// for more information concerning the license and the contributors participating to this project.
// </copyright>

namespace Cosmos.Common.Data
{
    using AspNetCore.Identity.FlexDb;
    using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;
    using Microsoft.AspNetCore.Identity;
    using Microsoft.Azure.Cosmos;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.EntityFrameworkCore.Diagnostics;
    using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
    using MySql.EntityFrameworkCore.Extensions;
    using System;
    using System.Configuration;
    using System.Linq;
    using System.Threading.Tasks;

    /// <summary>
    ///     Database Context for Cosmos CMS.
    /// </summary>
    public class ApplicationDbContext : CosmosIdentityDbContext<IdentityUser, IdentityRole, string>, IDataProtectionKeyContext
    {
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
        /// Initializes a new instance of the <see cref="ApplicationDbContext"/> class with connection string.
        /// </summary>
        /// <param name="connectionString">Connection string.</param>
        /// <remarks>
        /// Automatically determines if connection string is for Cosmos DB, MySQL or SQL Server.
        /// </remarks>
        public ApplicationDbContext(string connectionString)
            : base(CosmosDbOptionsBuilder.GetDbOptions<ApplicationDbContext>(connectionString), true)
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
        /// <returns>Success or not.</returns>
        public static DbStatus EnsureDatabaseExists(string connectionString)
        {
            using var dbContext = new ApplicationDbContext(connectionString);

            if (dbContext.Database.IsCosmos())
            {
                var databaseName = connectionString.Split(';').FirstOrDefault(s => s.StartsWith("Database=", StringComparison.InvariantCultureIgnoreCase))?.Split('=')[1];
                var cosmosClient = dbContext.Database.GetCosmosClient();
                var exists = DoesCosmosDatabaseExist(cosmosClient, databaseName).Result;

                if (exists == false)
                {
                    var task = dbContext.Database.EnsureCreatedAsync();
                    task.Wait();

                    if (task.IsFaulted)
                    {
                        return DbStatus.CreationFailed;
                    }
                }

                var userCount = dbContext.Users.Select(s => s.Id).ToListAsync().Result;
                if (userCount.Count == 0)
                {
                    return DbStatus.ExistsWithNoUsers;
                }

                return DbStatus.ExistsWithUsers;
            }

            var result = dbContext.Database.EnsureCreatedAsync().Result;

            if (result)
            {
                var userCount = dbContext.Users.CountAsync().Result;
                if (userCount == 0)
                {
                    return DbStatus.ExistsWithNoUsers;
                }

                return DbStatus.ExistsWithUsers;
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
            var isCosmosDb = optionsBuilder.IsConfigured && optionsBuilder.Options.Extensions.Any(e => e is Microsoft.EntityFrameworkCore.Cosmos.Infrastructure.Internal.CosmosOptionsExtension);

            if (isCosmosDb)
            {
                // Synchronous blocking on asynchronous methods can result in deadlock, and the
                // Azure Cosmos DB SDK only supports async methods.
                // https://docs.microsoft.com/en-us/ef/core/providers/cosmos/limitations#synchronous-and-blocking-calls
                // TODO: Remove all synchronous calls to the database.
                optionsBuilder.ConfigureWarnings(w => w.Ignore(CosmosEventId.SyncNotSupported));
            }

            base.OnConfiguring(optionsBuilder);
        }

        /// <summary>
        ///     On model creating.
        /// </summary>
        /// <param name="modelBuilder">DB Context model builder.</param>
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {

            if (this.Database.IsSqlite())
            {
                foreach (var entityType in modelBuilder.Model.GetEntityTypes())
                {
                    var properties = entityType.ClrType.GetProperties().Where(p => p.PropertyType == typeof(DateTimeOffset)
                                                                                || p.PropertyType == typeof(DateTimeOffset?));
                    foreach (var property in properties)
                    {
                        modelBuilder
                            .Entity(entityType.Name)
                            .Property(property.Name)
                            .HasConversion(new DateTimeOffsetToBinaryConverter());
                    }
                }
            }

            if (this.Database.IsCosmos())
            {
                // DEFAULT CONTAINER ENTITIES
                modelBuilder.HasDefaultContainer("CosmosCms");

                modelBuilder.Entity<Contact>()
                    .ToContainer("CosmosCms")
                    .HasPartitionKey(k => k.Id)
                    .HasKey(k => k.Id);

                modelBuilder.Entity<TotpToken>()
                    .ToContainer("CosmosCms")
                    .HasPartitionKey(k => k.Id)
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
                    .HasKey(layout => layout.Id);

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
            }
            else if (Database.IsMySql())
            {
                modelBuilder.Entity<Article>()
                    .HasIndex(a => a.ArticleNumber);

                modelBuilder.Entity<PublishedPage>()
                    .HasIndex(p => p.UrlPath);

                modelBuilder.Entity<CatalogEntry>()
                    .HasIndex(p => new { p.UrlPath });
            }

            base.OnModelCreating(modelBuilder);
        }

        private static async Task<bool> DoesCosmosDatabaseExist(CosmosClient client, string databaseId)
        {
            QueryDefinition query = new QueryDefinition(
                "select * from c where c.id = @databaseId")
                    .WithParameter("@databaseId", databaseId);

            FeedIterator<dynamic> resultSet = client.GetDatabaseQueryIterator<dynamic>(query);

            while (resultSet.HasMoreResults)
            {
                FeedResponse<dynamic> response = await resultSet.ReadNextAsync();

                return response.Count > 0;
            }

            return false;
        }
    }
}
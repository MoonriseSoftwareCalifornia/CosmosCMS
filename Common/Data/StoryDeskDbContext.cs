// <copyright file="StoryDeskDbContext.cs" company="Moonrise Software, LLC">
// Copyright (c) Moonrise Software, LLC. All rights reserved.
// Licensed under the GNU Public License, Version 3.0 (https://www.gnu.org/licenses/gpl-3.0.html)
// See https://github.com/MoonriseSoftwareCalifornia/CosmosCMS
// for more information concerning the license and the contributors participating to this project.
// </copyright>

namespace Cosmos.Common.Data
{
    using Microsoft.EntityFrameworkCore;

    /// <summary>
    /// Represents the database context for StoryDesk, managing entities related to website authors.
    /// </summary>
    public class StoryDeskDbContext : DbContext
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="StoryDeskDbContext"/> class with the specified options.
        /// </summary>
        /// <param name="options">Story Desk DB Options.</param>
        public StoryDeskDbContext(DbContextOptions<StoryDeskDbContext> options)
            : base(options)
        {
        }

        /// <summary>
        /// Gets or sets the collection of website authors in the database.
        /// </summary>
        public DbSet<StoryAuthorConfig> WebsiteAuthors { get; set; }

        /// <summary>
        /// Configures the model for the StoryDesk database context.
        /// </summary>
        /// <param name="modelBuilder">Model builder.</param>
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure your entities here
            // Example: modelBuilder.Entity<YourEntity>().ToTable("YourTableName");
            modelBuilder.Entity<StoryAuthorConfig>()
                .ToContainer("WebsiteAuthors")
                .HasPartitionKey(k => k.Id);
        }
    }
}

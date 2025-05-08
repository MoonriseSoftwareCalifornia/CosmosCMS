// <copyright file="ConnectionStringProvider.cs" company="Moonrise Software, LLC">
// Copyright (c) Moonrise Software, LLC. All rights reserved.
// Licensed under the GNU Public License, Version 3.0 (https://www.gnu.org/licenses/gpl-3.0.html)
// See https://github.com/MoonriseSoftwareCalifornia/CosmosCMS
// for more information concerning the license and the contributors participating to this project.
// </copyright>

namespace Cosmos.DynamicConfig
{
    using Microsoft.EntityFrameworkCore;

    public class DynamicConfigDbContext : DbContext
    {
        public DynamicConfigDbContext(DbContextOptions<DynamicConfigDbContext> options)
            : base(options)
        {
        }

        /// <summary>
        /// Gets or sets the connections entity.
        /// </summary>
        public DbSet<Connection> Connections { get; set; } = null!;

        /// <summary>
        /// Gets or sets the metrics entity.
        /// </summary>
        public DbSet<Metric> Metrics { get; set; } = null!;

        /// <summary>
        ///  Handles the on model creating event.
        /// </summary>
        /// <param name="modelBuilder"></param>
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Connection>()
                .ToContainer("config");

            modelBuilder.Entity<Metric>()
                .ToContainer("Metrics");

            base.OnModelCreating(modelBuilder);
        }
    }
}

﻿using Duende.IdentityServer.EntityFramework.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AspNetCore.Identity.FlexDb.EntityConfigurations
{
    public class DeviceFlowCodesEntityTypeConfiguration : IEntityTypeConfiguration<DeviceFlowCodes>
    {
        private readonly string _tableName;

        public DeviceFlowCodesEntityTypeConfiguration(string tableName = "Identity_DeviceFlowCodes")
        {
            _tableName = tableName;
        }

        public void Configure(EntityTypeBuilder<DeviceFlowCodes> builder)
        {
            builder
                .HasKey(_ => new { _.ClientId, _.SessionId, _.DeviceCode });

            builder
                .UseETagConcurrency()
                .HasPartitionKey(_ => _.SessionId);

            builder.ToContainer(_tableName);
        }
    }
}
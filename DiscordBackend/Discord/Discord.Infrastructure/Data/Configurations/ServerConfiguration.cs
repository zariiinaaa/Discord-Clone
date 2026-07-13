using Discord.Core.Entities.Servers;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Text;

namespace Discord.Infrastructure.Data.Configurations
{
    public class ServerConfiguration : IEntityTypeConfiguration<Server>
    {
        public void Configure(EntityTypeBuilder<Server> builder)
        {
            builder.ToTable("Servers");

            builder.HasKey(server => server.Id);

            builder.Property(server => server.Name)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(server => server.Description)
                .HasMaxLength(500);

            builder.Property(server => server.IconUrl)
                .HasMaxLength(500);

            builder.Property(server => server.BannerUrl)
                .HasMaxLength(500);

            builder.Property(server => server.IsPublic)
                .HasDefaultValue(false);

            builder.HasIndex(server => server.OwnerId);

            builder.HasOne(server => server.Owner)
                .WithMany(user => user.OwnedServers)
                .HasForeignKey(server => server.OwnerId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}

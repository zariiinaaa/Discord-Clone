using Discord.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Text;

namespace Discord.Infrastructure.Data.Configurations
{
    public class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
    {
        public void Configure(EntityTypeBuilder<RefreshToken> builder)
        {
            builder.ToTable("RefreshTokens");

            builder.HasKey(refreshToken => refreshToken.Id);

            builder.Property(refreshToken => refreshToken.TokenHash)
                .IsRequired()
                .HasMaxLength(500);

            builder.HasIndex(refreshToken => refreshToken.TokenHash)
                .IsUnique();

            builder.Property(refreshToken => refreshToken.ReplacedByTokenHash)
                .HasMaxLength(500);

            builder.Property(refreshToken => refreshToken.CreatedByIp)
                .HasMaxLength(45);
        }
    }
}

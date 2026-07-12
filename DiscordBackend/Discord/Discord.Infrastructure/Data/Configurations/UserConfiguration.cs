using Discord.Core.Entities;
using Discord.Core.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Text;

namespace Discord.Infrastructure.Data.Configurations
{
    public class UserConfiguration : IEntityTypeConfiguration<User>
    {
        public void Configure(EntityTypeBuilder<User> builder)
        {
            builder.ToTable("Users");

            builder.HasKey(user => user.Id);

            builder.Property(user => user.Username)
                .IsRequired()
                .HasMaxLength(32);

            builder.HasIndex(user => user.Username)
                .IsUnique();

            builder.Property(user => user.DisplayName)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(user => user.Email)
                .IsRequired()
                .HasMaxLength(256);

            builder.HasIndex(user => user.Email)
                .IsUnique();

            builder.Property(user => user.PasswordHash)
                .IsRequired()
                .HasMaxLength(500);

            builder.Property(user => user.AvatarUrl)
                .HasMaxLength(500);

            builder.Property(user => user.Bio)
                .HasMaxLength(500);

            builder.Property(user => user.Status)
                .HasConversion<int>()
                .HasDefaultValue(UserStatus.Offline);

            builder.Property(user => user.Role)
                .HasConversion<int>()
                .HasDefaultValue(PlatformRole.User);

            builder.Property(user => user.BanReason)
                .HasMaxLength(500);

            builder.HasMany(user => user.RefreshTokens)
                .WithOne(refreshToken => refreshToken.User)
                .HasForeignKey(refreshToken => refreshToken.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}

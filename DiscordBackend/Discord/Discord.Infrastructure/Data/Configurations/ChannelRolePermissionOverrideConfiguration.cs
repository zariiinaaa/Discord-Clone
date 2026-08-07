using Discord.Core.Entities.Servers;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Discord.Infrastructure.Data.Configurations;

public class ChannelRolePermissionOverrideConfiguration
    : IEntityTypeConfiguration<ChannelRolePermissionOverride>
{
    public void Configure(
        EntityTypeBuilder<ChannelRolePermissionOverride> builder)
    {
        builder.ToTable("ChannelRolePermissionOverrides");

        builder.HasKey(overrideItem => overrideItem.Id);

        builder.Property(overrideItem => overrideItem.Permission)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(overrideItem => overrideItem.OverrideType)
            .HasConversion<int>()
            .IsRequired();

        builder.HasIndex(overrideItem => new
        {
            overrideItem.ChannelId,
            overrideItem.RoleId,
            overrideItem.Permission
        })
        .IsUnique();

        builder.HasIndex(overrideItem => overrideItem.RoleId);

        builder.HasOne(overrideItem => overrideItem.Channel)
            .WithMany(channel => channel.RolePermissionOverrides)
            .HasForeignKey(overrideItem => overrideItem.ChannelId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(overrideItem => overrideItem.Role)
            .WithMany()
            .HasForeignKey(overrideItem => overrideItem.RoleId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
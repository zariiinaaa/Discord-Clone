using Discord.Core.Entities.Servers;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Discord.Infrastructure.Data.Configurations;

public class ChannelMemberPermissionOverrideConfiguration
    : IEntityTypeConfiguration<ChannelMemberPermissionOverride>
{
    public void Configure(
        EntityTypeBuilder<ChannelMemberPermissionOverride> builder)
    {
        builder.ToTable("ChannelMemberPermissionOverrides");

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
            overrideItem.UserId,
            overrideItem.Permission
        })
        .IsUnique();

        builder.HasIndex(overrideItem => overrideItem.UserId);

        builder.HasOne(overrideItem => overrideItem.Channel)
            .WithMany(channel => channel.MemberPermissionOverrides)
            .HasForeignKey(overrideItem => overrideItem.ChannelId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(overrideItem => overrideItem.User)
            .WithMany()
            .HasForeignKey(overrideItem => overrideItem.UserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
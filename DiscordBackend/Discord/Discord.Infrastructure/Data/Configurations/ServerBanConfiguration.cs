using Discord.Core.Entities.Servers;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Discord.Infrastructure.Data.Configurations;

public class ServerBanConfiguration
    : IEntityTypeConfiguration<ServerBan>
{
    public void Configure(
        EntityTypeBuilder<ServerBan> builder)
    {
        builder.HasIndex(serverBan => new
        {
            serverBan.ServerId,
            serverBan.UserId
        })
        .IsUnique();

        builder.Property(serverBan =>
                serverBan.Reason)
            .HasMaxLength(500);

        builder.HasOne(serverBan =>
                serverBan.Server)
            .WithMany()
            .HasForeignKey(serverBan =>
                serverBan.ServerId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(serverBan =>
                serverBan.User)
            .WithMany()
            .HasForeignKey(serverBan =>
                serverBan.UserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
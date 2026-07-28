using Discord.Core.Entities.Servers;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Discord.Infrastructure.Data.Configurations;

public class ServerInviteConfiguration: IEntityTypeConfiguration<ServerInvite>
{
    public void Configure(EntityTypeBuilder<ServerInvite> builder)
    {
        builder.ToTable("ServerInvites");

        builder.HasKey(invite => invite.Id);

        builder.Property(invite => invite.Code)
            .IsRequired()
            .HasMaxLength(32);

        builder.HasIndex(invite => invite.Code)
            .IsUnique();

        builder.Property(invite => invite.Uses)
            .HasDefaultValue(0);

        builder.Property(invite => invite.IsRevoked)
            .HasDefaultValue(false);

        builder.HasOne(invite => invite.Server)
            .WithMany(server => server.Invites)
            .HasForeignKey(invite => invite.ServerId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(invite => invite.CreatedByUser)
            .WithMany(user => user.CreatedServerInvites)
            .HasForeignKey(invite => invite.CreatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
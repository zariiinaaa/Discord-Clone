using Discord.Core.Entities.Servers;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Discord.Infrastructure.Data.Configurations;

public class ServerRoleConfiguration : IEntityTypeConfiguration<ServerRole>
{
    public void Configure(EntityTypeBuilder<ServerRole> builder)
    {
        builder.ToTable("ServerRoles");

        builder.HasKey(role =>
            role.Id);

        builder.Property(role =>
                role.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(role =>
                role.ColorHex)
            .HasMaxLength(7);

        builder.Property(role =>
                role.Position)
            .IsRequired();

        builder.Property(role =>
                role.IsDefault)
            .HasDefaultValue(false);

        builder.Property(role =>
                role.IsDisplayedSeparately)
            .HasDefaultValue(false);

        builder.Property(role =>
                role.IsMentionable)
            .HasDefaultValue(false);

        builder.HasIndex(role => new
        {
            role.ServerId,
            role.Position
        });

        builder.HasIndex(role =>
                role.ServerId)
            .IsUnique()
            .HasFilter("[IsDefault] = 1");

        builder.HasOne(role =>
                role.Server)
            .WithMany(server =>
                server.Roles)
            .HasForeignKey(role =>
                role.ServerId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
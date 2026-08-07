using Discord.Core.Entities.Servers;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Discord.Infrastructure.Data.Configurations;

public class ServerRolePermissionConfiguration: IEntityTypeConfiguration<ServerRolePermission>
{
    public void Configure(
        EntityTypeBuilder<ServerRolePermission> builder)
    {
        builder.ToTable(
            "ServerRolePermissions");

        builder.HasKey(rolePermission =>
            rolePermission.Id);

        builder.Property(rolePermission =>
                rolePermission.Permission)
            .IsRequired();

        builder.HasIndex(rolePermission => new
        {
            rolePermission.ServerRoleId,
            rolePermission.Permission
        })
            .IsUnique();

        builder.HasOne(rolePermission =>
                rolePermission.ServerRole)
            .WithMany(role =>
                role.Permissions)
            .HasForeignKey(rolePermission =>
                rolePermission.ServerRoleId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
using Discord.Core.Entities.Servers;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Discord.Infrastructure.Data.Configurations;

public class ServerMemberRoleConfiguration
    : IEntityTypeConfiguration<ServerMemberRole>
{
    public void Configure(
        EntityTypeBuilder<ServerMemberRole> builder)
    {
        builder.ToTable(
            "ServerMemberRoles");

        builder.HasKey(memberRole =>
            memberRole.Id);

        builder.HasIndex(memberRole => new
        {
            memberRole.ServerMemberId,
            memberRole.ServerRoleId
        })
            .IsUnique();

        builder.HasOne(memberRole =>
                memberRole.ServerMember)
            .WithMany(member =>
                member.MemberRoles)
            .HasForeignKey(memberRole =>
                memberRole.ServerMemberId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(memberRole =>
                memberRole.ServerRole)
            .WithMany(role =>
                role.MemberRoles)
            .HasForeignKey(memberRole =>
                memberRole.ServerRoleId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
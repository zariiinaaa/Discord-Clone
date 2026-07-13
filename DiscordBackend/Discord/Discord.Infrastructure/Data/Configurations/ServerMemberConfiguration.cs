using Discord.Core.Entities.Servers;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Text;

namespace Discord.Infrastructure.Data.Configurations
{
    public class ServerMemberConfiguration : IEntityTypeConfiguration<ServerMember>
    {
        public void Configure(
       EntityTypeBuilder<ServerMember> builder)
        {
            builder.ToTable("ServerMembers");

            builder.HasKey(member => member.Id);

            builder.Property(member => member.Nickname)
                .HasMaxLength(100);

            builder.Property(member => member.IsMuted)
                .HasDefaultValue(false);

            builder.Property(member => member.IsDeafened)
                .HasDefaultValue(false);

            builder.HasIndex(member => new
            {
                member.ServerId,
                member.UserId
            }).IsUnique();

            builder.HasOne(member => member.Server)
                .WithMany(server => server.Members)
                .HasForeignKey(member => member.ServerId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(member => member.User)
                .WithMany(user => user.ServerMemberships)
                .HasForeignKey(member => member.UserId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}

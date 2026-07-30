using Discord.Core.Entities.Friends;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Discord.Infrastructure.Data.Configurations
{
    public class UserBlockConfiguration: IEntityTypeConfiguration<UserBlock>
    {
        public void Configure(
            EntityTypeBuilder<UserBlock> builder)
        {
            builder.ToTable(
                "UserBlocks",
                table => table.HasCheckConstraint(
                    "CK_UserBlocks_DifferentUsers",
                    "[BlockerId] <> [BlockedUserId]"));

            builder.HasKey(userBlock => userBlock.Id);

            builder.HasIndex(userBlock => new
            {
                userBlock.BlockerId,
                userBlock.BlockedUserId
            })
                .IsUnique();

            builder.HasIndex(userBlock =>
                userBlock.BlockedUserId);

            builder.HasOne(userBlock => userBlock.Blocker)
                .WithMany(user => user.BlockedUsers)
                .HasForeignKey(userBlock => userBlock.BlockerId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(userBlock => userBlock.BlockedUser)
                .WithMany(user => user.BlockedByUsers)
                .HasForeignKey(userBlock => userBlock.BlockedUserId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
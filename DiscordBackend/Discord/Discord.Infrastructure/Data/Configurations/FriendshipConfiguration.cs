using Discord.Core.Entities.Friends;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Discord.Infrastructure.Data.Configurations
{
    public class FriendshipConfiguration : IEntityTypeConfiguration<Friendship>
    {
        public void Configure(
            EntityTypeBuilder<Friendship> builder)
        {
            builder.ToTable(
                "Friendships",
                table => table.HasCheckConstraint(
                    "CK_Friendships_UserOrder",
                    "[UserId] < [FriendId]"));

            builder.HasKey(friendship => friendship.Id);

            builder.HasIndex(friendship => new
            {
                friendship.UserId,
                friendship.FriendId
            })
                .IsUnique();

            builder.HasIndex(friendship =>
                friendship.FriendId);

            builder.HasOne(friendship => friendship.User)
                .WithMany(user => user.FriendshipsAsUser)
                .HasForeignKey(friendship => friendship.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(friendship => friendship.Friend)
                .WithMany(user => user.FriendshipsAsFriend)
                .HasForeignKey(friendship => friendship.FriendId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
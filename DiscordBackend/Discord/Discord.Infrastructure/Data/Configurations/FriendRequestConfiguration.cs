using Discord.Core.Entities.Friends;
using Discord.Core.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Discord.Infrastructure.Data.Configurations
{
    public class FriendRequestConfiguration: IEntityTypeConfiguration<FriendRequest>
    {
        public void Configure(EntityTypeBuilder<FriendRequest> builder)
        {
            builder.ToTable(
                "FriendRequests",
                table => table.HasCheckConstraint(
                    "CK_FriendRequests_DifferentUsers",
                    "[SenderId] <> [ReceiverId]"));

            builder.HasKey(request => request.Id);

            builder.Property(request => request.Status)
                .HasConversion<int>()
                .HasDefaultValue(FriendRequestStatus.Pending);

            builder.HasIndex(request => new
            {
                request.ReceiverId,
                request.Status
            });

            builder.HasIndex(request => new
            {
                request.SenderId,
                request.Status
            });

            builder.HasIndex(request => new
            {
                request.SenderId,
                request.ReceiverId
            })
                .IsUnique()
                .HasFilter("[Status] = 0");

            builder.HasOne(request => request.Sender)
                .WithMany(user => user.SentFriendRequests)
                .HasForeignKey(request => request.SenderId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(request => request.Receiver)
                .WithMany(user => user.ReceivedFriendRequests)
                .HasForeignKey(request => request.ReceiverId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
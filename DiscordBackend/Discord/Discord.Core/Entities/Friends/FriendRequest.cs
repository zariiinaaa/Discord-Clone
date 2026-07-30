using Discord.Core.Entities.Common;
using Discord.Core.Enums;

namespace Discord.Core.Entities.Friends
{
    public class FriendRequest : BaseEntity
    {
        public int SenderId { get; set; }
        public int ReceiverId { get; set; }
        public FriendRequestStatus Status { get; set; }= FriendRequestStatus.Pending;
        public DateTime? RespondedAt { get; set; }
        public User Sender { get; set; } = null!;
        public User Receiver { get; set; } = null!;
    }
}
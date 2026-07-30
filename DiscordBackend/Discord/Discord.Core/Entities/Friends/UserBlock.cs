using Discord.Core.Entities.Common;

namespace Discord.Core.Entities.Friends
{
    public class UserBlock : BaseEntity
    {
        public int BlockerId { get; set; }
        public int BlockedUserId { get; set; }
        public User Blocker { get; set; } = null!;
        public User BlockedUser { get; set; } = null!;
    }
}
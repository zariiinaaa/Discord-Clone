using Discord.Core.Entities.Common;

namespace Discord.Core.Entities.Friends
{
    public class Friendship : BaseEntity
    {
        public int UserId { get; set; }
        public int FriendId { get; set; }
        public User User { get; set; } = null!;
        public User Friend { get; set; } = null!;
    }
}
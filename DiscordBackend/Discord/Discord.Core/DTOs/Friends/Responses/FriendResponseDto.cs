using Discord.Core.Enums;

namespace Discord.Core.DTOs.Friends.Responses
{
    public class FriendResponseDto
    {
        public int Id { get; set; }
        public string Username { get; set; }= string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string? AvatarUrl { get; set; }
        public UserStatus Status { get; set; }
        public DateTime FriendsSince { get; set; }
    }
}
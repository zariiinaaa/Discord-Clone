using Discord.Core.Enums;

namespace Discord.Core.DTOs.Admin.Responses;

public class AdminUserDetailsResponseDto
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? AvatarUrl { get; set; }
    public string? Bio { get; set; }
    public UserStatus Status { get; set; }
    public UserStatus PreferredStatus { get; set; }
    public PlatformRole Role { get; set; }
    public bool IsBanned { get; set; }
    public string? BanReason { get; set; }
    public DateTime? BannedUntil { get; set; }
    public DateTime? LastSeenAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public int OwnedServerCount { get; set; }
    public int ServerMembershipCount { get; set; }
    public int MessageCount { get; set; }
    public int FriendshipCount { get; set; }
}

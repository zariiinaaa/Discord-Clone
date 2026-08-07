namespace Discord.Core.DTOs.Admin.Responses;

public class AdminServerDetailsResponseDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? IconUrl { get; set; }
    public string? BannerUrl { get; set; }
    public bool IsPublic { get; set; }
    public int OwnerId { get; set; }
    public string OwnerUsername { get; set; } = string.Empty;
    public string OwnerDisplayName { get; set; } = string.Empty;
    public string OwnerEmail { get; set; } = string.Empty;
    public int MemberCount { get; set; }
    public int ChannelCount { get; set; }
    public int MessageCount { get; set; }
    public int InviteCount { get; set; }
    public int RoleCount { get; set; }
    public int ServerBanCount { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

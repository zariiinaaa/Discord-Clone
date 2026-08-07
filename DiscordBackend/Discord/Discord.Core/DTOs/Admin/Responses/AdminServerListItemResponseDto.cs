namespace Discord.Core.DTOs.Admin.Responses;

public class AdminServerListItemResponseDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? IconUrl { get; set; }
    public bool IsPublic { get; set; }
    public int OwnerId { get; set; }
    public string OwnerUsername { get; set; } = string.Empty;
    public string OwnerDisplayName { get; set; } = string.Empty;
    public int MemberCount { get; set; }
    public int ChannelCount { get; set; }
    public int MessageCount { get; set; }
    public DateTime CreatedAt { get; set; }
}

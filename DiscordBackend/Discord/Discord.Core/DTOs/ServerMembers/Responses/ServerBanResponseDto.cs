namespace Discord.Core.DTOs.ServerMembers.Responses;

public class ServerBanResponseDto
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string? AvatarUrl { get; set; }
    public int BannedByUserId { get; set; }
    public string? Reason { get; set; }
    public DateTime CreatedAt { get; set; }
}
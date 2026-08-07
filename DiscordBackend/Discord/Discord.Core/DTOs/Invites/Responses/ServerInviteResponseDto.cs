namespace Discord.Core.DTOs.Invites.Responses;

public class ServerInviteResponseDto
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public int ServerId { get; set; }
    public string ServerName { get; set; } = string.Empty;
    public int ChannelId { get; set; }
    public string ChannelName { get; set; } = string.Empty;
    public int CreatedByUserId { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public int? MaxUses { get; set; }
    public int Uses { get; set; }
    public bool IsRevoked { get; set; }
    public DateTime CreatedAt { get; set; }
}
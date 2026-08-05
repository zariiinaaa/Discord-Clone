namespace Discord.Core.DTOs.Invites.Responses;

public class ServerInviteDetailsResponseDto
{
    public string Code { get; set; } =string.Empty;
    public int ServerId { get; set; }
    public string ServerName { get; set; } = string.Empty;
    public string? ServerDescription { get; set; }
    public string? ServerIconUrl { get; set; }
    public int MemberCount { get; set; }
    public string CreatedByDisplayName{ get; set; } = string.Empty;
    public DateTime? ExpiresAt { get; set; }
    public int? MaxUses { get; set; }
    public int Uses { get; set; }
    public bool IsAlreadyMember { get; set; }
}
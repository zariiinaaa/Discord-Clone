namespace Discord.Core.DTOs.Invites.Requests;

public class CreateServerInviteRequestDto
{
    public int ChannelId { get; set; }
    public int? ExpirationHours { get; set; } = 24;
    public int? MaxUses { get; set; }
}
namespace Discord.Core.DTOs.Invites.Requests;

public class CreateServerInviteRequestDto
{
    public int? ExpirationHours { get; set; } = 24;
    public int? MaxUses { get; set; }
}
using Discord.Core.Enums;

namespace Discord.Core.DTOs.ServerRoles.Requests;

public class UpdateServerRoleRequestDto
{
    public string Name { get; set; }= string.Empty;
    public string? ColorHex { get; set; }
    public bool IsDisplayedSeparately { get; set; }
    public bool IsMentionable { get; set; }
    public List<ServerPermission> Permissions { get; set; }= new List<ServerPermission>();
}
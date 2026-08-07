using Discord.Core.Enums;

namespace Discord.Core.DTOs.ServerRoles.Responses;

public class ServerRoleResponseDto
{
    public int Id { get; set; }
    public string Name { get; set; }= string.Empty;
    public string? ColorHex { get; set; }
    public int Position { get; set; }
    public bool IsDefault { get; set; }
    public bool IsDisplayedSeparately { get; set; }
    public bool IsMentionable { get; set; }
    public int ServerId { get; set; }

    public IReadOnlyCollection<ServerPermission>Permissions { get; set; } = Array.Empty<ServerPermission>();
}
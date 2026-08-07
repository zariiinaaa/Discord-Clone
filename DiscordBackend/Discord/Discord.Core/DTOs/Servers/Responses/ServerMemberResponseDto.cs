using Discord.Core.DTOs.ServerRoles.Responses;
using Discord.Core.Enums;

namespace Discord.Core.DTOs.ServerMembers.Responses;

public class ServerMemberResponseDto
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string? AvatarUrl { get; set; }
    public string? Nickname { get; set; }
    public UserStatus Status { get; set; }
    public bool IsOwner { get; set; }
    public bool IsMuted { get; set; }
    public bool IsDeafened { get; set; }
    public DateTime? TimedOutUntil { get; set; }
    public IReadOnlyCollection<ServerRoleResponseDto> Roles { get; set; }= Array.Empty<ServerRoleResponseDto>();
    public DateTime JoinedAt { get; set; }
}
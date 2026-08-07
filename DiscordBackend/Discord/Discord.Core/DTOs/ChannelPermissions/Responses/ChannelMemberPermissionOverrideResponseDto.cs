using Discord.Core.Enums;

namespace Discord.Core.DTOs.ChannelPermissions.Responses;

public class ChannelMemberPermissionOverrideResponseDto
{
    public int UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public ServerPermission Permission { get; set; }
    public PermissionOverrideType OverrideType { get; set; }
}
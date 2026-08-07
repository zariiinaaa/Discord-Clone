using Discord.Core.Enums;

namespace Discord.Core.DTOs.ChannelPermissions.Requests;

public class ChannelPermissionOverrideItemRequestDto
{
    public ServerPermission Permission { get; set; }
    public PermissionOverrideType OverrideType { get; set; }
}
using Discord.Core.Entities.Common;
using Discord.Core.Enums;

namespace Discord.Core.Entities.Servers;

public class ChannelRolePermissionOverride : BaseEntity
{
    public int ChannelId { get; set; }
    public Channel Channel { get; set; } = null!;

    public int RoleId { get; set; }
    public ServerRole Role { get; set; } = null!;

    public ServerPermission Permission { get; set; }
    public PermissionOverrideType OverrideType { get; set; }
}
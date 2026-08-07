using Discord.Core.Entities.Common;
using Discord.Core.Enums;

namespace Discord.Core.Entities.Servers;

public class ChannelMemberPermissionOverride : BaseEntity
{
    public int ChannelId { get; set; }
    public Channel Channel { get; set; } = null!;

    public int UserId { get; set; }
    public User User { get; set; } = null!;

    public ServerPermission Permission { get; set; }
    public PermissionOverrideType OverrideType { get; set; }
}
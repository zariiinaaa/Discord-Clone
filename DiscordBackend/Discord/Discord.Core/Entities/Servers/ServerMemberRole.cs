using Discord.Core.Entities.Common;

namespace Discord.Core.Entities.Servers;

public class ServerMemberRole : BaseEntity
{
    public int ServerMemberId { get; set; }
    public ServerMember ServerMember { get; set; }= null!;
    public int ServerRoleId { get; set; }
    public ServerRole ServerRole { get; set; } = null!;
}
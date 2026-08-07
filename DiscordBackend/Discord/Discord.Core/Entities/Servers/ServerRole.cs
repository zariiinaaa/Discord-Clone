using Discord.Core.Entities.Common;

namespace Discord.Core.Entities.Servers;

public class ServerRole : BaseEntity
{
    public string Name { get; set; } =string.Empty;

    public string? ColorHex { get; set; }

    public int Position { get; set; }
    public bool IsDefault { get; set; }
    public bool IsDisplayedSeparately { get; set; }
    public bool IsMentionable { get; set; }

    public int ServerId { get; set; }
    public Server Server { get; set; } =null!;
    public ICollection<ServerRolePermission> Permissions { get; set; }= new List<ServerRolePermission>();

    public ICollection<ServerMemberRole> MemberRoles { get; set; }
        = new List<ServerMemberRole>();
}
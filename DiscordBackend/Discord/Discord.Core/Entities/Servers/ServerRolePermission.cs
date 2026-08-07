using Discord.Core.Entities.Common;
using Discord.Core.Enums;

namespace Discord.Core.Entities.Servers;

public class ServerRolePermission : BaseEntity
{
    public int ServerRoleId { get; set; }
    public ServerRole ServerRole { get; set; }= null!;
    public ServerPermission Permission { get; set; }
}
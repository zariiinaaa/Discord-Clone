using Discord.Core.Entities.Common;

namespace Discord.Core.Entities.Servers;

public class ServerBan : BaseEntity
{
    public int ServerId { get; set; }
    public Server Server { get; set; } = null!;
    public int UserId { get; set; }
    public User User { get; set; } = null!;
    public int BannedByUserId { get; set; }
    public string? Reason { get; set; }
}
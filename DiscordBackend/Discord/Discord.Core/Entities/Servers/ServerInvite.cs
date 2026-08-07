using Discord.Core.Entities.Common;

namespace Discord.Core.Entities.Servers;

public class ServerInvite : BaseEntity
{
    public string Code { get; set; } = string.Empty;
    public DateTime? ExpiresAt { get; set; }
    public int? MaxUses { get; set; }
    public int Uses { get; set; }
    public bool IsRevoked { get; set; }
    public int ServerId { get; set; }
    public Server Server { get; set; } = null!;
    public int ChannelId { get; set; }
    public Channel Channel { get; set; } = null!;
    public int CreatedByUserId { get; set; }
    public User CreatedByUser { get; set; } = null!;
}
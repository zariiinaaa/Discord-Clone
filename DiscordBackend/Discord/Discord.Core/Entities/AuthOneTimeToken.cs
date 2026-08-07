using Discord.Core.Entities.Common;
using Discord.Core.Enums;

namespace Discord.Core.Entities;

public class AuthOneTimeToken : BaseEntity
{
    public int UserId { get; set; }
    public User User { get; set; } = null!;
    public AuthTokenPurpose Purpose { get; set; }
    public string TokenHash { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public DateTime? UsedAt { get; set; }
    public string? CreatedByIp { get; set; }
}

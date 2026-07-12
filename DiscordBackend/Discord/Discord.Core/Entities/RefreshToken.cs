using Discord.Core.Entities.Common;
using System;
using System.Collections.Generic;
using System.Text;

namespace Discord.Core.Entities
{
    public class RefreshToken:BaseEntity
    {
        public string TokenHash { get; set; } = string.Empty;
        public DateTime ExpiresAt { get; set; }
        public DateTime? RevokedAt { get; set; }
        public string? ReplacedByTokenHash { get; set; }
        public string? CreatedByIp { get; set; }
        public int UserId { get; set; }
        public User User { get; set; } = null!;
    }
}

using Discord.Core.Entities.Common;
using Discord.Core.Enums;
using Discord.Core.Entities.Servers;
using System;
using System.Collections.Generic;
using System.Text;

namespace Discord.Core.Entities
{
    public class User:BaseEntity
    {
        public string Username { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public string? AvatarUrl { get; set; }
        public string? Bio { get; set; }
        public UserStatus Status { get; set; } = UserStatus.Offline;
        public PlatformRole Role { get; set; } = PlatformRole.User;
        public DateTime? LastSeenAt { get; set; }
        public bool IsBanned { get; set; }
        public string? BanReason { get; set; }
        public DateTime? BannedUntil { get; set; }
        public ICollection<RefreshToken> RefreshTokens { get; set; }
            = new List<RefreshToken>();
        public ICollection<Server> OwnedServers { get; set; }
            = new List<Server>();
        public ICollection<ServerMember> ServerMemberships { get; set; }
            = new List<ServerMember>();
    }
}

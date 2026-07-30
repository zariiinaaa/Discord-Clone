using Discord.Core.Entities.Common;
using System;
using System.Collections.Generic;
using System.Text;

namespace Discord.Core.Entities.Servers
{
    public class ServerMember:BaseEntity
    {
        public string? Nickname { get; set; }
        public bool IsMuted { get; set; }
        public bool IsDeafened { get; set; }
        public DateTime? TimedOutUntil { get; set; }
        public int ServerId { get; set; }
        public Server Server { get; set; } = null!;
        public int UserId { get; set; }
        public User User { get; set; } = null!;
        public bool? AllowDirectMessages { get; set; }
        public bool? EnableMessageRequests { get; set; }
    }
}

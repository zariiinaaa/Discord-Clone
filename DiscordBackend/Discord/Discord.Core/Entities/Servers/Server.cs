using Discord.Core.Entities.Common;
using System;
using System.Collections.Generic;
using System.Text;

namespace Discord.Core.Entities.Servers
{
    public class Server:BaseEntity
    {
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? IconUrl { get; set; }
        public string? BannerUrl { get; set; }
        public bool IsPublic { get; set; }
        public int OwnerId { get; set; }
        public User Owner { get; set; } = null!;
        public ICollection<ServerMember> Members { get; set; }
            = new List<ServerMember>();
        public ICollection<Channel> Channels { get; set; }
            = new List<Channel>();
    }
}

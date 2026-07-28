using Discord.Core.Entities.Common;
using Discord.Core.Entities.Messages;
using Discord.Core.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace Discord.Core.Entities.Servers
{
    public class Channel:BaseEntity
    {
        public string Name { get; set; } = string.Empty;
        public string? Topic { get; set; }
        public ChannelType Type { get; set; }
        public int Position { get; set; }
        public bool IsPrivate { get; set; }
        public int? Bitrate { get; set; }
        public int? UserLimit { get; set; }
        public int ServerId { get; set; }
        public Server Server { get; set; } = null!;
        public int? ParentCategoryId { get; set; }
        public Channel? ParentCategory { get; set; }
        public ICollection<Channel> ChildChannels { get; set; }= new List<Channel>();
        public ICollection<Message> Messages { get; set; }= new List<Message>();
    }
}

using Discord.Core.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace Discord.Core.DTOs.Channels.Requests
{
    public class CreateChannelRequestDto
    {
        public string Name { get; set; } = string.Empty;
        public string? Topic { get; set; }
        public ChannelType Type { get; set; } = ChannelType.Text;
        public bool IsPrivate { get; set; }
        public int? ParentCategoryId { get; set; }
        public int? Bitrate { get; set; }
        public int? UserLimit { get; set; }
    }
}

using Discord.Core.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace Discord.Core.DTOs.Channels.Responses
{
    public class ChannelResponseDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Topic { get; set; }
        public ChannelType Type { get; set; }
        public int Position { get; set; }
        public bool IsPrivate { get; set; }
        public int? Bitrate { get; set; }
        public int? UserLimit { get; set; }
        public int? ParentCategoryId { get; set; }
    }
}

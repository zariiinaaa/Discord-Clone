using Discord.Core.DTOs.Channels.Responses;
using System;
using System.Collections.Generic;
using System.Text;

namespace Discord.Core.DTOs.Servers.Responses
{
    public class ServerDetailsResponseDto : ServerResponseDto
    {
        public int MemberCount { get; set; }

        public IReadOnlyList<ChannelResponseDto> Channels { get; set; }
            = new List<ChannelResponseDto>();
    }
}

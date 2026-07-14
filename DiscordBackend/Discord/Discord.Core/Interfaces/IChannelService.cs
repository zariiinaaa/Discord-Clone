using Discord.Core.DTOs.Channels.Requests;
using Discord.Core.DTOs.Channels.Responses;
using System;
using System.Collections.Generic;
using System.Text;

namespace Discord.Core.Interfaces
{
    public interface IChannelService
    {
        Task<ChannelResponseDto> CreateAsync(int serverId,int userId,CreateChannelRequestDto request,CancellationToken cancellationToken = default);
    
        
        Task<ChannelResponseDto> UpdateAsync(int serverId,int channelId,int userId,UpdateChannelRequestDto request,CancellationToken cancellationToken = default);
        Task DeleteAsync(int serverId,int channelId, int userId,CancellationToken cancellationToken = default);
    }

}

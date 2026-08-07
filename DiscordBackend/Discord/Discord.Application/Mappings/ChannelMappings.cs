using Discord.Core.DTOs.Channels.Responses;
using Discord.Core.Entities.Servers;

namespace Discord.Application.Mappings;

public static class ChannelMappings
{
    public static ChannelResponseDto ToResponseDto(this Channel channel)
    {
        ArgumentNullException.ThrowIfNull(channel);

        return new ChannelResponseDto
        {
            Id = channel.Id,
            Name = channel.Name,
            Topic = channel.Topic,
            Type = channel.Type,
            Position = channel.Position,
            IsPrivate = channel.IsPrivate,
            IsPermissionSynced = channel.IsPermissionSynced,
            Bitrate = channel.Bitrate,
            UserLimit = channel.UserLimit,
            ParentCategoryId = channel.ParentCategoryId
        };
    }
}
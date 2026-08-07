using Discord.Core.DTOs.Invites.Responses;
using Discord.Core.Entities.Servers;

namespace Discord.Application.Mappings;

public static class ServerInviteMappings
{
    public static ServerInviteResponseDto ToResponseDto(
        this ServerInvite invite, string serverName, string channelName = "")
    {
        return new ServerInviteResponseDto
        {
            Id = invite.Id,
            Code = invite.Code,
            ServerId = invite.ServerId,
            ServerName = serverName,
            ChannelId = invite.ChannelId,
            ChannelName = channelName,
            CreatedByUserId = invite.CreatedByUserId,
            ExpiresAt = invite.ExpiresAt,
            MaxUses = invite.MaxUses,
            Uses = invite.Uses,
            IsRevoked = invite.IsRevoked,
            CreatedAt = invite.CreatedAt
        };
    }
}
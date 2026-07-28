using Discord.Core.DTOs.ServerMembers.Responses;
using Discord.Core.Entities.Servers;

namespace Discord.Application.Mappings;

public static class ServerMemberMappings
{
    public static ServerMemberResponseDto ToResponseDto(this ServerMember member, int ownerId)
    {
        return new ServerMemberResponseDto
        {
            Id = member.Id,
            UserId = member.UserId,
            Username = member.User.Username,
            DisplayName = member.User.DisplayName,
            AvatarUrl = member.User.AvatarUrl,
            Nickname = member.Nickname,
            Status = member.User.Status,
            IsOwner = member.UserId == ownerId,
            IsMuted = member.IsMuted,
            IsDeafened = member.IsDeafened,
            TimedOutUntil = member.TimedOutUntil,
            JoinedAt = member.CreatedAt
        };
    }
}
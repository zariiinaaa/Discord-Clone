using Discord.Core.DTOs.Invites.Requests;
using Discord.Core.DTOs.Invites.Responses;

namespace Discord.Core.Interfaces;

public interface IServerInviteService
{
    Task<ServerInviteResponseDto> CreateAsync(int serverId, int userId,
        CreateServerInviteRequestDto request,
        CancellationToken cancellationToken = default);

    Task JoinAsync(string code,int userId,CancellationToken cancellationToken = default);
}
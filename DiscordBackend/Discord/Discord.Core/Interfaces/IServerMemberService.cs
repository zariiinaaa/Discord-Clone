using Discord.Core.DTOs.ServerMembers.Responses;

namespace Discord.Core.Interfaces;

public interface IServerMemberService
{
    Task<IReadOnlyCollection<ServerMemberResponseDto>>
        GetMembersAsync(int serverId,int userId,CancellationToken cancellationToken = default);

    Task LeaveAsync(int serverId,int userId,CancellationToken cancellationToken = default);

    Task KickAsync( int serverId, int memberUserId, int currentUserId,CancellationToken cancellationToken = default);

    Task BanAsync(int serverId,int memberUserId, int currentUserId, string? reason,
    CancellationToken cancellationToken = default);

    Task UnbanAsync( int serverId,int bannedUserId, int currentUserId,CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<ServerBanResponseDto>>GetBansAsync(int serverId, int currentUserId,
    CancellationToken cancellationToken = default);


}
using Discord.Core.DTOs.ServerMembers.Responses;

namespace Discord.Core.Interfaces;

public interface IServerMemberService
{
    Task<IReadOnlyCollection<ServerMemberResponseDto>>
        GetMembersAsync(int serverId,int userId,CancellationToken cancellationToken = default);

    Task LeaveAsync(int serverId,int userId,CancellationToken cancellationToken = default);

    Task KickAsync( int serverId, int memberUserId, int currentUserId,CancellationToken cancellationToken = default);

    
}
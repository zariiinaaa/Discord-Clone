using Discord.Core.DTOs.ServerRoles.Requests;
using Discord.Core.DTOs.ServerRoles.Responses;

namespace Discord.Core.Interfaces;

public interface IServerRoleService
{
    Task<IReadOnlyCollection<ServerRoleResponseDto>>GetRolesAsync(int serverId,int userId,
     CancellationToken cancellationToken = default);

    Task<ServerRoleResponseDto> CreateRoleAsync(int serverId,int userId,CreateServerRoleRequestDto request,
    CancellationToken cancellationToken = default);

    Task<ServerRoleResponseDto> UpdateRoleAsync(int serverId, int roleId,int userId,
      UpdateServerRoleRequestDto request,CancellationToken cancellationToken = default);

    Task DeleteRoleAsync(int serverId,int roleId,int userId,CancellationToken cancellationToken = default);

    Task AssignRoleAsync( int serverId, int memberUserId,int roleId,int userId,
        CancellationToken cancellationToken = default);

    Task RemoveRoleAsync(int serverId,int memberUserId,int roleId,int userId,
    CancellationToken cancellationToken = default);
}
using Discord.Core.DTOs.Servers.Requests;
using Discord.Core.DTOs.Servers.Responses;
using System;
using System.Collections.Generic;
using System.Text;

namespace Discord.Core.Interfaces
{
    public interface IServerService
    {
        Task<ServerResponseDto> CreateAsync(int ownerId,CreateServerRequestDto request,CancellationToken cancellationToken = default);
        Task<IReadOnlyList<ServerResponseDto>> GetMyServersAsync(int userId,CancellationToken cancellationToken = default);

        Task<ServerDetailsResponseDto> GetByIdAsync(int serverId,int userId, CancellationToken cancellationToken = default);

        Task<ServerResponseDto> UpdateAsync(int serverId,int userId,UpdateServerRequestDto request,CancellationToken cancellationToken = default);

        Task DeleteAsync(int serverId,int userId,CancellationToken cancellationToken = default);

        Task<ServerResponseDto> UpdateIconAsync(int serverId,int userId,string iconUrl,
        CancellationToken cancellationToken = default);
    }
}

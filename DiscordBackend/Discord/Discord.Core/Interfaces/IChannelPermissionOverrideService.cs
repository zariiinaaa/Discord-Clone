using Discord.Core.DTOs.ChannelPermissions.Requests;
using Discord.Core.DTOs.ChannelPermissions.Responses;

namespace Discord.Core.Interfaces;

public interface IChannelPermissionOverrideService
{
    Task<ChannelPermissionOverridesResponseDto> GetAsync(int channelId,int userId,
     CancellationToken cancellationToken = default);

    Task UpdateRoleAsync( int channelId,int roleId, int userId,
        UpdateChannelPermissionOverridesRequestDto request,
        CancellationToken cancellationToken = default);

    Task DeleteRoleAsync(int channelId,int roleId,
        int userId,
        CancellationToken cancellationToken = default);

    Task UpdateMemberAsync(int channelId, int memberUserId,int userId,UpdateChannelPermissionOverridesRequestDto request,
        CancellationToken cancellationToken = default);

    Task DeleteMemberAsync(int channelId,int memberUserId,int userId,
        CancellationToken cancellationToken = default);
    Task SyncWithCategoryAsync(int channelId, int userId,CancellationToken cancellationToken = default);
}
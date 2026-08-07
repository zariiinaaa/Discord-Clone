using Discord.Core.Enums;

namespace Discord.Core.Interfaces;

public interface IChannelPermissionService
{
    Task<IReadOnlyCollection<ServerPermission>> GetPermissionsAsync(int channelId,int userId,
        CancellationToken cancellationToken = default);

    Task<bool> HasPermissionAsync(int channelId,int userId,ServerPermission permission,
        CancellationToken cancellationToken = default);

    Task EnsurePermissionAsync(int channelId,int userId, ServerPermission permission,
        CancellationToken cancellationToken = default);
}
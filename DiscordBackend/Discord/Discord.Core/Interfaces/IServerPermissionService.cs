using Discord.Core.Enums;

namespace Discord.Core.Interfaces;

public interface IServerPermissionService
{
    Task<IReadOnlyCollection<ServerPermission>>GetPermissionsAsync(int serverId,int userId,
    CancellationToken cancellationToken = default);

    Task<bool> HasPermissionAsync(int serverId,int userId,ServerPermission permission,
     CancellationToken cancellationToken = default);

    Task EnsurePermissionAsync(int serverId,int userId,ServerPermission permission,
    CancellationToken cancellationToken = default);
}
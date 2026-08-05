using Discord.Core.Enums;

namespace Discord.Core.Interfaces;

public interface IUserPresenceService
{
    Task<UserStatus> MarkConnectedAsync(int userId,CancellationToken cancellationToken = default);
    Task<UserStatus> MarkDisconnectedAsync(int userId, CancellationToken cancellationToken = default);
    Task ResetAllUsersToOfflineAsync(CancellationToken cancellationToken = default);
    Task<UserStatus> ChangeStatusAsync(int userId,UserStatus status,
    CancellationToken cancellationToken = default);
}
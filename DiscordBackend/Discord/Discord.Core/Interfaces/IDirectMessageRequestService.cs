using Discord.Core.DTOs.MessageRequests.Responses;

namespace Discord.Core.Interfaces;

public interface IDirectMessageRequestService
{
    Task<IReadOnlyCollection<DirectMessageRequestResponseDto>>GetIncomingAsync(int userId,CancellationToken cancellationToken = default);

    Task AcceptAsync(int requestId, int userId, CancellationToken cancellationToken = default);

    Task IgnoreAsync(int requestId,int userId,CancellationToken cancellationToken = default);

    Task MarkAsSpamAsync(int requestId,int userId,CancellationToken cancellationToken = default);
}
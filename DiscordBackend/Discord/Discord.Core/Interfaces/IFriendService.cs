using System;
using System.Collections.Generic;
using System.Text;

using Discord.Core.DTOs.Friends.Requests;
using Discord.Core.DTOs.Friends.Responses;

namespace Discord.Core.Interfaces
{
    public interface IFriendService
    {
        Task<IReadOnlyCollection<FriendResponseDto>>
            GetFriendsAsync(
                int userId,
                CancellationToken cancellationToken = default);

        Task<IReadOnlyCollection<FriendRequestResponseDto>>GetIncomingRequestsAsync(int userId,
                CancellationToken cancellationToken = default);

        Task<IReadOnlyCollection<FriendRequestResponseDto>>
            GetOutgoingRequestsAsync(
                int userId,
                CancellationToken cancellationToken = default);

        Task<FriendRequestResponseDto> SendRequestAsync(int userId,SendFriendRequestDto request,
            CancellationToken cancellationToken = default);

        Task<FriendResponseDto> AcceptRequestAsync( int userId,int requestId,
            CancellationToken cancellationToken = default);

        Task RejectRequestAsync(int userId, int requestId,
            CancellationToken cancellationToken = default);

        Task CancelRequestAsync( int userId,int requestId,
            CancellationToken cancellationToken = default);

        Task RemoveFriendAsync(int userId,int friendUserId,
            CancellationToken cancellationToken = default);

        Task<IReadOnlyCollection<FriendUserResponseDto>>
            GetBlockedUsersAsync(
                int userId,
                CancellationToken cancellationToken = default);

        Task BlockUserAsync(int userId,int blockedUserId,
            CancellationToken cancellationToken = default);

        Task UnblockUserAsync( int userId,int blockedUserId,
            CancellationToken cancellationToken = default);
    }
}
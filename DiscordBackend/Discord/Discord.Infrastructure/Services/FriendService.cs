using Discord.Application.Mappings;
using Discord.Core.DTOs.Friends.Requests;
using Discord.Core.DTOs.Friends.Responses;
using Discord.Core.Entities.Friends;
using Discord.Core.Enums;
using Discord.Core.Exceptions;
using Discord.Core.Interfaces;
using Discord.Infrastructure.Data;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace Discord.Infrastructure.Services
{
    public class FriendService : IFriendService
    {
        private readonly AppDbContext _dbContext;

        private readonly IValidator<SendFriendRequestDto> _sendRequestValidator;

        public FriendService(AppDbContext dbContext, IValidator<SendFriendRequestDto>sendRequestValidator)
        {
            _dbContext = dbContext;
            _sendRequestValidator = sendRequestValidator;
        }

        public async Task<
            IReadOnlyCollection<FriendResponseDto>>
            GetFriendsAsync(
                int userId,
                CancellationToken cancellationToken = default)
        {
            var friendships =
                await _dbContext.Friendships
                    .AsNoTracking()
                    .Include(friendship => friendship.User)
                    .Include(friendship => friendship.Friend)
                    .Where(friendship =>
                        friendship.UserId == userId ||
                        friendship.FriendId == userId)
                    .ToListAsync(cancellationToken);

            return friendships
                .Select(friendship =>
                {
                    var friendUser =
                        friendship.UserId == userId
                            ? friendship.Friend
                            : friendship.User;

                    return friendUser.ToFriendResponseDto(
                        friendship.CreatedAt);
                })
                .OrderBy(friend =>
                    friend.DisplayName)
                .ToList();
        }

        public async Task<
            IReadOnlyCollection<FriendRequestResponseDto>>
            GetIncomingRequestsAsync(
                int userId,
                CancellationToken cancellationToken = default)
        {
            var requests =
                await _dbContext.FriendRequests
                    .AsNoTracking()
                    .Include(request => request.Sender)
                    .Include(request => request.Receiver)
                    .Where(request =>
                        request.ReceiverId == userId &&
                        request.Status ==
                            FriendRequestStatus.Pending)
                    .OrderByDescending(request =>
                        request.CreatedAt)
                    .ToListAsync(cancellationToken);

            return requests
                .Select(request =>
                    request.ToResponseDto())
                .ToList();
        }

        public async Task<
            IReadOnlyCollection<FriendRequestResponseDto>>
            GetOutgoingRequestsAsync(
                int userId,
                CancellationToken cancellationToken = default)
        {
            var requests =
                await _dbContext.FriendRequests
                    .AsNoTracking()
                    .Include(request => request.Sender)
                    .Include(request => request.Receiver)
                    .Where(request =>
                        request.SenderId == userId &&
                        request.Status ==
                            FriendRequestStatus.Pending)
                    .OrderByDescending(request =>
                        request.CreatedAt)
                    .ToListAsync(cancellationToken);

            return requests
                .Select(request =>
                    request.ToResponseDto())
                .ToList();
        }

        public async Task<FriendRequestResponseDto>
            SendRequestAsync(
                int userId,
                SendFriendRequestDto request,
                CancellationToken cancellationToken = default)
        {
            var validationResult =
                await _sendRequestValidator.ValidateAsync(
                    request,
                    cancellationToken);

            if (!validationResult.IsValid)
            {
                throw new ValidationException(
                    validationResult.Errors);
            }

            var username = request.Username.Trim();

            var sender =
                await _dbContext.Users
                    .AsNoTracking()
                    .FirstOrDefaultAsync(
                        user => user.Id == userId,
                        cancellationToken)
                ?? throw new KeyNotFoundException(
                    "Cari istifadəçi tapılmadı.");

            var receiver =
                await _dbContext.Users
                    .AsNoTracking()
                    .FirstOrDefaultAsync(
                        user => user.Username == username,
                        cancellationToken)
                ?? throw new KeyNotFoundException(
                    "İstifadəçi tapılmadı.");

            if (receiver.Id == userId)
            {
                throw new BadRequestException(
                    "Özünüzə dostluq sorğusu göndərə bilməzsiniz.");
            }

            if (await IsBlockedAsync(
                    userId,
                    receiver.Id,
                    cancellationToken))
            {
                throw new ForbiddenException(
                    "Bu istifadəçiyə dostluq sorğusu göndərmək mümkün deyil.");
            }

            var (firstUserId, secondUserId) =
                GetOrderedUserIds(
                    userId,
                    receiver.Id);

            var friendshipExists =
                await _dbContext.Friendships
                    .AsNoTracking()
                    .AnyAsync(
                        friendship =>
                            friendship.UserId ==
                                firstUserId &&
                            friendship.FriendId ==
                                secondUserId,
                        cancellationToken);

            if (friendshipExists)
            {
                throw new ConflictException(
                    "Bu istifadəçi artıq dostunuzdur.");
            }

            var pendingRequest =
                await _dbContext.FriendRequests
                    .AsNoTracking()
                    .FirstOrDefaultAsync(
                        friendRequest =>
                            friendRequest.Status ==
                                FriendRequestStatus.Pending &&
                            (
                                friendRequest.SenderId ==
                                    userId &&
                                friendRequest.ReceiverId ==
                                    receiver.Id ||
                                friendRequest.SenderId ==
                                    receiver.Id &&
                                friendRequest.ReceiverId ==
                                    userId
                            ),
                        cancellationToken);

            if (pendingRequest is not null)
            {
                if (pendingRequest.SenderId == userId)
                {
                    throw new ConflictException(
                        "Bu istifadəçiyə artıq dostluq sorğusu göndərmisiniz.");
                }

                throw new ConflictException(
                    "Bu istifadəçidən artıq gələn dostluq sorğunuz var.");
            }

            var friendRequest = new FriendRequest
            {
                SenderId = userId,
                ReceiverId = receiver.Id,
                Status = FriendRequestStatus.Pending
            };

            _dbContext.FriendRequests.Add(
                friendRequest);

            await _dbContext.SaveChangesAsync(
                cancellationToken);

            friendRequest.Sender = sender;
            friendRequest.Receiver = receiver;

            return friendRequest.ToResponseDto();
        }

        public async Task<FriendResponseDto>
            AcceptRequestAsync(
                int userId,
                int requestId,
                CancellationToken cancellationToken = default)
        {
            await using var transaction =
                await _dbContext.Database
                    .BeginTransactionAsync(
                        cancellationToken);

            var friendRequest =
                await _dbContext.FriendRequests
                    .Include(request => request.Sender)
                    .Include(request => request.Receiver)
                    .FirstOrDefaultAsync(
                        request =>
                            request.Id == requestId &&
                            request.ReceiverId == userId &&
                            request.Status ==
                                FriendRequestStatus.Pending,
                        cancellationToken)
                ?? throw new KeyNotFoundException(
                    "Gələn dostluq sorğusu tapılmadı.");

            if (await IsBlockedAsync(
                    friendRequest.SenderId,
                    friendRequest.ReceiverId,
                    cancellationToken))
            {
                throw new ForbiddenException(
                    "Bu dostluq sorğusunu qəbul etmək mümkün deyil.");
            }

            var (firstUserId, secondUserId) =
                GetOrderedUserIds(
                    friendRequest.SenderId,
                    friendRequest.ReceiverId);

            var friendshipExists =
                await _dbContext.Friendships
                    .AnyAsync(
                        friendship =>
                            friendship.UserId ==
                                firstUserId &&
                            friendship.FriendId ==
                                secondUserId,
                        cancellationToken);

            if (friendshipExists)
            {
                throw new ConflictException(
                    "Bu istifadəçi artıq dostunuzdur.");
            }

            var friendship = new Friendship
            {
                UserId = firstUserId,
                FriendId = secondUserId
            };

            friendRequest.Status =
                FriendRequestStatus.Accepted;

            friendRequest.RespondedAt =
                DateTime.UtcNow;

            friendRequest.UpdatedAt =
                DateTime.UtcNow;

            _dbContext.Friendships.Add(friendship);

            await _dbContext.SaveChangesAsync(
                cancellationToken);

            await transaction.CommitAsync(
                cancellationToken);

            return friendRequest.Sender
                .ToFriendResponseDto(
                    friendship.CreatedAt);
        }

        public async Task<int> RejectRequestAsync(
            int userId,
            int requestId,
            CancellationToken cancellationToken = default)
        {
            var friendRequest =
                await _dbContext.FriendRequests
                    .FirstOrDefaultAsync(
                        request =>
                            request.Id == requestId &&
                            request.ReceiverId == userId &&
                            request.Status ==
                                FriendRequestStatus.Pending,
                        cancellationToken)
                ?? throw new KeyNotFoundException(
                    "Gələn dostluq sorğusu tapılmadı.");

            friendRequest.Status =
                FriendRequestStatus.Rejected;

            friendRequest.RespondedAt =
                DateTime.UtcNow;

            friendRequest.UpdatedAt =
                DateTime.UtcNow;

            await _dbContext.SaveChangesAsync(
                cancellationToken);
            return friendRequest.SenderId;
        }

        public async Task<int> CancelRequestAsync(
            int userId,
            int requestId,
            CancellationToken cancellationToken = default)
        {
            var friendRequest =
                await _dbContext.FriendRequests
                    .FirstOrDefaultAsync(
                        request =>
                            request.Id == requestId &&
                            request.SenderId == userId &&
                            request.Status ==
                                FriendRequestStatus.Pending,
                        cancellationToken)
                ?? throw new KeyNotFoundException(
                    "Göndərilmiş dostluq sorğusu tapılmadı.");

            friendRequest.Status =
                FriendRequestStatus.Cancelled;

            friendRequest.RespondedAt =
                DateTime.UtcNow;

            friendRequest.UpdatedAt =
                DateTime.UtcNow;

            await _dbContext.SaveChangesAsync(
                cancellationToken);
            return friendRequest.ReceiverId;
        }

        public async Task RemoveFriendAsync(
            int userId,
            int friendUserId,
            CancellationToken cancellationToken = default)
        {
            if (userId == friendUserId)
            {
                throw new BadRequestException(
                    "İstifadəçi məlumatı düzgün deyil.");
            }

            var (firstUserId, secondUserId) =
                GetOrderedUserIds(
                    userId,
                    friendUserId);

            var friendship =
                await _dbContext.Friendships
                    .FirstOrDefaultAsync(
                        friendship =>
                            friendship.UserId ==
                                firstUserId &&
                            friendship.FriendId ==
                                secondUserId,
                        cancellationToken)
                ?? throw new KeyNotFoundException(
                    "Dostluq tapılmadı.");

            _dbContext.Friendships.Remove(
                friendship);

            await _dbContext.SaveChangesAsync(
                cancellationToken);
        }

        public async Task<
            IReadOnlyCollection<FriendUserResponseDto>>
            GetBlockedUsersAsync(
                int userId,
                CancellationToken cancellationToken = default)
        {
            var blocks =
                await _dbContext.UserBlocks
                    .AsNoTracking()
                    .Include(userBlock =>
                        userBlock.BlockedUser)
                    .Where(userBlock =>
                        userBlock.BlockerId == userId)
                    .OrderBy(userBlock =>
                        userBlock.BlockedUser.DisplayName)
                    .ToListAsync(cancellationToken);

            return blocks
                .Select(userBlock =>
                    userBlock.BlockedUser
                        .ToFriendUserResponseDto())
                .ToList();
        }

        public async Task BlockUserAsync(
            int userId,
            int blockedUserId,
            CancellationToken cancellationToken = default)
        {
            if (userId == blockedUserId)
            {
                throw new BadRequestException(
                    "Özünüzü bloklaya bilməzsiniz.");
            }

            var blockedUserExists =
                await _dbContext.Users
                    .AsNoTracking()
                    .AnyAsync(
                        user => user.Id == blockedUserId,
                        cancellationToken);

            if (!blockedUserExists)
            {
                throw new KeyNotFoundException(
                    "İstifadəçi tapılmadı.");
            }

            var blockExists =
                await _dbContext.UserBlocks
                    .AsNoTracking()
                    .AnyAsync(
                        userBlock =>
                            userBlock.BlockerId == userId &&
                            userBlock.BlockedUserId ==
                                blockedUserId,
                        cancellationToken);

            if (blockExists)
            {
                throw new ConflictException(
                    "Bu istifadəçi artıq bloklanıb.");
            }

            await using var transaction =
                await _dbContext.Database
                    .BeginTransactionAsync(
                        cancellationToken);

            var (firstUserId, secondUserId) =
                GetOrderedUserIds(
                    userId,
                    blockedUserId);

            var friendship =
                await _dbContext.Friendships
                    .FirstOrDefaultAsync(
                        friendship =>
                            friendship.UserId ==
                                firstUserId &&
                            friendship.FriendId ==
                                secondUserId,
                        cancellationToken);

            if (friendship is not null)
            {
                _dbContext.Friendships.Remove(
                    friendship);
            }

            var pendingRequests =
                await _dbContext.FriendRequests
                    .Where(request =>
                        request.Status ==
                            FriendRequestStatus.Pending &&
                        (
                            request.SenderId == userId &&
                            request.ReceiverId ==
                                blockedUserId ||
                            request.SenderId ==
                                blockedUserId &&
                            request.ReceiverId == userId
                        ))
                    .ToListAsync(cancellationToken);

            foreach (var pendingRequest
                     in pendingRequests)
            {
                pendingRequest.Status =
                    FriendRequestStatus.Cancelled;

                pendingRequest.RespondedAt =
                    DateTime.UtcNow;

                pendingRequest.UpdatedAt =
                    DateTime.UtcNow;
            }

            var userBlock = new UserBlock
            {
                BlockerId = userId,
                BlockedUserId = blockedUserId
            };

            _dbContext.UserBlocks.Add(userBlock);

            await _dbContext.SaveChangesAsync(
                cancellationToken);

            await transaction.CommitAsync(
                cancellationToken);
        }

        public async Task UnblockUserAsync(
            int userId,
            int blockedUserId,
            CancellationToken cancellationToken = default)
        {
            var userBlock =
                await _dbContext.UserBlocks
                    .FirstOrDefaultAsync(
                        block =>
                            block.BlockerId == userId &&
                            block.BlockedUserId ==
                                blockedUserId,
                        cancellationToken)
                ?? throw new KeyNotFoundException(
                    "Bloklanmış istifadəçi tapılmadı.");

            _dbContext.UserBlocks.Remove(userBlock);

            await _dbContext.SaveChangesAsync(
                cancellationToken);
        }

        private async Task<bool> IsBlockedAsync(int firstUserId,int secondUserId,
            CancellationToken cancellationToken)
        {
            return await _dbContext.UserBlocks.AsNoTracking()
                .AnyAsync(
                    block =>
                        block.BlockerId ==firstUserId &&block.BlockedUserId ==secondUserId || block.BlockerId ==
                            secondUserId &&
                        block.BlockedUserId ==
                            firstUserId,
                    cancellationToken);
        }

        private static (int FirstUserId, int SecondUserId)GetOrderedUserIds(int firstUserId,int secondUserId)
        {
            return firstUserId < secondUserId? (firstUserId, secondUserId)
                : (secondUserId, firstUserId);
        }
    }
}
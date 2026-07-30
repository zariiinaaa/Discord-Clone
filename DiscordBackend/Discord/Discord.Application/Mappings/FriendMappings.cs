using Discord.Core.DTOs.Friends.Responses;
using Discord.Core.Entities;
using Discord.Core.Entities.Friends;

namespace Discord.Application.Mappings
{
    public static class FriendMappings
    {
        public static FriendUserResponseDto ToFriendUserResponseDto(this User user)
        {
            return new FriendUserResponseDto
            {
                Id = user.Id,
                Username = user.Username,
                DisplayName = user.DisplayName,
                AvatarUrl = user.AvatarUrl,
                Status = user.Status
            };
        }

        public static FriendRequestResponseDto ToResponseDto(this FriendRequest request)
        {
            return new FriendRequestResponseDto
            {
                Id = request.Id,
                Sender = request.Sender.ToFriendUserResponseDto(),
                Receiver = request.Receiver.ToFriendUserResponseDto(),
                Status = request.Status,
                CreatedAt = request.CreatedAt,
                RespondedAt = request.RespondedAt
            };
        }

        public static FriendResponseDto ToFriendResponseDto(this User user,DateTime friendsSince)
        {
            return new FriendResponseDto
            {
                Id = user.Id,
                Username = user.Username,
                DisplayName = user.DisplayName,
                AvatarUrl = user.AvatarUrl,
                Status = user.Status,
                FriendsSince = friendsSince
            };
        }
    }
}
using Discord.Core.DTOs.Users.Responses;
using Discord.Core.Entities;

namespace Discord.Application.Mappings;

public static class UserMappings
{
    public static UserResponseDto ToResponseDto(this User user)
    {
        ArgumentNullException.ThrowIfNull(user);

        return new UserResponseDto
        {
            Id = user.Id,
            Username = user.Username,
            DisplayName = user.DisplayName,
            Email = user.Email,
            AvatarUrl = user.AvatarUrl,
            Bio = user.Bio,
            Status = user.Status,
            Role = user.Role
        };
    }
}
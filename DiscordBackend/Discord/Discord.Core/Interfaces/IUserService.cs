using Discord.Core.DTOs.Users.Requests;
using Discord.Core.DTOs.Users.Responses;
using System;
using System.Collections.Generic;
using System.Text;

namespace Discord.Core.Interfaces
{
    public interface IUserService
    {
        Task<UserResponseDto> GetByIdAsync(int userId,CancellationToken cancellationToken = default);
        Task<UserResponseDto> UpdateProfileAsync(int userId,UpdateProfileRequestDto request,CancellationToken cancellationToken = default);

        Task<UserResponseDto> UpdateAvatarAsync(int userId,string? avatarUrl,CancellationToken cancellationToken = default);
    }
}

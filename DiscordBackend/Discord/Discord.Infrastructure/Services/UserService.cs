using Discord.Application.Mappings;
using Discord.Core.DTOs.Users.Requests;
using Discord.Core.DTOs.Users.Responses;
using Discord.Core.Interfaces;
using Discord.Infrastructure.Data;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace Discord.Infrastructure.Services;

public class UserService : IUserService
{
    private readonly AppDbContext _dbContext;
    private readonly IValidator<UpdateProfileRequestDto>
        _updateProfileValidator;

    public UserService(AppDbContext dbContext,IValidator<UpdateProfileRequestDto> updateProfileValidator)
    {
        _dbContext = dbContext;
        _updateProfileValidator = updateProfileValidator;
    }

    public async Task<UserResponseDto> GetByIdAsync(int userId,CancellationToken cancellationToken = default)
    {
        var user = await _dbContext.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(
                user => user.Id == userId,
                cancellationToken)
            ?? throw new KeyNotFoundException(
                "İstifadəçi tapılmadı.");

        return user.ToResponseDto();
    }

    public async Task<UserResponseDto> UpdateAvatarAsync(int userId,string? avatarUrl,CancellationToken cancellationToken = default)
    {
        var user = await _dbContext.Users
            .FirstOrDefaultAsync(
                user => user.Id == userId,
                cancellationToken)
            ?? throw new KeyNotFoundException(
                "İstifadəçi tapılmadı.");

        user.AvatarUrl = avatarUrl;
        user.UpdatedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync(
            cancellationToken);

        return user.ToResponseDto();
    }

    public async Task<UserResponseDto> UpdateProfileAsync(int userId,UpdateProfileRequestDto request,CancellationToken cancellationToken = default)
    {
        var validationResult =
            await _updateProfileValidator.ValidateAsync(
                request,
                cancellationToken);

        if (!validationResult.IsValid)
        {
            throw new ValidationException(
                validationResult.Errors);
        }

        var user = await _dbContext.Users
            .FirstOrDefaultAsync(
                user => user.Id == userId,
                cancellationToken)
            ?? throw new KeyNotFoundException(
                "İstifadəçi tapılmadı.");

        user.DisplayName = request.DisplayName.Trim();

        user.Bio = string.IsNullOrWhiteSpace(request.Bio)
            ? null
            : request.Bio.Trim();

        user.UpdatedAt = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);

        return user.ToResponseDto();
    }
}
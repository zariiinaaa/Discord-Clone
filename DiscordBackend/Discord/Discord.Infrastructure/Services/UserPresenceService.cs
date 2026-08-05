using Discord.Core.Enums;
using Discord.Core.Exceptions;
using Discord.Core.Interfaces;
using Discord.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Discord.Infrastructure.Services;

public class UserPresenceService : IUserPresenceService
{
    private readonly AppDbContext _dbContext;

    public UserPresenceService(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<UserStatus> ChangeStatusAsync(int userId, UserStatus status, CancellationToken cancellationToken = default)
    {
        if (!Enum.IsDefined(status))
        {
            throw new BadRequestException(
                "Status dəyəri etibarsızdır.");
        }

        if (status == UserStatus.Offline)
        {
            throw new BadRequestException(
                "Offline statusu istifadəçi tərəfindən seçilə bilməz.");
        }

        var user = await _dbContext.Users.FirstOrDefaultAsync(
       user => user.Id == userId,cancellationToken)
       ?? throw new KeyNotFoundException("İstifadəçi tapılmadı.");
        var isConnected = user.Status != UserStatus.Offline;
        user.PreferredStatus = status;
        if (isConnected)
        {
            user.Status = status;
        }

        user.UpdatedAt = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);
        if (!isConnected || status == UserStatus.Invisible)
        {
            return UserStatus.Offline;
        }
        else
        {
            return status;
        }
    }

    public async Task<UserStatus> MarkConnectedAsync(
        int userId,
        CancellationToken cancellationToken = default)
    {
        var user = await _dbContext.Users.FirstOrDefaultAsync(user => user.Id == userId,cancellationToken)
        ?? throw new KeyNotFoundException("İstifadəçi tapılmadı.");

        
        if (user.PreferredStatus == UserStatus.Offline)
        {
            user.PreferredStatus = UserStatus.Online;
        }

        user.Status = user.PreferredStatus;
        user.UpdatedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);
        return user.Status == UserStatus.Invisible? UserStatus.Offline: user.Status;
    }

    public async Task<UserStatus> MarkDisconnectedAsync(int userId,
    CancellationToken cancellationToken = default)
    {
        var user = await _dbContext.Users.FirstOrDefaultAsync(
        user => user.Id == userId,cancellationToken)
        ?? throw new KeyNotFoundException("İstifadəçi tapılmadı.");

        var now = DateTime.UtcNow;

        user.Status = UserStatus.Offline;
        user.LastSeenAt = now;
        user.UpdatedAt = now;

        await _dbContext.SaveChangesAsync(cancellationToken);

        return UserStatus.Offline;
    }

    public async Task ResetAllUsersToOfflineAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;

        await _dbContext.Users.Where(user => user.Status != UserStatus.Offline)
        .ExecuteUpdateAsync( setters => setters.SetProperty(
         user => user.Status, UserStatus.Offline).SetProperty(user => user.LastSeenAt,now)
         .SetProperty(user => user.UpdatedAt,now),
         cancellationToken);
    }
}
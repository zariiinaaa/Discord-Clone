using Discord.Application.Mappings;
using Discord.Core.DTOs.ServerMembers.Responses;
using Discord.Core.Exceptions;
using Discord.Core.Interfaces;
using Discord.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Discord.Infrastructure.Services;

public class ServerMemberService : IServerMemberService
{
    private readonly AppDbContext _dbContext;

    public ServerMemberService(
        AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<
        IReadOnlyCollection<ServerMemberResponseDto>>
        GetMembersAsync(int serverId,int userId,CancellationToken cancellationToken = default)
    {
        var server = await _dbContext.Servers
            .AsNoTracking()
            .FirstOrDefaultAsync(
                server => server.Id == serverId,
                cancellationToken)
            ?? throw new KeyNotFoundException(
                "Server tapılmadı.");

        var isMember =
            await _dbContext.ServerMembers.AnyAsync(
                member =>
                    member.ServerId == serverId &&
                    member.UserId == userId,
                cancellationToken);

        if (!isMember)
        {
            throw new ForbiddenException(
                "Yalnız server üzvləri üzv siyahısını görə bilər.");
        }

        var members = await _dbContext.ServerMembers
            .AsNoTracking()
            .Include(member => member.User)
            .Where(member => member.ServerId == serverId)
            .OrderByDescending(
                member => member.UserId == server.OwnerId)
            .ThenBy(member => member.User.DisplayName)
            .ToListAsync(cancellationToken);

        return members
            .Select(member =>
                member.ToResponseDto(server.OwnerId))
            .ToList();
    }

    public async Task LeaveAsync(int serverId,int userId,CancellationToken cancellationToken = default)
    {
        var server = await _dbContext.Servers
            .AsNoTracking()
            .FirstOrDefaultAsync(
                server => server.Id == serverId,
                cancellationToken)
            ?? throw new KeyNotFoundException(
                "Server tapılmadı.");

        if (server.OwnerId == userId)
        {
            throw new BadRequestException("Server sahibi serverdən çıxa bilməz.");
        }

        var membership =
            await _dbContext.ServerMembers
                .FirstOrDefaultAsync(
                    member => member.ServerId == serverId &&member.UserId == userId, cancellationToken)
            ?? throw new KeyNotFoundException("Server üzvlüyü tapılmadı.");

        _dbContext.ServerMembers.Remove(membership);

        await _dbContext.SaveChangesAsync( cancellationToken);
    }

    public async Task KickAsync(int serverId,int memberUserId,int currentUserId,CancellationToken cancellationToken = default)
    {
        var server = await _dbContext.Servers
            .AsNoTracking()
            .FirstOrDefaultAsync(
                server => server.Id == serverId,
                cancellationToken)
            ?? throw new KeyNotFoundException(
                "Server tapılmadı.");

        if (server.OwnerId != currentUserId)
        {
            throw new ForbiddenException(
                "Yalnız server sahibi üzvü çıxara bilər.");
        }

        if (server.OwnerId == memberUserId)
        {
            throw new BadRequestException(
                "Server sahibi serverdən çıxarıla bilməz.");
        }

        var membership =
            await _dbContext.ServerMembers
                .FirstOrDefaultAsync(
                    member =>member.ServerId == serverId &&
                        member.UserId == memberUserId,
                    cancellationToken)
            ?? throw new KeyNotFoundException(
                "Server üzvü tapılmadı.");

        _dbContext.ServerMembers.Remove(membership);

        await _dbContext.SaveChangesAsync(
            cancellationToken);
    }
}
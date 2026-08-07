using Discord.Application.Mappings;
using Discord.Core.DTOs.ServerMembers.Responses;
using Discord.Core.Entities.Servers;
using Discord.Core.Enums;
using Discord.Core.Exceptions;
using Discord.Core.Interfaces;
using Discord.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Discord.Infrastructure.Services;

public class ServerMemberService : IServerMemberService
{
    private readonly AppDbContext _dbContext;
    private readonly IServerPermissionService _serverPermissionService;
    public ServerMemberService(AppDbContext dbContext, IServerPermissionService serverPermissionService)
    {
        _dbContext = dbContext;
        _serverPermissionService = serverPermissionService;
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
     .Include(member => member.MemberRoles)
         .ThenInclude(memberRole => memberRole.ServerRole)
         .ThenInclude(role => role.Permissions)
     .Where(member => member.ServerId == serverId)
     .OrderByDescending(member => member.UserId == server.OwnerId)
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
    public async Task KickAsync(int serverId,int memberUserId,int currentUserId,
    CancellationToken cancellationToken = default)
    {
        var server = await _dbContext.Servers.AsNoTracking().FirstOrDefaultAsync(
        server => server.Id == serverId,cancellationToken)
            ?? throw new KeyNotFoundException(
                "Server tapılmadı.");

        await _serverPermissionService.EnsurePermissionAsync( serverId,
         currentUserId,ServerPermission.KickMembers,cancellationToken);

        if (server.OwnerId == memberUserId)
        {
            throw new BadRequestException(
                "Server sahibi serverdən çıxarıla bilməz.");
        }

        if (currentUserId == memberUserId)
        {
            throw new BadRequestException(
                "Özünüzü serverdən kick edə bilməzsiniz.");
        }

        var membership = await _dbContext.ServerMembers
            .Include(member => member.MemberRoles)
            .FirstOrDefaultAsync(
                member =>
                    member.ServerId == serverId &&
                    member.UserId == memberUserId,
                cancellationToken)
            ?? throw new KeyNotFoundException("Server üzvü tapılmadı.");

        if (server.OwnerId != currentUserId)
        {
            var currentUserHighestRolePosition =await GetHighestRolePositionAsync(serverId,
           currentUserId,cancellationToken);

            var targetUserHighestRolePosition =
                await GetHighestRolePositionAsync(serverId, memberUserId,cancellationToken);

            if (currentUserHighestRolePosition <= targetUserHighestRolePosition)
            {
                throw new ForbiddenException(
                    "Eyni və ya daha yüksək rola sahib üzvü serverdən çıxara bilməzsiniz.");
            }
        }

        _dbContext.ServerMemberRoles.RemoveRange(membership.MemberRoles);

        _dbContext.ServerMembers.Remove(membership);

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task BanAsync(int serverId,int memberUserId,int currentUserId,string? reason,
    CancellationToken cancellationToken = default)
    {
        var server = await _dbContext.Servers
            .AsNoTracking()
            .FirstOrDefaultAsync(
                server => server.Id == serverId,
                cancellationToken)
            ?? throw new KeyNotFoundException(
                "Server tapılmadı.");

        await _serverPermissionService
            .EnsurePermissionAsync(
                serverId,
                currentUserId,
                ServerPermission.BanMembers,
                cancellationToken);

        if (server.OwnerId == memberUserId)
        {
            throw new BadRequestException(
                "Server sahibi ban edilə bilməz.");
        }

        if (currentUserId == memberUserId)
        {
            throw new BadRequestException(
                "Özünüzü serverdən ban edə bilməzsiniz.");
        }

        var alreadyBanned =
            await _dbContext.ServerBans
                .AnyAsync(
                    serverBan =>serverBan.ServerId == serverId &&
                        serverBan.UserId == memberUserId,
                    cancellationToken);

        if (alreadyBanned)
        {
            throw new ConflictException(
                "Bu istifadəçi artıq serverdən ban edilib.");
        }

        var membership =
            await _dbContext.ServerMembers .Include(member =>
                    member.MemberRoles).FirstOrDefaultAsync(
                    member =>  member.ServerId == serverId &&member.UserId == memberUserId,
                    cancellationToken)
            ?? throw new KeyNotFoundException(
                "Server üzvü tapılmadı.");

        if (server.OwnerId != currentUserId)
        {
            var currentUserHighestRolePosition =
                await GetHighestRolePositionAsync(
                    serverId,
                    currentUserId,
                    cancellationToken);

            var targetUserHighestRolePosition = await GetHighestRolePositionAsync(serverId,
            memberUserId,cancellationToken);

            if (
                currentUserHighestRolePosition <=targetUserHighestRolePosition
            )
            {
                throw new ForbiddenException(
                    "Eyni və ya daha yüksək rola sahib üzvü ban edə bilməzsiniz.");
            }
        }

        var normalizedReason =
            string.IsNullOrWhiteSpace(reason)
                ? null
                : reason.Trim();

        if (normalizedReason?.Length > 500)
        {
            throw new BadRequestException(
                "Ban səbəbi maksimum 500 simvol ola bilər.");
        }

        var serverBan = new ServerBan
        {
            ServerId = serverId,
            UserId = memberUserId,
            BannedByUserId = currentUserId,
            Reason = normalizedReason
        };

        _dbContext.ServerBans.Add(serverBan);

        _dbContext.ServerMemberRoles
            .RemoveRange( membership.MemberRoles);

        _dbContext.ServerMembers .Remove(membership);

        await _dbContext.SaveChangesAsync(
            cancellationToken);
    }
    public async Task UnbanAsync(int serverId, int bannedUserId,int currentUserId,
    CancellationToken cancellationToken = default)
    {
        await _serverPermissionService.EnsurePermissionAsync( serverId,currentUserId,
         ServerPermission.BanMembers,cancellationToken);

        var serverBan = await _dbContext.ServerBans.FirstOrDefaultAsync(
       ban => ban.ServerId == serverId && ban.UserId == bannedUserId,
                    cancellationToken)
            ?? throw new KeyNotFoundException(
                "Ban tapılmadı.");

        _dbContext.ServerBans.Remove( serverBan);

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task< IReadOnlyCollection<ServerBanResponseDto>>GetBansAsync(int serverId, int currentUserId,
        CancellationToken cancellationToken = default)
    {
        await _serverPermissionService.EnsurePermissionAsync(serverId,
        currentUserId, ServerPermission.BanMembers,
        cancellationToken);

        var bans = await _dbContext.ServerBans
            .AsNoTracking()
            .Include(serverBan =>
                serverBan.User)
            .Where(serverBan =>
                serverBan.ServerId == serverId)
            .OrderByDescending(serverBan =>
                serverBan.CreatedAt)
            .ToListAsync(cancellationToken);

        return bans
            .Select(serverBan =>
                new ServerBanResponseDto
                {
                    Id = serverBan.Id,
                    UserId = serverBan.UserId,
                    Username =serverBan.User.Username,
                    DisplayName = serverBan.User.DisplayName,
                    AvatarUrl =serverBan.User.AvatarUrl,
                    BannedByUserId =serverBan.BannedByUserId,
                    Reason = serverBan.Reason,
                    CreatedAt =serverBan.CreatedAt
                }).ToList();
    }
    private async Task<int>GetHighestRolePositionAsync(int serverId, int userId,
     CancellationToken cancellationToken)
    {
        var highestRolePosition =
            await _dbContext.ServerMemberRoles
                .AsNoTracking()
                .Where(memberRole =>
                    memberRole.ServerMember.ServerId == serverId &&
                    memberRole.ServerMember.UserId ==userId)
                .MaxAsync( memberRole =>(int?)memberRole.ServerRole.Position,
                    cancellationToken);

        return highestRolePosition ?? 0;
    }
}
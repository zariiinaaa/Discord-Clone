using Discord.Core.Enums;
using Discord.Core.Exceptions;
using Discord.Core.Interfaces;
using Discord.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Discord.Infrastructure.Services;

public class ChannelPermissionService : IChannelPermissionService
{
    private readonly AppDbContext _dbContext;
    private readonly IServerPermissionService _serverPermissionService;

    public ChannelPermissionService(AppDbContext dbContext,IServerPermissionService serverPermissionService)
    {
        _dbContext = dbContext;
        _serverPermissionService = serverPermissionService;
    }

    public async Task<IReadOnlyCollection<ServerPermission>> GetPermissionsAsync(int channelId,int userId,
        CancellationToken cancellationToken = default)
    {
        var channel = await _dbContext.Channels
            .AsNoTracking()
            .Where(channel => channel.Id == channelId)
            .Select(channel => new
            {
                channel.Id,
                channel.ServerId
            })
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new KeyNotFoundException(
                "Kanal tapılmadı.");

        var serverPermissions =
            await _serverPermissionService.GetPermissionsAsync(
                channel.ServerId,
                userId,
                cancellationToken);

        if (serverPermissions.Contains(
            ServerPermission.Administrator))
        {
            return Enum.GetValues<ServerPermission>();
        }

        var serverMemberId = await _dbContext.ServerMembers
            .AsNoTracking()
            .Where(member =>
                member.ServerId == channel.ServerId &&
                member.UserId == userId)
            .Select(member => (int?)member.Id)
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new ForbiddenException(
                "Yalnız server üzvləri kanal icazələrindən istifadə edə bilər.");

        var roles = await _dbContext.ServerRoles
            .AsNoTracking()
            .Where(role =>
                role.ServerId == channel.ServerId &&
                (role.IsDefault ||
                 role.MemberRoles.Any(memberRole =>
                     memberRole.ServerMemberId == serverMemberId)))
            .Select(role => new
            {
                role.Id,
                role.IsDefault
            })
            .ToListAsync(cancellationToken);

        var defaultRoleId = roles
            .Where(role => role.IsDefault)
            .Select(role => (int?)role.Id)
            .FirstOrDefault()
            ?? throw new InvalidOperationException(
                "Serverin @everyone rolu tapılmadı.");

        var roleIds = roles
            .Select(role => role.Id)
            .ToList();

        var roleOverrides =
            await _dbContext.ChannelRolePermissionOverrides
                .AsNoTracking()
                .Where(overrideItem =>
                    overrideItem.ChannelId == channelId &&
                    roleIds.Contains(overrideItem.RoleId))
                .Select(overrideItem => new
                {
                    overrideItem.RoleId,
                    overrideItem.Permission,
                    overrideItem.OverrideType
                })
                .ToListAsync(cancellationToken);

        var memberOverrides =
            await _dbContext.ChannelMemberPermissionOverrides
                .AsNoTracking()
                .Where(overrideItem =>
                    overrideItem.ChannelId == channelId &&
                    overrideItem.UserId == userId)
                .Select(overrideItem => new
                {
                    overrideItem.Permission,
                    overrideItem.OverrideType
                })
                .ToListAsync(cancellationToken);

        var effectivePermissions =
            serverPermissions.ToHashSet();

        var everyoneOverrides = roleOverrides
            .Where(overrideItem =>
                overrideItem.RoleId == defaultRoleId);

        foreach (var overrideItem in everyoneOverrides)
        {
            ApplyOverride(
                effectivePermissions,
                overrideItem.Permission,
                overrideItem.OverrideType);
        }

        var assignedRoleOverrides = roleOverrides
            .Where(overrideItem =>
                overrideItem.RoleId != defaultRoleId)
            .GroupBy(overrideItem =>
                overrideItem.Permission);

        foreach (var permissionGroup in assignedRoleOverrides)
        {
            var hasAllow = permissionGroup.Any(
                overrideItem =>
                    overrideItem.OverrideType ==
                    PermissionOverrideType.Allow);

            var hasDeny = permissionGroup.Any(
                overrideItem =>
                    overrideItem.OverrideType ==
                    PermissionOverrideType.Deny);

            if (hasAllow)
            {
                effectivePermissions.Add(
                    permissionGroup.Key);
            }
            else if (hasDeny)
            {
                effectivePermissions.Remove(
                    permissionGroup.Key);
            }
        }

        foreach (var overrideItem in memberOverrides)
        {
            ApplyOverride(
                effectivePermissions,
                overrideItem.Permission,
                overrideItem.OverrideType);
        }

        return effectivePermissions
            .OrderBy(permission => (int)permission)
            .ToList();
    }

    public async Task<bool> HasPermissionAsync(
        int channelId,
        int userId,
        ServerPermission permission,
        CancellationToken cancellationToken = default)
    {
        var permissions = await GetPermissionsAsync(
            channelId,
            userId,
            cancellationToken);

        return permissions.Contains(permission);
    }

    public async Task EnsurePermissionAsync(
        int channelId,
        int userId,
        ServerPermission permission,
        CancellationToken cancellationToken = default)
    {
        var hasPermission = await HasPermissionAsync(
            channelId,
            userId,
            permission,
            cancellationToken);

        if (!hasPermission)
        {
            throw new ForbiddenException(
                "Bu kanalda həmin əməliyyatı etmək icazəniz yoxdur.");
        }
    }

    private static void ApplyOverride(
        HashSet<ServerPermission> permissions,
        ServerPermission permission,
        PermissionOverrideType overrideType)
    {
        if (overrideType == PermissionOverrideType.Allow)
        {
            permissions.Add(permission);
            return;
        }

        permissions.Remove(permission);
    }
}
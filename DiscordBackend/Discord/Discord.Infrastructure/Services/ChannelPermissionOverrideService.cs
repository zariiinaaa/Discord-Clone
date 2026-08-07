using Discord.Core.DTOs.ChannelPermissions.Requests;
using Discord.Core.DTOs.ChannelPermissions.Responses;
using Discord.Core.Entities.Servers;
using Discord.Core.Enums;
using Discord.Core.Exceptions;
using Discord.Core.Interfaces;
using Discord.Infrastructure.Data;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace Discord.Infrastructure.Services;

public class ChannelPermissionOverrideService : IChannelPermissionOverrideService
{
    private readonly AppDbContext _dbContext;
    private readonly IChannelPermissionService _channelPermissionService;
    private readonly IValidator<UpdateChannelPermissionOverridesRequestDto> _validator;
    private readonly IChannelPermissionRealtimeNotifier _realtimeNotifier;

    public ChannelPermissionOverrideService(AppDbContext dbContext,
        IChannelPermissionService channelPermissionService,
        IValidator<UpdateChannelPermissionOverridesRequestDto> validator,
        IChannelPermissionRealtimeNotifier realtimeNotifier)
    {
        _dbContext = dbContext;
        _channelPermissionService = channelPermissionService;
        _validator = validator;
        _realtimeNotifier = realtimeNotifier;
    }

    public async Task<ChannelPermissionOverridesResponseDto> GetAsync(int channelId, int userId,
        CancellationToken cancellationToken = default)
    {
        await EnsureCanManageChannelAsync(channelId, userId, cancellationToken);

        var channelExists = await _dbContext.Channels.AsNoTracking()
            .AnyAsync(channel => channel.Id == channelId, cancellationToken);

        if (!channelExists)
        {
            throw new KeyNotFoundException("Kanal tapılmadı.");
        }

        var roleOverrides = await _dbContext.ChannelRolePermissionOverrides
            .AsNoTracking()
            .Where(overrideItem => overrideItem.ChannelId == channelId)
            .OrderBy(overrideItem => overrideItem.Role.Position)
            .ThenBy(overrideItem => overrideItem.Permission)
            .Select(overrideItem => new ChannelRolePermissionOverrideResponseDto
            {
                RoleId = overrideItem.RoleId,
                RoleName = overrideItem.Role.Name,
                Permission = overrideItem.Permission,
                OverrideType = overrideItem.OverrideType
            })
            .ToListAsync(cancellationToken);

        var memberOverrides = await _dbContext.ChannelMemberPermissionOverrides
            .AsNoTracking()
            .Where(overrideItem => overrideItem.ChannelId == channelId)
            .OrderBy(overrideItem => overrideItem.User.Username)
            .ThenBy(overrideItem => overrideItem.Permission)
            .Select(overrideItem => new ChannelMemberPermissionOverrideResponseDto
            {
                UserId = overrideItem.UserId,
                Username = overrideItem.User.Username,
                DisplayName = overrideItem.User.DisplayName,
                Permission = overrideItem.Permission,
                OverrideType = overrideItem.OverrideType
            })
            .ToListAsync(cancellationToken);

        return new ChannelPermissionOverridesResponseDto
        {
            ChannelId = channelId,
            RoleOverrides = roleOverrides,
            MemberOverrides = memberOverrides
        };
    }

    public async Task UpdateRoleAsync(int channelId, int roleId, int userId,
        UpdateChannelPermissionOverridesRequestDto request,
        CancellationToken cancellationToken = default)
    {
        await ValidateAsync(request, cancellationToken);
        await EnsureCanManageChannelAsync(channelId, userId, cancellationToken);

        var channel = await GetTrackedChannelAsync(channelId, cancellationToken);

        var roleExists = await _dbContext.ServerRoles.AsNoTracking()
            .AnyAsync(role =>
                role.Id == roleId &&
                role.ServerId == channel.ServerId,
                cancellationToken);

        if (!roleExists)
        {
            throw new KeyNotFoundException("Bu serverə aid rol tapılmadı.");
        }

        await using var transaction = await _dbContext.Database
            .BeginTransactionAsync(cancellationToken);

        var existingOverrides = await _dbContext.ChannelRolePermissionOverrides
            .Where(overrideItem =>
                overrideItem.ChannelId == channelId &&
                overrideItem.RoleId == roleId)
            .ToListAsync(cancellationToken);

        _dbContext.ChannelRolePermissionOverrides.RemoveRange(existingOverrides);

        var newOverrides = request.Overrides
            .Select(overrideItem => new ChannelRolePermissionOverride
            {
                ChannelId = channelId,
                RoleId = roleId,
                Permission = overrideItem.Permission,
                OverrideType = overrideItem.OverrideType
            })
            .ToList();

        if (newOverrides.Count > 0)
        {
            await _dbContext.ChannelRolePermissionOverrides
                .AddRangeAsync(newOverrides, cancellationToken);
        }

        MarkAsNotSyncedWhenChild(channel);

        await _dbContext.SaveChangesAsync(cancellationToken);
        await RefreshPrivateStateAsync(channel, cancellationToken);

        if (channel.Type == ChannelType.Category)
        {
            await SynchronizeSyncedChildrenAsync(channel.Id, cancellationToken);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        await NotifyPermissionChangesAsync(
     channel.ServerId, channel.Id, channel.Type, cancellationToken);
    }

    public async Task DeleteRoleAsync(int channelId, int roleId, int userId,
        CancellationToken cancellationToken = default)
    {
        await EnsureCanManageChannelAsync(channelId, userId, cancellationToken);

        var channel = await GetTrackedChannelAsync(channelId, cancellationToken);

        var roleExists = await _dbContext.ServerRoles.AsNoTracking()
            .AnyAsync(role =>
                role.Id == roleId &&
                role.ServerId == channel.ServerId,
                cancellationToken);

        if (!roleExists)
        {
            throw new KeyNotFoundException("Bu serverə aid rol tapılmadı.");
        }

        await using var transaction = await _dbContext.Database
            .BeginTransactionAsync(cancellationToken);

        var overrides = await _dbContext.ChannelRolePermissionOverrides
            .Where(overrideItem =>
                overrideItem.ChannelId == channelId &&
                overrideItem.RoleId == roleId)
            .ToListAsync(cancellationToken);

        if (overrides.Count > 0)
        {
            _dbContext.ChannelRolePermissionOverrides.RemoveRange(overrides);
        }

        MarkAsNotSyncedWhenChild(channel);

        await _dbContext.SaveChangesAsync(cancellationToken);
        await RefreshPrivateStateAsync(channel, cancellationToken);

        if (channel.Type == ChannelType.Category)
        {
            await SynchronizeSyncedChildrenAsync(channel.Id, cancellationToken);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        await NotifyPermissionChangesAsync(
     channel.ServerId, channel.Id, channel.Type, cancellationToken);
    }

    public async Task UpdateMemberAsync(int channelId, int memberUserId, int userId,
        UpdateChannelPermissionOverridesRequestDto request,
        CancellationToken cancellationToken = default)
    {
        await ValidateAsync(request, cancellationToken);
        await EnsureCanManageChannelAsync(channelId, userId, cancellationToken);

        var channel = await GetTrackedChannelAsync(channelId, cancellationToken);

        var memberExists = await _dbContext.ServerMembers.AsNoTracking()
            .AnyAsync(member =>
                member.ServerId == channel.ServerId &&
                member.UserId == memberUserId,
                cancellationToken);

        if (!memberExists)
        {
            throw new KeyNotFoundException("Bu serverə aid üzv tapılmadı.");
        }

        await using var transaction = await _dbContext.Database
            .BeginTransactionAsync(cancellationToken);

        var existingOverrides = await _dbContext.ChannelMemberPermissionOverrides
            .Where(overrideItem =>
                overrideItem.ChannelId == channelId &&
                overrideItem.UserId == memberUserId)
            .ToListAsync(cancellationToken);

        _dbContext.ChannelMemberPermissionOverrides.RemoveRange(existingOverrides);

        var newOverrides = request.Overrides
            .Select(overrideItem => new ChannelMemberPermissionOverride
            {
                ChannelId = channelId,
                UserId = memberUserId,
                Permission = overrideItem.Permission,
                OverrideType = overrideItem.OverrideType
            })
            .ToList();

        if (newOverrides.Count > 0)
        {
            await _dbContext.ChannelMemberPermissionOverrides
                .AddRangeAsync(newOverrides, cancellationToken);
        }

        MarkAsNotSyncedWhenChild(channel);

        await _dbContext.SaveChangesAsync(cancellationToken);
        await RefreshPrivateStateAsync(channel, cancellationToken);

        if (channel.Type == ChannelType.Category)
        {
            await SynchronizeSyncedChildrenAsync(channel.Id, cancellationToken);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        await NotifyPermissionChangesAsync(
       channel.ServerId, channel.Id, channel.Type, cancellationToken);
    }

    public async Task DeleteMemberAsync(int channelId, int memberUserId, int userId,
        CancellationToken cancellationToken = default)
    {
        await EnsureCanManageChannelAsync(channelId, userId, cancellationToken);

        var channel = await GetTrackedChannelAsync(channelId, cancellationToken);

        var memberExists = await _dbContext.ServerMembers.AsNoTracking()
            .AnyAsync(member =>
                member.ServerId == channel.ServerId &&
                member.UserId == memberUserId,
                cancellationToken);

        if (!memberExists)
        {
            throw new KeyNotFoundException("Bu serverə aid üzv tapılmadı.");
        }

        await using var transaction = await _dbContext.Database
            .BeginTransactionAsync(cancellationToken);

        var overrides = await _dbContext.ChannelMemberPermissionOverrides
            .Where(overrideItem =>
                overrideItem.ChannelId == channelId &&
                overrideItem.UserId == memberUserId)
            .ToListAsync(cancellationToken);

        if (overrides.Count > 0)
        {
            _dbContext.ChannelMemberPermissionOverrides.RemoveRange(overrides);
        }

        MarkAsNotSyncedWhenChild(channel);

        await _dbContext.SaveChangesAsync(cancellationToken);
        await RefreshPrivateStateAsync(channel, cancellationToken);

        if (channel.Type == ChannelType.Category)
        {
            await SynchronizeSyncedChildrenAsync(channel.Id, cancellationToken);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        await NotifyPermissionChangesAsync(
     channel.ServerId, channel.Id, channel.Type, cancellationToken);
    }

    public async Task SyncWithCategoryAsync(int channelId, int userId,
        CancellationToken cancellationToken = default)
    {
        await EnsureCanManageChannelAsync(channelId, userId, cancellationToken);

        var channel = await GetTrackedChannelAsync(channelId, cancellationToken);

        if (channel.Type == ChannelType.Category)
        {
            throw new BadRequestException(
                "Category özü başqa category ilə sync edilə bilməz.");
        }

        if (!channel.ParentCategoryId.HasValue)
        {
            throw new BadRequestException(
                "Kanal heç bir category daxilində deyil.");
        }

        var categoryExists = await _dbContext.Channels.AsNoTracking()
            .AnyAsync(category =>
                category.Id == channel.ParentCategoryId.Value &&
                category.ServerId == channel.ServerId &&
                category.Type == ChannelType.Category,
                cancellationToken);

        if (!categoryExists)
        {
            throw new BadRequestException(
                "Kanalın parent category-si tapılmadı.");
        }

        await using var transaction = await _dbContext.Database
            .BeginTransactionAsync(cancellationToken);

        var existingRoleOverrides = await _dbContext.ChannelRolePermissionOverrides
            .Where(overrideItem => overrideItem.ChannelId == channelId)
            .ToListAsync(cancellationToken);

        var existingMemberOverrides = await _dbContext.ChannelMemberPermissionOverrides
            .Where(overrideItem => overrideItem.ChannelId == channelId)
            .ToListAsync(cancellationToken);

        _dbContext.ChannelRolePermissionOverrides
            .RemoveRange(existingRoleOverrides);

        _dbContext.ChannelMemberPermissionOverrides
            .RemoveRange(existingMemberOverrides);

        var categoryRoleOverrides = await _dbContext.ChannelRolePermissionOverrides
            .AsNoTracking()
            .Where(overrideItem =>
                overrideItem.ChannelId == channel.ParentCategoryId.Value)
            .ToListAsync(cancellationToken);

        var categoryMemberOverrides = await _dbContext.ChannelMemberPermissionOverrides
            .AsNoTracking()
            .Where(overrideItem =>
                overrideItem.ChannelId == channel.ParentCategoryId.Value)
            .ToListAsync(cancellationToken);

        var newRoleOverrides = categoryRoleOverrides
            .Select(overrideItem => new ChannelRolePermissionOverride
            {
                ChannelId = channelId,
                RoleId = overrideItem.RoleId,
                Permission = overrideItem.Permission,
                OverrideType = overrideItem.OverrideType
            })
            .ToList();

        var newMemberOverrides = categoryMemberOverrides
            .Select(overrideItem => new ChannelMemberPermissionOverride
            {
                ChannelId = channelId,
                UserId = overrideItem.UserId,
                Permission = overrideItem.Permission,
                OverrideType = overrideItem.OverrideType
            })
            .ToList();

        if (newRoleOverrides.Count > 0)
        {
            await _dbContext.ChannelRolePermissionOverrides
                .AddRangeAsync(newRoleOverrides, cancellationToken);
        }

        if (newMemberOverrides.Count > 0)
        {
            await _dbContext.ChannelMemberPermissionOverrides
                .AddRangeAsync(newMemberOverrides, cancellationToken);
        }

        var defaultRoleId = await GetDefaultRoleIdAsync(
            channel.ServerId, cancellationToken);

        channel.IsPrivate = defaultRoleId.HasValue &&
            categoryRoleOverrides.Any(overrideItem =>
                overrideItem.RoleId == defaultRoleId.Value &&
                overrideItem.Permission == ServerPermission.ViewChannels &&
                overrideItem.OverrideType == PermissionOverrideType.Deny);

        channel.IsPermissionSynced = true;
        channel.UpdatedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);
        await NotifyPermissionChangesAsync(
            channel.ServerId, channel.Id, channel.Type, cancellationToken);
    }

    private async Task SynchronizeSyncedChildrenAsync(int categoryId,
        CancellationToken cancellationToken)
    {
        var children = await _dbContext.Channels
            .Where(channel =>
                channel.ParentCategoryId == categoryId &&
                channel.IsPermissionSynced)
            .ToListAsync(cancellationToken);

        if (children.Count == 0)
        {
            return;
        }

        var childIds = children
            .Select(channel => channel.Id)
            .ToArray();

        var existingRoleOverrides = await _dbContext.ChannelRolePermissionOverrides
            .Where(overrideItem =>
                childIds.Contains(overrideItem.ChannelId))
            .ToListAsync(cancellationToken);

        var existingMemberOverrides = await _dbContext.ChannelMemberPermissionOverrides
            .Where(overrideItem =>
                childIds.Contains(overrideItem.ChannelId))
            .ToListAsync(cancellationToken);

        _dbContext.ChannelRolePermissionOverrides
            .RemoveRange(existingRoleOverrides);

        _dbContext.ChannelMemberPermissionOverrides
            .RemoveRange(existingMemberOverrides);

        var categoryRoleOverrides = await _dbContext.ChannelRolePermissionOverrides
            .AsNoTracking()
            .Where(overrideItem => overrideItem.ChannelId == categoryId)
            .ToListAsync(cancellationToken);

        var categoryMemberOverrides = await _dbContext.ChannelMemberPermissionOverrides
            .AsNoTracking()
            .Where(overrideItem => overrideItem.ChannelId == categoryId)
            .ToListAsync(cancellationToken);

        var newRoleOverrides = new List<ChannelRolePermissionOverride>();
        var newMemberOverrides = new List<ChannelMemberPermissionOverride>();

        foreach (var child in children)
        {
            newRoleOverrides.AddRange(categoryRoleOverrides.Select(
                overrideItem => new ChannelRolePermissionOverride
                {
                    ChannelId = child.Id,
                    RoleId = overrideItem.RoleId,
                    Permission = overrideItem.Permission,
                    OverrideType = overrideItem.OverrideType
                }));

            newMemberOverrides.AddRange(categoryMemberOverrides.Select(
                overrideItem => new ChannelMemberPermissionOverride
                {
                    ChannelId = child.Id,
                    UserId = overrideItem.UserId,
                    Permission = overrideItem.Permission,
                    OverrideType = overrideItem.OverrideType
                }));
        }

        if (newRoleOverrides.Count > 0)
        {
            await _dbContext.ChannelRolePermissionOverrides
                .AddRangeAsync(newRoleOverrides, cancellationToken);
        }

        if (newMemberOverrides.Count > 0)
        {
            await _dbContext.ChannelMemberPermissionOverrides
                .AddRangeAsync(newMemberOverrides, cancellationToken);
        }

        var serverId = children[0].ServerId;
        var defaultRoleId = await GetDefaultRoleIdAsync(
            serverId, cancellationToken);

        var isPrivate = defaultRoleId.HasValue &&
            categoryRoleOverrides.Any(overrideItem =>
                overrideItem.RoleId == defaultRoleId.Value &&
                overrideItem.Permission == ServerPermission.ViewChannels &&
                overrideItem.OverrideType == PermissionOverrideType.Deny);

        foreach (var child in children)
        {
            child.IsPrivate = isPrivate;
            child.UpdatedAt = DateTime.UtcNow;
        }
    }

    private async Task RefreshPrivateStateAsync(Channel channel,
        CancellationToken cancellationToken)
    {
        var defaultRoleId = await GetDefaultRoleIdAsync(
            channel.ServerId, cancellationToken);

        channel.IsPrivate = defaultRoleId.HasValue &&
            await _dbContext.ChannelRolePermissionOverrides
                .AsNoTracking()
                .AnyAsync(overrideItem =>
                    overrideItem.ChannelId == channel.Id &&
                    overrideItem.RoleId == defaultRoleId.Value &&
                    overrideItem.Permission == ServerPermission.ViewChannels &&
                    overrideItem.OverrideType == PermissionOverrideType.Deny,
                    cancellationToken);

        channel.UpdatedAt = DateTime.UtcNow;
    }

    private async Task<int?> GetDefaultRoleIdAsync(int serverId,
        CancellationToken cancellationToken)
    {
        return await _dbContext.ServerRoles.AsNoTracking()
            .Where(role =>
                role.ServerId == serverId &&
                role.IsDefault)
            .Select(role => (int?)role.Id)
            .FirstOrDefaultAsync(cancellationToken);
    }

    private static void MarkAsNotSyncedWhenChild(Channel channel)
    {
        if (channel.Type != ChannelType.Category)
        {
            channel.IsPermissionSynced = false;
            channel.UpdatedAt = DateTime.UtcNow;
        }
    }

    private async Task NotifyPermissionChangesAsync(int serverId, int channelId,
    ChannelType channelType, CancellationToken cancellationToken)
    {
        var affectedChannelIds = channelType == ChannelType.Category
            ? await _dbContext.Channels.AsNoTracking()
                .Where(channel =>
                    channel.Id == channelId ||
                    (channel.ParentCategoryId == channelId &&
                     channel.IsPermissionSynced))
                .Select(channel => channel.Id)
                .ToListAsync(cancellationToken)
            : new List<int> { channelId };

        var userIds = await _dbContext.ServerMembers.AsNoTracking()
            .Where(member => member.ServerId == serverId)
            .Select(member => member.UserId)
            .ToListAsync(cancellationToken);

        if (userIds.Count == 0)
        {
            return;
        }

        await _realtimeNotifier.NotifyServerChannelsChangedAsync(
            serverId, userIds, cancellationToken);

        foreach (var affectedChannelId in affectedChannelIds.Distinct())
        {
            var revokedUserIds = new List<int>();

            foreach (var memberUserId in userIds)
            {
                var canView = await _channelPermissionService.HasPermissionAsync(
                    affectedChannelId, memberUserId,
                    ServerPermission.ViewChannels, cancellationToken);

                if (!canView)
                {
                    revokedUserIds.Add(memberUserId);
                }
            }

            if (revokedUserIds.Count > 0)
            {
                await _realtimeNotifier.NotifyChannelAccessRevokedAsync(
                    serverId, affectedChannelId, revokedUserIds,
                    "Bu kanala giriş icazəniz dəyişdirildi.",
                    cancellationToken);
            }
        }
    }

    private async Task ValidateAsync(
        UpdateChannelPermissionOverridesRequestDto request,
        CancellationToken cancellationToken)
    {
        var validationResult = await _validator
            .ValidateAsync(request, cancellationToken);

        if (!validationResult.IsValid)
        {
            throw new ValidationException(validationResult.Errors);
        }
    }

    private async Task<Channel> GetTrackedChannelAsync(int channelId,
        CancellationToken cancellationToken)
    {
        return await _dbContext.Channels
            .FirstOrDefaultAsync(channel =>
                channel.Id == channelId,
                cancellationToken)
            ?? throw new KeyNotFoundException("Kanal tapılmadı.");
    }

    private Task EnsureCanManageChannelAsync(int channelId, int userId,
        CancellationToken cancellationToken)
    {
        return _channelPermissionService.EnsurePermissionAsync(
            channelId, userId, ServerPermission.ManageChannels, cancellationToken);
    }
}
using Discord.Core.DTOs.Admin.Requests;
using Discord.Core.DTOs.Admin.Responses;
using Discord.Core.DTOs.Common;
using Discord.Core.Enums;
using Discord.Core.Exceptions;
using Discord.Core.Interfaces;
using Discord.Core.Models.Admin;
using Discord.Infrastructure.Data;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace Discord.Infrastructure.Services;

public class AdminService : IAdminService
{
    private readonly AppDbContext _dbContext;
    private readonly IValidator<AdminUserListQueryDto> _userListValidator;
    private readonly IValidator<AdminServerListQueryDto> _serverListValidator;
    private readonly IValidator<BanPlatformUserRequestDto> _banUserValidator;

    public AdminService(
        AppDbContext dbContext,
        IValidator<AdminUserListQueryDto> userListValidator,
        IValidator<AdminServerListQueryDto> serverListValidator,
        IValidator<BanPlatformUserRequestDto> banUserValidator)
    {
        _dbContext = dbContext;
        _userListValidator = userListValidator;
        _serverListValidator = serverListValidator;
        _banUserValidator = banUserValidator;
    }

    public async Task<AdminDashboardResponseDto> GetDashboardAsync(
        CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var sevenDaysAgo = now.AddDays(-7);
        var oneDayAgo = now.AddDays(-1);

        return new AdminDashboardResponseDto
        {
            TotalUsers = await _dbContext.Users.AsNoTracking().CountAsync(cancellationToken),
            TotalAdmins = await _dbContext.Users.AsNoTracking()
                .CountAsync(user => user.Role == PlatformRole.Admin, cancellationToken),
            ActiveBannedUsers = await _dbContext.Users.AsNoTracking()
                .CountAsync(user => user.IsBanned &&
                    (!user.BannedUntil.HasValue || user.BannedUntil > now), cancellationToken),
            TotalServers = await _dbContext.Servers.AsNoTracking().CountAsync(cancellationToken),
            PublicServers = await _dbContext.Servers.AsNoTracking()
                .CountAsync(server => server.IsPublic, cancellationToken),
            TotalChannels = await _dbContext.Channels.AsNoTracking().CountAsync(cancellationToken),
            TotalMessages = await _dbContext.Messages.AsNoTracking().CountAsync(cancellationToken),
            TotalConversations = await _dbContext.Conversations.AsNoTracking().CountAsync(cancellationToken),
            TotalAttachments = await _dbContext.MessageAttachments.AsNoTracking().CountAsync(cancellationToken),
            TotalServerMemberships = await _dbContext.ServerMembers.AsNoTracking().CountAsync(cancellationToken),
            UsersCreatedLast7Days = await _dbContext.Users.AsNoTracking()
                .CountAsync(user => user.CreatedAt >= sevenDaysAgo, cancellationToken),
            MessagesCreatedLast24Hours = await _dbContext.Messages.AsNoTracking()
                .CountAsync(message => message.CreatedAt >= oneDayAgo, cancellationToken)
        };
    }

    public async Task<PagedResponseDto<AdminUserListItemResponseDto>> GetUsersAsync(
        AdminUserListQueryDto query,
        CancellationToken cancellationToken = default)
    {
        await ValidateAsync(_userListValidator, query, cancellationToken);

        var now = DateTime.UtcNow;
        var users = _dbContext.Users.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim();
            users = users.Where(user =>
                user.Username.Contains(search) ||
                user.DisplayName.Contains(search) ||
                user.Email.Contains(search));
        }

        if (query.Role.HasValue)
        {
            users = users.Where(user => user.Role == query.Role.Value);
        }

        if (query.IsBanned.HasValue)
        {
            users = query.IsBanned.Value
                ? users.Where(user => user.IsBanned &&
                    (!user.BannedUntil.HasValue || user.BannedUntil > now))
                : users.Where(user => !user.IsBanned ||
                    (user.BannedUntil.HasValue && user.BannedUntil <= now));
        }

        var totalCount = await users.CountAsync(cancellationToken);
        var items = await users
            .OrderByDescending(user => user.CreatedAt)
            .ThenByDescending(user => user.Id)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(user => new AdminUserListItemResponseDto
            {
                Id = user.Id,
                Username = user.Username,
                DisplayName = user.DisplayName,
                Email = user.Email,
                AvatarUrl = user.AvatarUrl,
                Status = user.Status,
                Role = user.Role,
                IsBanned = user.IsBanned &&
                    (!user.BannedUntil.HasValue || user.BannedUntil > now),
                BanReason = user.IsBanned &&
                    (!user.BannedUntil.HasValue || user.BannedUntil > now)
                        ? user.BanReason
                        : null,
                BannedUntil = user.IsBanned &&
                    (!user.BannedUntil.HasValue || user.BannedUntil > now)
                        ? user.BannedUntil
                        : null,
                LastSeenAt = user.LastSeenAt,
                CreatedAt = user.CreatedAt,
                UpdatedAt = user.UpdatedAt
            })
            .ToListAsync(cancellationToken);

        return CreatePagedResponse(items, query.Page, query.PageSize, totalCount);
    }

    public async Task<AdminUserDetailsResponseDto> GetUserByIdAsync(
        int userId,
        CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;

        return await _dbContext.Users
            .AsNoTracking()
            .Where(user => user.Id == userId)
            .Select(user => new AdminUserDetailsResponseDto
            {
                Id = user.Id,
                Username = user.Username,
                DisplayName = user.DisplayName,
                Email = user.Email,
                AvatarUrl = user.AvatarUrl,
                Bio = user.Bio,
                Status = user.Status,
                PreferredStatus = user.PreferredStatus,
                Role = user.Role,
                IsBanned = user.IsBanned &&
                    (!user.BannedUntil.HasValue || user.BannedUntil > now),
                BanReason = user.IsBanned &&
                    (!user.BannedUntil.HasValue || user.BannedUntil > now)
                        ? user.BanReason
                        : null,
                BannedUntil = user.IsBanned &&
                    (!user.BannedUntil.HasValue || user.BannedUntil > now)
                        ? user.BannedUntil
                        : null,
                LastSeenAt = user.LastSeenAt,
                CreatedAt = user.CreatedAt,
                UpdatedAt = user.UpdatedAt,
                OwnedServerCount = user.OwnedServers.Count,
                ServerMembershipCount = user.ServerMemberships.Count,
                MessageCount = user.SentMessages.Count,
                FriendshipCount = _dbContext.Friendships.Count(friendship =>
                    friendship.UserId == user.Id || friendship.FriendId == user.Id)
            })
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new KeyNotFoundException("İstifadəçi tapılmadı.");
    }

    public async Task<AdminUserDetailsResponseDto> BanUserAsync(
        int userId,
        int currentAdminId,
        BanPlatformUserRequestDto request,
        CancellationToken cancellationToken = default)
    {
        await ValidateAsync(_banUserValidator, request, cancellationToken);

        if (userId == currentAdminId)
        {
            throw new BadRequestException("Platform administratoru öz hesabını ban edə bilməz.");
        }

        var user = await _dbContext.Users.FirstOrDefaultAsync(
            user => user.Id == userId,
            cancellationToken)
            ?? throw new KeyNotFoundException("İstifadəçi tapılmadı.");

        if (user.Role == PlatformRole.Admin)
        {
            throw new ForbiddenException("Başqa platform administratoru ban edilə bilməz.");
        }

        var now = DateTime.UtcNow;
        user.IsBanned = true;
        user.BanReason = request.Reason.Trim();
        user.BannedUntil = request.BannedUntil;
        user.Status = UserStatus.Offline;
        user.LastSeenAt = now;
        user.UpdatedAt = now;

        await using var transaction = await _dbContext.Database
            .BeginTransactionAsync(cancellationToken);

        await _dbContext.RefreshTokens
            .Where(token => token.UserId == userId && token.RevokedAt == null)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(token => token.RevokedAt, now)
                .SetProperty(token => token.UpdatedAt, now), cancellationToken);

        await _dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return await GetUserByIdAsync(userId, cancellationToken);
    }

    public async Task UnbanUserAsync(
        int userId,
        CancellationToken cancellationToken = default)
    {
        var user = await _dbContext.Users.FirstOrDefaultAsync(
            user => user.Id == userId,
            cancellationToken)
            ?? throw new KeyNotFoundException("İstifadəçi tapılmadı.");

        if (!user.IsBanned && user.BanReason is null && !user.BannedUntil.HasValue)
        {
            return;
        }

        user.IsBanned = false;
        user.BanReason = null;
        user.BannedUntil = null;
        user.UpdatedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<PagedResponseDto<AdminServerListItemResponseDto>> GetServersAsync(
        AdminServerListQueryDto query,
        CancellationToken cancellationToken = default)
    {
        await ValidateAsync(_serverListValidator, query, cancellationToken);

        var servers = _dbContext.Servers.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim();
            servers = servers.Where(server =>
                server.Name.Contains(search) ||
                (server.Description != null && server.Description.Contains(search)) ||
                server.Owner.Username.Contains(search) ||
                server.Owner.DisplayName.Contains(search));
        }

        if (query.IsPublic.HasValue)
        {
            servers = servers.Where(server => server.IsPublic == query.IsPublic.Value);
        }

        var totalCount = await servers.CountAsync(cancellationToken);
        var items = await servers
            .OrderByDescending(server => server.CreatedAt)
            .ThenByDescending(server => server.Id)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(server => new AdminServerListItemResponseDto
            {
                Id = server.Id,
                Name = server.Name,
                Description = server.Description,
                IconUrl = server.IconUrl,
                IsPublic = server.IsPublic,
                OwnerId = server.OwnerId,
                OwnerUsername = server.Owner.Username,
                OwnerDisplayName = server.Owner.DisplayName,
                MemberCount = server.Members.Count,
                ChannelCount = server.Channels.Count,
                MessageCount = server.Channels.SelectMany(channel => channel.Messages).Count(),
                CreatedAt = server.CreatedAt
            })
            .ToListAsync(cancellationToken);

        return CreatePagedResponse(items, query.Page, query.PageSize, totalCount);
    }

    public async Task<AdminServerDetailsResponseDto> GetServerByIdAsync(
        int serverId,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.Servers
            .AsNoTracking()
            .Where(server => server.Id == serverId)
            .Select(server => new AdminServerDetailsResponseDto
            {
                Id = server.Id,
                Name = server.Name,
                Description = server.Description,
                IconUrl = server.IconUrl,
                BannerUrl = server.BannerUrl,
                IsPublic = server.IsPublic,
                OwnerId = server.OwnerId,
                OwnerUsername = server.Owner.Username,
                OwnerDisplayName = server.Owner.DisplayName,
                OwnerEmail = server.Owner.Email,
                MemberCount = server.Members.Count,
                ChannelCount = server.Channels.Count,
                MessageCount = server.Channels.SelectMany(channel => channel.Messages).Count(),
                InviteCount = server.Invites.Count,
                RoleCount = server.Roles.Count,
                ServerBanCount = _dbContext.ServerBans.Count(serverBan => serverBan.ServerId == server.Id),
                CreatedAt = server.CreatedAt,
                UpdatedAt = server.UpdatedAt
            })
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new KeyNotFoundException("Server tapılmadı.");
    }

    public async Task<AdminServerDeletionResult> DeleteServerAsync(
        int serverId,
        CancellationToken cancellationToken = default)
    {
        await using var transaction = await _dbContext.Database
            .BeginTransactionAsync(cancellationToken);

        var server = await _dbContext.Servers.FirstOrDefaultAsync(
            server => server.Id == serverId,
            cancellationToken)
            ?? throw new KeyNotFoundException("Server tapılmadı.");

        var memberUserIds = await _dbContext.ServerMembers
            .AsNoTracking()
            .Where(member => member.ServerId == serverId)
            .Select(member => member.UserId)
            .Distinct()
            .ToListAsync(cancellationToken);

        var attachmentUrls = await _dbContext.MessageAttachments
            .AsNoTracking()
            .Where(attachment =>
                attachment.Message.Channel != null &&
                attachment.Message.Channel.ServerId == serverId)
            .Select(attachment => attachment.FileUrl)
            .Distinct()
            .ToListAsync(cancellationToken);

        var iconUrl = server.IconUrl;

        _dbContext.Servers.Remove(server);
        await _dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return new AdminServerDeletionResult(
            serverId,
            iconUrl,
            attachmentUrls,
            memberUserIds);
    }

    private static PagedResponseDto<T> CreatePagedResponse<T>(
        IReadOnlyList<T> items,
        int page,
        int pageSize,
        int totalCount)
    {
        return new PagedResponseDto<T>
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount,
            TotalPages = totalCount == 0
                ? 0
                : (int)Math.Ceiling(totalCount / (double)pageSize)
        };
    }

    private static async Task ValidateAsync<T>(
        IValidator<T> validator,
        T value,
        CancellationToken cancellationToken)
    {
        var validationResult = await validator.ValidateAsync(value, cancellationToken);
        if (!validationResult.IsValid)
        {
            throw new ValidationException(validationResult.Errors);
        }
    }
}

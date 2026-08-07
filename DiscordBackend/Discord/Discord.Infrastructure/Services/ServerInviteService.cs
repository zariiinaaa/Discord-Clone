using Discord.Application.Mappings;
using Discord.Core.DTOs.Invites.Requests;
using Discord.Core.DTOs.Invites.Responses;
using Discord.Core.Entities.Servers;
using Discord.Core.Enums;
using Discord.Core.Exceptions;
using Discord.Core.Interfaces;
using Discord.Infrastructure.Data;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;

namespace Discord.Infrastructure.Services;

public class ServerInviteService : IServerInviteService
{
    private readonly AppDbContext _dbContext;
    private readonly IValidator<CreateServerInviteRequestDto> _createInviteValidator;
    private readonly IServerPermissionService _serverPermissionService;
    private readonly IChannelPermissionService _channelPermissionService;

    public ServerInviteService(AppDbContext dbContext,
        IValidator<CreateServerInviteRequestDto> createInviteValidator,
        IServerPermissionService serverPermissionService,
        IChannelPermissionService channelPermissionService)
    {
        _dbContext = dbContext;
        _createInviteValidator = createInviteValidator;
        _serverPermissionService = serverPermissionService;
        _channelPermissionService = channelPermissionService;
    }

    public async Task<ServerInviteResponseDto> CreateAsync(int serverId, int userId,
        CreateServerInviteRequestDto request, CancellationToken cancellationToken = default)
    {
        var validationResult = await _createInviteValidator.ValidateAsync(
            request, cancellationToken);

        if (!validationResult.IsValid)
        {
            throw new ValidationException(validationResult.Errors);
        }

        var server = await _dbContext.Servers.AsNoTracking()
            .FirstOrDefaultAsync(server => server.Id == serverId, cancellationToken)
            ?? throw new KeyNotFoundException("Server tapılmadı.");

        var channel = await _dbContext.Channels.AsNoTracking()
            .FirstOrDefaultAsync(channel =>
                channel.Id == request.ChannelId &&
                channel.ServerId == serverId,
                cancellationToken)
            ?? throw new KeyNotFoundException("Kanal tapılmadı.");

        if (channel.Type == ChannelType.Category)
        {
            throw new BadRequestException(
                "Category üçün dəvət linki yaradıla bilməz.");
        }

        await _channelPermissionService.EnsurePermissionAsync(
            channel.Id, userId, ServerPermission.ViewChannels, cancellationToken);

        await _channelPermissionService.EnsurePermissionAsync(
            channel.Id, userId, ServerPermission.CreateInvites, cancellationToken);

        string code;

        do
        {
            code = GenerateInviteCode();
        }
        while (await _dbContext.ServerInvites.AnyAsync(
            invite => invite.Code == code, cancellationToken));

        var invite = new ServerInvite
        {
            Code = code,
            ServerId = serverId,
            ChannelId = channel.Id,
            CreatedByUserId = userId,
            ExpiresAt = request.ExpirationHours.HasValue
                ? DateTime.UtcNow.AddHours(request.ExpirationHours.Value)
                : null,
            MaxUses = request.MaxUses,
            Uses = 0,
            IsRevoked = false
        };

        _dbContext.ServerInvites.Add(invite);

        await _dbContext.SaveChangesAsync(cancellationToken);

        return invite.ToResponseDto(server.Name, channel.Name);
    }

    public async Task JoinAsync(string code, int userId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            throw new BadRequestException("Dəvət kodu boş ola bilməz.");
        }

        var normalizedCode = code.Trim().ToLowerInvariant();

        await using var transaction = await _dbContext.Database
            .BeginTransactionAsync(
                System.Data.IsolationLevel.Serializable, cancellationToken);

        var invite = await _dbContext.ServerInvites
            .FirstOrDefaultAsync(invite =>
                invite.Code == normalizedCode, cancellationToken)
            ?? throw new KeyNotFoundException("Dəvət tapılmadı.");

        if (invite.IsRevoked)
        {
            throw new BadRequestException("Bu dəvət ləğv edilib.");
        }

        if (invite.ExpiresAt.HasValue &&
            invite.ExpiresAt.Value <= DateTime.UtcNow)
        {
            throw new BadRequestException(
                "Bu dəvətin istifadə müddəti bitib.");
        }

        if (invite.MaxUses.HasValue &&
            invite.Uses >= invite.MaxUses.Value)
        {
            throw new BadRequestException(
                "Bu dəvətin istifadə limiti bitib.");
        }
        var isBanned = await _dbContext.ServerBans
    .AsNoTracking()
    .AnyAsync(
        serverBan =>
            serverBan.ServerId == invite.ServerId &&
            serverBan.UserId == userId,
        cancellationToken);

        if (isBanned)
        {
            throw new ForbiddenException(
                "Bu serverdən ban edildiyiniz üçün dəvətlə qoşula bilməzsiniz.");
        }
        var channelExists = await _dbContext.Channels.AsNoTracking()
            .AnyAsync(channel =>
                channel.Id == invite.ChannelId &&
                channel.ServerId == invite.ServerId,
                cancellationToken);

        if (!channelExists)
        {
            throw new BadRequestException(
                "Dəvətin bağlı olduğu kanal artıq mövcud deyil.");
        }

        var isAlreadyMember = await _dbContext.ServerMembers
            .AnyAsync(member =>
                member.ServerId == invite.ServerId &&
                member.UserId == userId,
                cancellationToken);

        if (isAlreadyMember)
        {
            throw new ConflictException(
                "İstifadəçi artıq bu serverin üzvüdür.");
        }

        var serverMember = new ServerMember
        {
            ServerId = invite.ServerId,
            UserId = userId,
            IsMuted = false,
            IsDeafened = false
        };

        _dbContext.ServerMembers.Add(serverMember);

        invite.Uses++;
        invite.UpdatedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    public async Task<ServerInviteDetailsResponseDto> GetInviteDetailsAsync(
        string code, int userId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            throw new BadRequestException("Dəvət kodu boş ola bilməz.");
        }

        var normalizedCode = code.Trim().ToLowerInvariant();

        var invite = await _dbContext.ServerInvites.AsNoTracking()
            .Include(invite => invite.Server)
            .ThenInclude(server => server.Members)
            .Include(invite => invite.Channel)
            .Include(invite => invite.CreatedByUser)
            .FirstOrDefaultAsync(invite =>
                invite.Code == normalizedCode, cancellationToken)
            ?? throw new KeyNotFoundException("Dəvət tapılmadı.");

        if (invite.IsRevoked)
        {
            throw new BadRequestException("Bu dəvət ləğv edilib.");
        }

        if (invite.ExpiresAt.HasValue &&
            invite.ExpiresAt.Value <= DateTime.UtcNow)
        {
            throw new BadRequestException(
                "Bu dəvətin istifadə müddəti bitib.");
        }

        if (invite.MaxUses.HasValue &&
            invite.Uses >= invite.MaxUses.Value)
        {
            throw new BadRequestException(
                "Bu dəvətin istifadə limiti bitib.");
        }

        var isAlreadyMember = invite.Server.Members
            .Any(member => member.UserId == userId);

        return new ServerInviteDetailsResponseDto
        {
            Code = invite.Code,
            ServerId = invite.ServerId,
            ServerName = invite.Server.Name,
            ServerDescription = invite.Server.Description,
            ServerIconUrl = invite.Server.IconUrl,
            ChannelId = invite.ChannelId,
            ChannelName = invite.Channel.Name,
            MemberCount = invite.Server.Members.Count,
            CreatedByDisplayName = invite.CreatedByUser.DisplayName,
            ExpiresAt = invite.ExpiresAt,
            MaxUses = invite.MaxUses,
            Uses = invite.Uses,
            IsAlreadyMember = isAlreadyMember
        };
    }

    public async Task<IReadOnlyCollection<ServerInviteResponseDto>>
        GetServerInvitesAsync(int serverId, int userId,
        CancellationToken cancellationToken = default)
    {
        var server = await _dbContext.Servers.AsNoTracking()
            .FirstOrDefaultAsync(server => server.Id == serverId, cancellationToken)
            ?? throw new KeyNotFoundException("Server tapılmadı.");

        await _serverPermissionService.EnsurePermissionAsync(
            serverId, userId, ServerPermission.ManageServer, cancellationToken);

        var invites = await _dbContext.ServerInvites.AsNoTracking()
            .Include(invite => invite.Channel)
            .Where(invite =>
                invite.ServerId == serverId &&
                !invite.IsRevoked)
            .OrderByDescending(invite => invite.CreatedAt)
            .ToListAsync(cancellationToken);

        return invites
            .Select(invite =>
                invite.ToResponseDto(server.Name, invite.Channel.Name))
            .ToArray();
    }

    public async Task RevokeAsync(int serverId, int inviteId, int userId,
        CancellationToken cancellationToken = default)
    {
        var invite = await _dbContext.ServerInvites
            .FirstOrDefaultAsync(invite =>
                invite.Id == inviteId &&
                invite.ServerId == serverId,
                cancellationToken)
            ?? throw new KeyNotFoundException("Dəvət tapılmadı.");

        await _serverPermissionService.EnsurePermissionAsync(
            serverId, userId, ServerPermission.ManageServer, cancellationToken);

        if (invite.IsRevoked)
        {
            return;
        }

        invite.IsRevoked = true;
        invite.UpdatedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private static string GenerateInviteCode()
    {
        var randomBytes = RandomNumberGenerator.GetBytes(8);

        return Convert.ToHexString(randomBytes).ToLowerInvariant();
    }
}
using Discord.Application.Mappings;
using Discord.Core.DTOs.Servers.Requests;
using Discord.Core.DTOs.Servers.Responses;
using Discord.Core.Entities.Servers;
using Discord.Core.Enums;
using Discord.Core.Exceptions;
using Discord.Core.Interfaces;
using Discord.Infrastructure.Data;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace Discord.Infrastructure.Services;

public class ServerService : IServerService
{
    private readonly AppDbContext _dbContext;
    private readonly IValidator<CreateServerRequestDto>
        _createServerValidator;
    private readonly IValidator<UpdateServerRequestDto>
    _updateServerValidator;

    public ServerService(AppDbContext dbContext,IValidator<CreateServerRequestDto> createServerValidator,IValidator<UpdateServerRequestDto> updateServerValidator)
    {
        _dbContext = dbContext;
        _createServerValidator = createServerValidator;
        _updateServerValidator = updateServerValidator;
    }

    public async Task<ServerResponseDto> CreateAsync(int ownerId,CreateServerRequestDto request,CancellationToken cancellationToken = default)
    {
        var validationResult =
            await _createServerValidator.ValidateAsync(
                request,
                cancellationToken);

        if (!validationResult.IsValid)
        {
            throw new ValidationException(
                validationResult.Errors);
        }

        var owner = await _dbContext.Users.AsNoTracking().FirstOrDefaultAsync(
                user => user.Id == ownerId,
                cancellationToken)
            ?? throw new KeyNotFoundException(
                "İstifadəçi tapılmadı.");

        if (owner.IsBanned &&
            (owner.BannedUntil is null ||
             owner.BannedUntil > DateTime.UtcNow))
        {
            throw new ForbiddenException(
                "Bloklanmış istifadəçi server yarada bilməz.");
        }

        var server = new Server
        {
            Name = request.Name.Trim(),

            Description =
                string.IsNullOrWhiteSpace(request.Description)
                    ? null
                    : request.Description.Trim(),

            IsPublic = request.IsPublic,
            OwnerId = ownerId
        };

        var ownerMember = new ServerMember
        {
            UserId = ownerId,
            Server = server
        };

        var generalCategory = new Channel
        {
            Name = "GENERAL",
            Type = ChannelType.Category,
            Position = 0,
            Server = server
        };

        var generalTextChannel = new Channel
        {
            Name = "general",
            Type = ChannelType.Text,
            Position = 0,
            Server = server,
            ParentCategory = generalCategory
        };

        var generalVoiceChannel = new Channel
        {
            Name = "General",
            Type = ChannelType.Voice,
            Position = 1,
            Bitrate = 64000,
            UserLimit = 0,
            Server = server,
            ParentCategory = generalCategory
        };

        server.Members.Add(ownerMember);

        server.Channels.Add(generalCategory);
        server.Channels.Add(generalTextChannel);
        server.Channels.Add(generalVoiceChannel);

        _dbContext.Servers.Add(server);

        await _dbContext.SaveChangesAsync(
            cancellationToken);

        return server.ToResponseDto();
    }

    public async Task<IReadOnlyList<ServerResponseDto>>
    GetMyServersAsync(
        int userId,
        CancellationToken cancellationToken = default)
    {
        var servers = await _dbContext.ServerMembers
            .AsNoTracking()
            .Where(member => member.UserId == userId)
            .Select(member => member.Server)
            .OrderBy(server => server.Name)
            .ToListAsync(cancellationToken);

        return servers
            .Select(server => server.ToResponseDto())
            .ToList();
    }


    public async Task<ServerDetailsResponseDto> GetByIdAsync(
    int serverId,
    int userId,
    CancellationToken cancellationToken = default)
    {
        var server = await _dbContext.Servers
            .AsNoTracking()
            .AsSplitQuery()
            .Include(server => server.Members)
            .Include(server => server.Channels)
            .FirstOrDefaultAsync(
                server => server.Id == serverId,
                cancellationToken)
            ?? throw new KeyNotFoundException(
                "Server tapılmadı.");

        var isMember = server.Members
            .Any(member => member.UserId == userId);

        if (!isMember)
        {
            throw new ForbiddenException(
                "Bu serverin məlumatlarını görmək icazəniz yoxdur.");
        }

        return server.ToDetailsResponseDto();
    }

    public async Task<ServerResponseDto> UpdateAsync(
    int serverId,
    int userId,
    UpdateServerRequestDto request,
    CancellationToken cancellationToken = default)
    {
        var validationResult =
            await _updateServerValidator.ValidateAsync(
                request,
                cancellationToken);

        if (!validationResult.IsValid)
        {
            throw new ValidationException(
                validationResult.Errors);
        }

        var server = await _dbContext.Servers
            .FirstOrDefaultAsync(
                server => server.Id == serverId,
                cancellationToken)
            ?? throw new KeyNotFoundException(
                "Server tapılmadı.");

        if (server.OwnerId != userId)
        {
            throw new ForbiddenException(
                "Yalnız server sahibi bu məlumatları dəyişə bilər.");
        }

        server.Name = request.Name.Trim();

        server.Description =
            string.IsNullOrWhiteSpace(request.Description)
                ? null
                : request.Description.Trim();

        server.IsPublic = request.IsPublic;
        server.UpdatedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync(
            cancellationToken);

        return server.ToResponseDto();
    }



    public async Task DeleteAsync(int serverId,int userId,CancellationToken cancellationToken = default)
    {
        var server = await _dbContext.Servers
            .FirstOrDefaultAsync(
                server => server.Id == serverId,
                cancellationToken)
            ?? throw new KeyNotFoundException(
                "Server tapılmadı.");

        if (server.OwnerId != userId)
        {
            throw new ForbiddenException(
                "Yalnız server sahibi serveri silə bilər.");
        }

        _dbContext.Servers.Remove(server);

        await _dbContext.SaveChangesAsync(
            cancellationToken);
    }


    public async Task<ServerResponseDto> UpdateIconAsync(int serverId,int userId,string iconUrl,
    CancellationToken cancellationToken = default)
    {
        var server = await _dbContext.Servers
            .FirstOrDefaultAsync(
                server => server.Id == serverId,
                cancellationToken)
            ?? throw new KeyNotFoundException(
                "Server tapılmadı.");

        if (server.OwnerId != userId)
        {
            throw new ForbiddenException(
                "Yalnız server sahibi server iconunu dəyişə bilər.");
        }

        server.IconUrl = iconUrl;
        server.UpdatedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync(
            cancellationToken);

        return server.ToResponseDto();
    }
}
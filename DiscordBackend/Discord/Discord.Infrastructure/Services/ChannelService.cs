using Discord.Application.Mappings;
using Discord.Core.DTOs.Channels.Requests;
using Discord.Core.DTOs.Channels.Responses;
using Discord.Core.Entities.Servers;
using Discord.Core.Enums;
using Discord.Core.Exceptions;
using Discord.Core.Interfaces;
using Discord.Infrastructure.Data;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace Discord.Infrastructure.Services;

public class ChannelService : IChannelService
{
    private readonly AppDbContext _dbContext;


    private readonly IValidator<CreateChannelRequestDto>_createChannelValidator;

    private readonly IValidator<UpdateChannelRequestDto>_updateChannelValidator;

    public ChannelService(AppDbContext dbContext,IValidator<CreateChannelRequestDto> createChannelValidator,
        IValidator<UpdateChannelRequestDto> updateChannelValidator)
    {
        _dbContext = dbContext;
        _createChannelValidator = createChannelValidator;
        _updateChannelValidator = updateChannelValidator;
    }

    public async Task<IReadOnlyList<ChannelResponseDto>> GetByServerAsync(int serverId,int userId,
    CancellationToken cancellationToken = default)
    {
        var serverExists = await _dbContext.Servers
            .AsNoTracking()
            .AnyAsync( server => server.Id == serverId,cancellationToken);

        if (!serverExists)
        {
            throw new KeyNotFoundException(
                "Server tapılmadı.");
        }

        var isMember = await _dbContext.ServerMembers
            .AsNoTracking()
            .AnyAsync( member => member.ServerId == serverId &&member.UserId == userId,
              cancellationToken);

        if (!isMember)
        {
            throw new ForbiddenException( "Yalnız server üzvləri kanalları görə bilər.");
        }

        var channels = await _dbContext.Channels
            .AsNoTracking()
            .Where(channel => channel.ServerId == serverId)
            .OrderBy(channel => channel.ParentCategoryId)
            .ThenBy(channel => channel.Position)
            .ToListAsync(cancellationToken);

        return channels
            .Select(channel => channel.ToResponseDto())
            .ToList();
    }

    public async Task<ChannelResponseDto> CreateAsync(int serverId,int userId,CreateChannelRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var validationResult =
            await _createChannelValidator.ValidateAsync(
                request,
                cancellationToken);

        if (!validationResult.IsValid)
        {
            throw new ValidationException(
                validationResult.Errors);
        }

        var server = await _dbContext.Servers
            .AsNoTracking()
            .FirstOrDefaultAsync(
                server => server.Id == serverId,
                cancellationToken)
            ?? throw new KeyNotFoundException(
                "Server tapılmadı.");

        if (server.OwnerId != userId)
        {
            throw new ForbiddenException(
                "Yalnız server sahibi kanal yarada bilər.");
        }

        var parentCategoryId =
            request.Type == ChannelType.Category
                ? null
                : request.ParentCategoryId;

        if (parentCategoryId.HasValue)
        {
            var parentCategory =
                await _dbContext.Channels
                    .AsNoTracking()
                    .FirstOrDefaultAsync(
                        channel =>
                            channel.Id == parentCategoryId.Value &&
                            channel.ServerId == serverId,
                        cancellationToken)
                ?? throw new BadRequestException(
                    "Seçilmiş category tapılmadı.");

            if (parentCategory.Type != ChannelType.Category)
            {
                throw new BadRequestException(
                    "Parent channel mütləq category olmalıdır.");
            }
        }

        var maximumPosition =
            await _dbContext.Channels
                .Where(channel =>
                    channel.ServerId == serverId &&
                    channel.ParentCategoryId ==
                        parentCategoryId)
                .MaxAsync(
                    channel => (int?)channel.Position,
                    cancellationToken)
            ?? -1;

        var channel = new Channel
        {
            Name = request.Name.Trim(),

            Topic =
                request.Type == ChannelType.Text &&
                !string.IsNullOrWhiteSpace(request.Topic)
                    ? request.Topic.Trim()
                    : null,

            Type = request.Type,
            IsPrivate = request.IsPrivate,
            ServerId = serverId,
            ParentCategoryId = parentCategoryId,
            Position = maximumPosition + 1,

            Bitrate =
                request.Type == ChannelType.Voice
                    ? request.Bitrate ?? 64000
                    : null,

            UserLimit =
                request.Type == ChannelType.Voice
                    ? request.UserLimit ?? 0
                    : null
        };

        _dbContext.Channels.Add(channel);

        await _dbContext.SaveChangesAsync(
            cancellationToken);

        return channel.ToResponseDto();
    }

    public async Task<ChannelResponseDto> UpdateAsync(int serverId,int channelId, int userId,UpdateChannelRequestDto request,CancellationToken cancellationToken = default)
    {
        var validationResult =
            await _updateChannelValidator.ValidateAsync(
                request,
                cancellationToken);

        if (!validationResult.IsValid)
        {
            throw new ValidationException(
                validationResult.Errors);
        }

        var channel = await _dbContext.Channels
            .Include(channel => channel.Server)
            .FirstOrDefaultAsync(
                channel =>
                    channel.Id == channelId &&
                    channel.ServerId == serverId,
                cancellationToken)
            ?? throw new KeyNotFoundException(
                "Kanal tapılmadı.");

        if (channel.Server.OwnerId != userId)
        {
            throw new ForbiddenException(
                "Yalnız server sahibi kanalı dəyişə bilər.");
        }

        if (channel.Type == ChannelType.Category &&
            request.ParentCategoryId.HasValue)
        {
            throw new BadRequestException(
                "Category başqa category daxilində ola bilməz.");
        }

        var newParentCategoryId =
            channel.Type == ChannelType.Category
                ? null
                : request.ParentCategoryId;

        if (newParentCategoryId.HasValue)
        {
            if (newParentCategoryId.Value == channelId)
            {
                throw new BadRequestException(
                    "Kanal öz parent category-si ola bilməz.");
            }

            var parentCategory = await _dbContext.Channels
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    parent =>
                        parent.Id == newParentCategoryId.Value &&
                        parent.ServerId == serverId,
                    cancellationToken)
                ?? throw new BadRequestException(
                    "Seçilmiş category tapılmadı.");

            if (parentCategory.Type != ChannelType.Category)
            {
                throw new BadRequestException(
                    "Parent channel mütləq category olmalıdır.");
            }
        }

        if (channel.Type != ChannelType.Text &&
            !string.IsNullOrWhiteSpace(request.Topic))
        {
            throw new BadRequestException(
                "Topic yalnız text channel üçün yazıla bilər.");
        }

        if (channel.Type != ChannelType.Voice &&
            (request.Bitrate.HasValue ||
             request.UserLimit.HasValue))
        {
            throw new BadRequestException(
                "Bitrate və user limit yalnız voice channel üçündür.");
        }

        if (channel.ParentCategoryId != newParentCategoryId)
        {
            var maximumPosition =
                await _dbContext.Channels
                    .Where(other =>
                        other.ServerId == serverId &&
                        other.ParentCategoryId ==
                            newParentCategoryId &&
                        other.Id != channelId)
                    .MaxAsync(
                        other => (int?)other.Position,
                        cancellationToken)
                ?? -1;

            channel.Position = maximumPosition + 1;
        }

        channel.Name = request.Name.Trim();

        channel.Topic =
            channel.Type == ChannelType.Text &&
            !string.IsNullOrWhiteSpace(request.Topic)
                ? request.Topic.Trim()
                : null;

        channel.IsPrivate = request.IsPrivate;
        channel.ParentCategoryId = newParentCategoryId;

        channel.Bitrate =
            channel.Type == ChannelType.Voice
                ? request.Bitrate ?? channel.Bitrate ?? 64000
                : null;

        channel.UserLimit =
            channel.Type == ChannelType.Voice
                ? request.UserLimit ?? channel.UserLimit ?? 0
                : null;

        channel.UpdatedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync(
            cancellationToken);

        return channel.ToResponseDto();
    }


    public async Task DeleteAsync(int serverId,int channelId,int userId,
    CancellationToken cancellationToken = default)
    {
        var channel = await _dbContext.Channels
            .Include(channel => channel.Server)
            .FirstOrDefaultAsync(
                channel =>
                    channel.Id == channelId &&
                    channel.ServerId == serverId,
                cancellationToken)
            ?? throw new KeyNotFoundException(
                "Kanal tapılmadı.");

        if (channel.Server.OwnerId != userId)
        {
            throw new ForbiddenException(
                "Yalnız server sahibi kanal silə bilər.");
        }

        await using var transaction =
            await _dbContext.Database.BeginTransactionAsync(
                cancellationToken);

        if (channel.Type == ChannelType.Category)
        {
            var childChannels = await _dbContext.Channels
                .Where(child =>
                    child.ServerId == serverId &&
                    child.ParentCategoryId == channelId)
                .OrderBy(child => child.Position)
                .ToListAsync(cancellationToken);

            var maximumRootPosition = await _dbContext.Channels
                .Where(root =>
                    root.ServerId == serverId &&
                    root.ParentCategoryId == null &&
                    root.Id != channelId)
                .MaxAsync(
                    root => (int?)root.Position,
                    cancellationToken) ?? -1;

            foreach (var child in childChannels)
            {
                child.ParentCategoryId = null;
                child.Position = ++maximumRootPosition;
                child.UpdatedAt = DateTime.UtcNow;
            }

            await _dbContext.SaveChangesAsync(
                cancellationToken);
        }

        _dbContext.Channels.Remove(channel);

        await _dbContext.SaveChangesAsync(
            cancellationToken);

        await transaction.CommitAsync(
            cancellationToken);
    }
}
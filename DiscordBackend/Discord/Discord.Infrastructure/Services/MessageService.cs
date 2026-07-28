using Discord.Application.Mappings;
using Discord.Core.DTOs.Messages.Requests;
using Discord.Core.DTOs.Messages.Responses;
using Discord.Core.Entities.Messages;
using Discord.Core.Entities.Servers;
using Discord.Core.Enums;
using Discord.Core.Exceptions;
using Discord.Core.Interfaces;
using Discord.Infrastructure.Data;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace Discord.Infrastructure.Services;

public class MessageService : IMessageService
{
    private readonly AppDbContext _dbContext;

    private readonly IValidator<CreateMessageRequestDto> _createMessageValidator;

    private readonly IValidator<UpdateMessageRequestDto> _updateMessageValidator;

    private readonly IChannelAccessService _channelAccessService;

    public MessageService(AppDbContext dbContext,IValidator<CreateMessageRequestDto>createMessageValidator,
        IValidator<UpdateMessageRequestDto>
            updateMessageValidator, IChannelAccessService channelAccessService)
    {
        _dbContext = dbContext;
        _createMessageValidator = createMessageValidator;
        _updateMessageValidator = updateMessageValidator;
        _channelAccessService = channelAccessService;
    }

    public async Task<
        IReadOnlyCollection<MessageResponseDto>>
        GetChannelMessagesAsync(
            int channelId,
            int userId,
            int? beforeMessageId = null,
            int limit = 50,
            CancellationToken cancellationToken = default)
    {
        await _channelAccessService.GetAccessibleTextChannelAsync(channelId,userId,
            cancellationToken);

        if (limit is < 1 or > 100)
        {
            throw new BadRequestException( "Limit 1–100 arasında olmalıdır.");
        }

        if (beforeMessageId.HasValue &&
            beforeMessageId.Value <= 0)
        {
            throw new BadRequestException("Mesaj ID-si düzgün deyil.");
        }

        var query = _dbContext.Messages.AsNoTracking().Include(message => message.Author)
            .Where(message =>
                message.ChannelId == channelId);

        if (beforeMessageId.HasValue)
        {
            query = query.Where(message =>
                message.Id < beforeMessageId.Value);
        }

        var messages = await query
            .OrderByDescending(message => message.Id)
            .Take(limit)
            .ToListAsync(cancellationToken);

        messages.Reverse();

        return messages
            .Select(message => message.ToResponseDto())
            .ToList();
    }

    public async Task<MessageResponseDto> CreateAsync(int channelId,int userId,
        CreateMessageRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var validationResult =
            await _createMessageValidator.ValidateAsync(request,
                cancellationToken);

        if (!validationResult.IsValid)
        {
            throw new ValidationException(validationResult.Errors);
        }

        await _channelAccessService.GetAccessibleTextChannelAsync(channelId,userId,cancellationToken);

        if (request.ReplyToMessageId.HasValue)
        {
            var replyMessageExists =
                await _dbContext.Messages.AnyAsync(
                    message =>
                        message.Id ==
                            request.ReplyToMessageId.Value &&
                        message.ChannelId == channelId,
                    cancellationToken);

            if (!replyMessageExists)
            {
                throw new BadRequestException(
                    "Cavab verilən mesaj bu kanalda tapılmadı.");
            }
        }

        var message = new Message
        {
            Content = request.Content.Trim(),
            ChannelId = channelId,
            AuthorId = userId,
            ReplyToMessageId = request.ReplyToMessageId,
            IsPinned = false
        };

        _dbContext.Messages.Add(message);

        await _dbContext.SaveChangesAsync(
            cancellationToken);

        message.Author = await _dbContext.Users
            .AsNoTracking()
            .FirstAsync(
                user => user.Id == userId,
                cancellationToken);

        return message.ToResponseDto();
    }

    public async Task<MessageResponseDto> UpdateAsync(int channelId,int messageId,
        int userId,
        UpdateMessageRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var validationResult =
            await _updateMessageValidator.ValidateAsync(
                request,
                cancellationToken);

        if (!validationResult.IsValid)
        {
            throw new ValidationException(
                validationResult.Errors);
        }

        await _channelAccessService.GetAccessibleTextChannelAsync(channelId,userId,
            cancellationToken);

        var message = await _dbContext.Messages
            .Include(message => message.Author)
            .FirstOrDefaultAsync(
                message =>
                    message.Id == messageId &&
                    message.ChannelId == channelId,
                cancellationToken)
            ?? throw new KeyNotFoundException(
                "Mesaj tapılmadı.");

        if (message.AuthorId != userId)
        {
            throw new ForbiddenException(
                "Yalnız mesaj müəllifi mesajı redaktə edə bilər.");
        }

        message.Content = request.Content.Trim();
        message.EditedAt = DateTime.UtcNow;
        message.UpdatedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync( cancellationToken);

        return message.ToResponseDto();
    }

    public async Task DeleteAsync(int channelId,int messageId, int userId,
        CancellationToken cancellationToken = default)
    {
        var channel = await _channelAccessService.GetAccessibleTextChannelAsync(channelId,userId,cancellationToken);

        var message = await _dbContext.Messages
            .FirstOrDefaultAsync(
                message =>
                    message.Id == messageId &&
                    message.ChannelId == channelId,
                cancellationToken)
            ?? throw new KeyNotFoundException(
                "Mesaj tapılmadı.");

        var canDelete =
            message.AuthorId == userId ||
            channel.Server.OwnerId == userId;

        if (!canDelete)
        {
            throw new ForbiddenException( "Bu mesajı silmək icazəniz yoxdur.");
        }

        await using var transaction =
            await _dbContext.Database.BeginTransactionAsync(
                cancellationToken);

        var replies = await _dbContext.Messages
            .Where(reply =>
                reply.ReplyToMessageId == messageId)
            .ToListAsync(cancellationToken);

        if (replies.Count > 0)
        {
            foreach (var reply in replies)
            {
                reply.ReplyToMessageId = null;
                reply.UpdatedAt = DateTime.UtcNow;
            }

            await _dbContext.SaveChangesAsync(
                cancellationToken);
        }

        _dbContext.Messages.Remove(message);

        await _dbContext.SaveChangesAsync(
            cancellationToken);

        await transaction.CommitAsync(
            cancellationToken);
    }

    
}
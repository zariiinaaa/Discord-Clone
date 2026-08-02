using Discord.Application.Mappings;
using Discord.Core.DTOs.Messages.Requests;
using Discord.Core.DTOs.Messages.Responses;
using Discord.Core.Entities.Conversations;
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
    private readonly IConversationAccessService _conversationAccessService;

    public MessageService(AppDbContext dbContext,IValidator<CreateMessageRequestDto>createMessageValidator,
    IValidator<UpdateMessageRequestDto>updateMessageValidator,IChannelAccessService channelAccessService,
    IConversationAccessService conversationAccessService)
    {
        _dbContext = dbContext;
        _createMessageValidator = createMessageValidator;
        _updateMessageValidator =updateMessageValidator;
        _channelAccessService =channelAccessService;
        _conversationAccessService =conversationAccessService;
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


    public async Task<
    IReadOnlyCollection<MessageResponseDto>>
    GetConversationMessagesAsync(
        int conversationId,
        int userId,
        int? beforeMessageId = null,
        int limit = 50,
        CancellationToken cancellationToken = default)
    {
        await _conversationAccessService
            .GetAccessibleConversationAsync(
                conversationId,
                userId,
                cancellationToken);

        if (limit is < 1 or > 100)
        {
            throw new BadRequestException(
                "Limit 1–100 arasında olmalıdır.");
        }

        if (beforeMessageId.HasValue &&
            beforeMessageId.Value <= 0)
        {
            throw new BadRequestException(
                "Mesaj ID-si düzgün deyil.");
        }

        var query = _dbContext.Messages
            .AsNoTracking()
            .Include(message => message.Author)
            .Where(message =>
                message.ConversationId ==
                conversationId);

        if (beforeMessageId.HasValue)
        {
            query = query.Where(message =>
                message.Id <
                beforeMessageId.Value);
        }

        var messages = await query
            .OrderByDescending(message =>
                message.Id)
            .Take(limit)
            .ToListAsync(cancellationToken);

        messages.Reverse();

        return messages
            .Select(message =>
                message.ToResponseDto())
            .ToList();
    }


    public async Task<MessageResponseDto>CreateConversationMessageAsync(int conversationId, int userId,
        CreateMessageRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var validationResult =
            await _createMessageValidator
                .ValidateAsync(
                    request,
                    cancellationToken);

        if (!validationResult.IsValid)
        {
            throw new ValidationException(
                validationResult.Errors);
        }

        var conversation =
            await _conversationAccessService
                .GetAccessibleConversationAsync(
                    conversationId,
                    userId,
                    cancellationToken);

        if (request.ReplyToMessageId.HasValue)
        {
            var replyMessageExists =
                await _dbContext.Messages
                    .AnyAsync(
                        message =>
                            message.Id ==
                                request
                                    .ReplyToMessageId
                                    .Value &&

                            message.ConversationId ==
                                conversationId,
                        cancellationToken);

            if (!replyMessageExists)
            {
                throw new BadRequestException(
                    "Cavab verilən mesaj bu conversation-da tapılmadı.");
            }
        }

        await using var transaction =await _dbContext.Database.BeginTransactionAsync(
                    cancellationToken);

        if (
            conversation.Type ==ConversationType.Direct
        )
        {
            await PrepareDirectMessageRequestAsync(
                conversation,
                userId,
                cancellationToken);

            
            var conversationMembers = await _dbContext
                    .ConversationMembers
                    .Where(member =>
                        member.ConversationId ==
                            conversationId)
                    .ToListAsync(
                        cancellationToken);

            foreach (
                var conversationMember
                in conversationMembers)
            {
                conversationMember
                    .IsVisibleInList = true;
            }
        }

        var message = new Message
        {
            Content =
                request.Content.Trim(),

            ChannelId = null,

            ConversationId =
                conversationId,

            AuthorId = userId,

            ReplyToMessageId =
                request.ReplyToMessageId,

            IsPinned = false
        };

        _dbContext.Messages.Add(
            message);

        await _dbContext.SaveChangesAsync(
            cancellationToken);

        await transaction.CommitAsync(
            cancellationToken);

        message.Author =
            await _dbContext.Users
                .AsNoTracking()
                .FirstAsync(
                    user =>
                        user.Id == userId,
                    cancellationToken);

        return message.ToResponseDto();
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


    public async Task<MessageResponseDto>UpdateConversationMessageAsync(int conversationId, int messageId, int userId,
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

        await _conversationAccessService
            .GetAccessibleConversationAsync(
                conversationId,
                userId,
                cancellationToken);

        var message = await _dbContext.Messages
            .Include(message => message.Author)
            .FirstOrDefaultAsync(
                message =>
                    message.Id == messageId &&
                    message.ConversationId ==
                        conversationId,
                cancellationToken)
            ?? throw new KeyNotFoundException(
                "Mesaj tapılmadı.");

        if (message.AuthorId != userId)
        {
            throw new ForbiddenException(
                "Yalnız mesaj müəllifi mesajı redaktə edə bilər.");
        }

        var currentTime = DateTime.UtcNow;

        message.Content = request.Content.Trim();
        message.EditedAt = currentTime;
        message.UpdatedAt = currentTime;

        await _dbContext.SaveChangesAsync(
            cancellationToken);

        return message.ToResponseDto();
    }

    public async Task DeleteConversationMessageAsync(
        int conversationId,
        int messageId,
        int userId,
        CancellationToken cancellationToken = default)
    {
        await _conversationAccessService
            .GetAccessibleConversationAsync(
                conversationId,
                userId,
                cancellationToken);

        var message = await _dbContext.Messages
            .FirstOrDefaultAsync(
                message =>
                    message.Id == messageId &&
                    message.ConversationId ==
                        conversationId,
                cancellationToken)
            ?? throw new KeyNotFoundException(
                "Mesaj tapılmadı.");

        if (message.AuthorId != userId)
        {
            throw new ForbiddenException(
                "Yalnız mesaj müəllifi mesajı silə bilər.");
        }

        await using var transaction =
            await _dbContext.Database
                .BeginTransactionAsync(
                    cancellationToken);

        var replies = await _dbContext.Messages
            .Where(reply =>
                reply.ReplyToMessageId == messageId)
            .ToListAsync(cancellationToken);

        if (replies.Count > 0)
        {
            var currentTime = DateTime.UtcNow;

            foreach (var reply in replies)
            {
                reply.ReplyToMessageId = null;
                reply.UpdatedAt = currentTime;
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

    private async Task PrepareDirectMessageRequestAsync(
    Conversation conversation,
    int senderId,
    CancellationToken cancellationToken)
    {
        var memberIds = conversation.Members
            .Select(member => member.UserId)
            .Distinct()
            .ToList();

        if (memberIds.Count != 2)
        {
            throw new BadRequestException(
                "Direct conversation düzgün qurulmayıb.");
        }

        var recipientId = memberIds.Single(
            memberId => memberId != senderId);

        var isBlocked =
            await _dbContext.UserBlocks
                .AsNoTracking()
                .AnyAsync(
                    block =>
                        (block.BlockerId == senderId &&
                         block.BlockedUserId == recipientId) ||
                        (block.BlockerId == recipientId &&
                         block.BlockedUserId == senderId),
                    cancellationToken);

        if (isBlocked)
        {
            throw new ForbiddenException(
                "Bu istifadəçiyə mesaj göndərmək mümkün deyil.");
        }

        var areFriends =
            await _dbContext.Friendships
                .AsNoTracking()
                .AnyAsync(
                    friendship =>
                        (friendship.UserId == senderId &&
                         friendship.FriendId == recipientId) ||
                        (friendship.UserId == recipientId &&
                         friendship.FriendId == senderId),
                    cancellationToken);

        if (areFriends)
        {
            return;
        }

    
        var acceptedRequestExists =
            await _dbContext.DirectMessageRequests
                .AsNoTracking()
                .AnyAsync(
                    messageRequest =>
                        messageRequest.ConversationId ==
                            conversation.Id &&
                        messageRequest.Status ==
                            MessageRequestStatus.Accepted,
                    cancellationToken);

        if (acceptedRequestExists)
        {
            return;
        }

        
        var incomingRequest =
            await _dbContext.DirectMessageRequests
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    messageRequest =>
                        messageRequest.ConversationId ==
                            conversation.Id &&
                        messageRequest.SenderId ==
                            recipientId &&
                        messageRequest.RecipientId ==
                            senderId,
                    cancellationToken);

        if (incomingRequest is not null)
        {
            throw new ForbiddenException(
                "Cavab yazmaq üçün əvvəlcə mesaj sorğusunu qəbul edin.");
        }

        var outgoingRequest =
            await _dbContext.DirectMessageRequests
                .FirstOrDefaultAsync(
                    messageRequest =>
                        messageRequest.ConversationId ==
                            conversation.Id &&
                        messageRequest.SenderId ==
                            senderId &&
                        messageRequest.RecipientId ==
                            recipientId,
                    cancellationToken);

        
        if (outgoingRequest?.Status ==
            MessageRequestStatus.Pending)
        {
            return;
        }

        if (outgoingRequest?.Status ==
            MessageRequestStatus.Spam)
        {
            throw new ForbiddenException(
                "Bu istifadəçiyə mesaj göndərmək mümkün deyil.");
        }

        var privacySettings =
            await _dbContext.UserPrivacySettings
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    settings =>
                        settings.UserId == recipientId,
                    cancellationToken);

       
        var allowServerMemberMessages =
            privacySettings?
                .AllowDirectMessagesFromServerMembers
            ?? true;

        var enableMessageRequests =
            privacySettings?
                .EnableMessageRequests
            ?? true;

        var sharedServerSettings =
            await (
                from recipientMembership
                    in _dbContext.ServerMembers
                        .AsNoTracking()
                join senderMembership
                    in _dbContext.ServerMembers
                        .AsNoTracking()
                    on recipientMembership.ServerId
                    equals senderMembership.ServerId
                where
                    recipientMembership.UserId ==
                        recipientId &&
                    senderMembership.UserId ==
                        senderId
                select new
                {
                    recipientMembership
                        .AllowDirectMessages,

                    recipientMembership
                        .EnableMessageRequests
                })
                .ToListAsync(cancellationToken);

        var allowedSharedServers =
            sharedServerSettings
                .Where(settings =>
                    settings.AllowDirectMessages ??
                    allowServerMemberMessages)
                .ToList();

        if (allowedSharedServers.Count == 0)
        {
            throw new ForbiddenException(
                "Bu istifadəçi ortaq server üzvlərindən DM qəbul etmir.");
        }

        var requiresMessageRequest =
            allowedSharedServers.All(settings =>
                settings.EnableMessageRequests ??
                enableMessageRequests);

        if (!requiresMessageRequest)
        {
            return;
        }

        if (outgoingRequest is null)
        {
            _dbContext.DirectMessageRequests.Add(
                new DirectMessageRequest
                {
                    ConversationId = conversation.Id,
                    SenderId = senderId,
                    RecipientId = recipientId,
                    Status = MessageRequestStatus.Pending
                });

            return;
        }

        
        outgoingRequest.Status =
            MessageRequestStatus.Pending;

        outgoingRequest.RespondedAt = null;
        outgoingRequest.UpdatedAt = DateTime.UtcNow;
    }
}
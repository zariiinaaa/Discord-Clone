using Discord.Application.Mappings;
using Discord.Core.DTOs.Conversations.Requests;
using Discord.Core.DTOs.Conversations.Responses;
using Discord.Core.Entities.Conversations;
using Discord.Core.Enums;
using Discord.Core.Exceptions;
using Discord.Core.Interfaces;
using Discord.Infrastructure.Data;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace Discord.Infrastructure.Services;

public class ConversationService : IConversationService
{
    private readonly AppDbContext _dbContext;

    private readonly IValidator<CreateDirectConversationRequestDto> _directConversationValidator;

    private readonly IValidator<CreateGroupConversationRequestDto>  _groupConversationValidator;

    private readonly IValidator<AddGroupMemberRequestDto> _addGroupMemberValidator;

    private readonly IValidator<UpdateGroupConversationRequestDto> _updateGroupConversationValidator;

    public ConversationService( AppDbContext dbContext,IValidator<CreateDirectConversationRequestDto>directConversationValidator,
        IValidator<CreateGroupConversationRequestDto>groupConversationValidator,
        IValidator<AddGroupMemberRequestDto>addGroupMemberValidator,
        IValidator<UpdateGroupConversationRequestDto>  updateGroupConversationValidator)
    {
        _dbContext = dbContext;
        _directConversationValidator = directConversationValidator;
        _groupConversationValidator = groupConversationValidator;
        _addGroupMemberValidator =addGroupMemberValidator;
        _updateGroupConversationValidator =updateGroupConversationValidator;
    }

    public async Task<IReadOnlyCollection<ConversationResponseDto>>GetMyConversationsAsync(int userId,
        CancellationToken cancellationToken =default)
    {
        var conversations = await _dbContext.Conversations .AsNoTracking().Include(conversation =>
                    conversation.Members)
                .ThenInclude(member =>
                    member.User)
                .Where(conversation =>
                    conversation.Members.Any(
                        member =>
                            member.UserId ==
                                userId &&

                            member
                                .IsVisibleInList) &&

                    !_dbContext
                        .DirectMessageRequests
                        .Any(messageRequest =>
                            messageRequest.ConversationId ==conversation.Id &&

                            messageRequest.RecipientId ==
                            userId &&

                            messageRequest.Status !=
                            MessageRequestStatus
                                .Accepted))
                .OrderByDescending(conversation =>
                    conversation.UpdatedAt)
                .ThenByDescending(conversation =>
                    conversation.CreatedAt)
                .ToListAsync(
                    cancellationToken);

        var unreadCounts =
            await (
                from message in
                    _dbContext.Messages
                        .AsNoTracking()

                join membership in
                    _dbContext.ConversationMembers
                        .AsNoTracking()

                on message.ConversationId
                    equals
                    (int?)membership
                        .ConversationId

                where
                    membership.UserId ==
                        userId &&

                    membership
                        .IsVisibleInList &&

                    message.AuthorId !=
                        userId &&

                    (
                        membership
                            .LastReadMessageId ==
                        null ||

                        message.Id >
                        membership
                            .LastReadMessageId
                            .Value
                    )

                group message by
                    membership.ConversationId
                into messageGroup

                select new
                {
                    ConversationId =
                        messageGroup.Key,

                    Count =
                        messageGroup.Count()
                }
            )
            .ToDictionaryAsync(
                item =>
                    item.ConversationId,
                item =>
                    item.Count,
                cancellationToken);

        return conversations
            .Select(conversation =>
                conversation.ToResponseDto(
                    unreadCounts
                        .GetValueOrDefault(
                            conversation.Id)))
            .ToArray();
    }

    public async Task<ConversationResponseDto>GetByIdAsync(int conversationId, int userId,
            CancellationToken cancellationToken = default)
    {
        if (conversationId <= 0)
        {
            throw new BadRequestException(
                "Söhbət ID-si düzgün deyil.");
        }

        var conversation =await _dbContext.Conversations
                .AsNoTracking()
                .Include(item =>
                    item.Members)
                .ThenInclude(member =>
                    member.User)
                .FirstOrDefaultAsync(
                    item =>
                        item.Id == conversationId &&
                        item.Members.Any(member =>
                            member.UserId == userId),
                    cancellationToken)
            ?? throw new KeyNotFoundException(
                "Söhbət tapılmadı və ya bu söhbətin üzvü deyilsiniz.");

        return conversation.ToResponseDto();
    }

    public async Task<ConversationResponseDto> CreateDirectConversationAsync(int userId,
        CreateDirectConversationRequestDto request,CancellationToken cancellationToken = default)
    {
        var validationResult =
            await _directConversationValidator
                .ValidateAsync(
                    request,
                    cancellationToken);

        if (!validationResult.IsValid)
        {
            throw new ValidationException(
                validationResult.Errors);
        }

        var otherUserId = request.OtherUserId;

        if (otherUserId == userId)
        {
            throw new BadRequestException(
                "Özünüzlə şəxsi söhbət yarada bilməzsiniz.");
        }

        var users =
            await _dbContext.Users
                .Where(user =>
                    user.Id == userId ||
                    user.Id == otherUserId)
                .ToDictionaryAsync(
                    user => user.Id,
                    cancellationToken);

        if (!users.ContainsKey(otherUserId))
        {
            throw new KeyNotFoundException(
                "İstifadəçi tapılmadı.");
        }

        if (!users.ContainsKey(userId))
        {
            throw new KeyNotFoundException(
                "Cari istifadəçi tapılmadı.");
        }

        var isBlocked =
            await _dbContext.UserBlocks
                .AnyAsync(
                    userBlock =>
                        (
                            userBlock.BlockerId ==
                                userId &&
                            userBlock.BlockedUserId ==
                                otherUserId
                        ) ||
                        (
                            userBlock.BlockerId ==
                                otherUserId &&
                            userBlock.BlockedUserId ==
                                userId
                        ),
                    cancellationToken);

        if (isBlocked)
        {
            throw new ForbiddenException(
                "Bloklanmış istifadəçi ilə şəxsi söhbət yaratmaq mümkün deyil.");
        }

        var areFriends =await _dbContext.Friendships
                .AsNoTracking()
                .AnyAsync(
                    friendship =>
                        (
                            friendship.UserId ==userId &&
                            friendship.FriendId ==otherUserId
                        ) ||
                        (
                            friendship.UserId ==otherUserId &&
                            friendship.FriendId == userId
                        ),
                    cancellationToken);

        if (!areFriends)
        {
            await EnsureCanMessageThroughSharedServerAsync( userId,otherUserId,cancellationToken);
        }

      
        var existingConversation =await _dbContext.Conversations
                .Include(conversation =>
                    conversation.Members)
                .ThenInclude(member =>
                    member.User)
                .FirstOrDefaultAsync(
                    conversation =>
                        conversation.Type == ConversationType.Direct && conversation.Members.Count == 2 && conversation.Members.Any(
                            member =>member.UserId == userId) &&
                        conversation.Members.Any( member => member.UserId ==otherUserId),cancellationToken);

        if (existingConversation is not null)
        {
            var currentMember =existingConversation.Members.First(member =>
                        member.UserId == userId);

            if (!currentMember.IsVisibleInList)
            {
                currentMember.IsVisibleInList =
                    true;

                await _dbContext.SaveChangesAsync(
                    cancellationToken);
            }

            return existingConversation
                .ToResponseDto();
        }

        var conversation = new Conversation
        {
            Type = ConversationType.Direct,
            Name = null,
            IconUrl = null,
            OwnerId = null
        };

     
        conversation.Members.Add(
            new ConversationMember
            {
                UserId = userId,
                User = users[userId],
                IsVisibleInList = true
            });

        
        conversation.Members.Add(
            new ConversationMember
            {
                UserId = otherUserId,
                User = users[otherUserId],
                IsVisibleInList = false
            });

        _dbContext.Conversations.Add(
            conversation);

        await _dbContext.SaveChangesAsync(
            cancellationToken);

        return conversation.ToResponseDto();
    }

    public async Task<ConversationResponseDto>CreateGroupConversationAsync(int userId,CreateGroupConversationRequestDto request,
            CancellationToken cancellationToken = default)
    {
        var validationResult =await _groupConversationValidator.ValidateAsync( request,
                    cancellationToken);

        if (!validationResult.IsValid)
        {
            throw new ValidationException( validationResult.Errors);
        }

        var memberUserIds =request.MemberUserIds
                .Distinct()
                .ToArray();

        if (memberUserIds.Contains(userId))
        {
            throw new BadRequestException( "Cari istifadəçini üzv siyahısına əlavə etməyin. O, avtomatik əlavə olunur.");
        }

        var blockedMemberExists =await _dbContext.UserBlocks.AnyAsync(
                userBlock => (userBlock.BlockerId == userId &&memberUserIds.Contains(userBlock.BlockedUserId)) ||
                    (userBlock.BlockedUserId ==
                        userId &&memberUserIds.Contains(userBlock.BlockerId)),
                cancellationToken);

        if (blockedMemberExists)
        {
            throw new ForbiddenException("Bloklanmış istifadəçi Group DM-ə əlavə edilə bilməz.");
        }

        var friendUserIds = await _dbContext.Friendships
                .Where(friendship =>
                    (friendship.UserId == userId &&
                     memberUserIds.Contains(
                         friendship.FriendId)) ||
                    (friendship.FriendId == userId &&
                     memberUserIds.Contains(
                         friendship.UserId)))
                .Select(friendship =>
                    friendship.UserId == userId
                        ? friendship.FriendId
                        : friendship.UserId)
                .Distinct()
                .ToListAsync(cancellationToken);

        if (friendUserIds.Count != memberUserIds.Length)
        {
            throw new ForbiddenException("Group DM-ə yalnız dostlarınızı əlavə edə bilərsiniz.");
        }

        var allUserIds =memberUserIds
                .Append(userId)
                .ToArray();

        var users =
            await _dbContext.Users
                .Where(user =>
                    allUserIds.Contains(user.Id))
                .ToDictionaryAsync(
                    user => user.Id,
                    cancellationToken);

        if (users.Count != allUserIds.Length)
        {
            throw new KeyNotFoundException(
                "Seçilmiş istifadəçilərdən biri tapılmadı.");
        }

        var conversation = new Conversation
        {
            Type = ConversationType.Group,
            Name = request.Name.Trim(),
            IconUrl = null,
            OwnerId = userId,
            Owner = users[userId]
        };

        foreach (var memberUserId in allUserIds)
        {
            conversation.Members.Add(
                new ConversationMember
                {
                    UserId = memberUserId,
                    User = users[memberUserId],
                    IsMuted = false
                });
        }

        _dbContext.Conversations.Add(
            conversation);

        await _dbContext.SaveChangesAsync(
            cancellationToken);

        return conversation.ToResponseDto();
    }

    public async Task<ConversationResponseDto>AddGroupMemberAsync(int conversationId, int userId,AddGroupMemberRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var validationResult =
            await _addGroupMemberValidator
                .ValidateAsync(
                    request,
                    cancellationToken);

        if (!validationResult.IsValid)
        {
            throw new ValidationException(
                validationResult.Errors);
        }

        if (conversationId <= 0)
        {
            throw new BadRequestException(
                "Söhbət ID-si düzgün deyil.");
        }

        var conversation =
            await _dbContext.Conversations
                .Include(item => item.Members)
                .ThenInclude(member => member.User)
                .FirstOrDefaultAsync(
                    item =>
                        item.Id == conversationId,
                    cancellationToken)
            ?? throw new KeyNotFoundException(
                "Söhbət tapılmadı.");

        if (conversation.Type !=
            ConversationType.Group)
        {
            throw new BadRequestException(
                "Üzv yalnız Group DM-ə əlavə edilə bilər.");
        }

        var currentUserIsMember =
            conversation.Members.Any(member =>
                member.UserId == userId);

        if (!currentUserIsMember)
        {
            throw new ForbiddenException(
                "Yalnız Group DM üzvləri yeni üzv əlavə edə bilər.");
        }

        if (conversation.Members.Count >= 10)
        {
            throw new ConflictException(
                "Group DM maksimum 10 üzvdən ibarət ola bilər.");
        }

        var userAlreadyMember =
            conversation.Members.Any(member =>
                member.UserId == request.UserId);

        if (userAlreadyMember)
        {
            throw new ConflictException(
                "İstifadəçi artıq bu Group DM-in üzvüdür.");
        }

        var userToAdd =
            await _dbContext.Users
                .FirstOrDefaultAsync(
                    user =>
                        user.Id == request.UserId,
                    cancellationToken)
            ?? throw new KeyNotFoundException(
                "Əlavə ediləcək istifadəçi tapılmadı.");

        var isBlocked =
            await _dbContext.UserBlocks.AnyAsync(
                userBlock =>
                    (userBlock.BlockerId == userId &&
                     userBlock.BlockedUserId ==
                        request.UserId) ||
                    (userBlock.BlockerId ==
                        request.UserId &&
                     userBlock.BlockedUserId == userId),
                cancellationToken);

        if (isBlocked)
        {
            throw new ForbiddenException(
                "Bloklanmış istifadəçi Group DM-ə əlavə edilə bilməz.");
        }

        var areFriends =
            await _dbContext.Friendships
                .AsNoTracking()
                .AnyAsync(
                    friendship =>
                        (friendship.UserId == userId &&
                         friendship.FriendId ==
                            request.UserId) ||
                        (friendship.UserId ==
                            request.UserId &&
                         friendship.FriendId == userId),
                    cancellationToken);

        if (!areFriends)
        {
            throw new ForbiddenException(
                "Group DM-ə yalnız dostunuzu əlavə edə bilərsiniz.");
        }

        conversation.Members.Add(
            new ConversationMember
            {
                UserId = request.UserId,
                User = userToAdd,
                IsMuted = false
            });

        conversation.UpdatedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync(
            cancellationToken);

        return conversation.ToResponseDto();
    }

    public async Task<ConversationResponseDto> UpdateGroupConversationAsync(int conversationId, int userId, UpdateGroupConversationRequestDto request,
         CancellationToken cancellationToken = default)
    {
        var validationResult =
            await _updateGroupConversationValidator
                .ValidateAsync(
                    request,
                    cancellationToken);

        if (!validationResult.IsValid)
        {
            throw new ValidationException(
                validationResult.Errors);
        }

        if (conversationId <= 0)
        {
            throw new BadRequestException(
                "Söhbət ID-si düzgün deyil.");
        }

        var conversation =
            await _dbContext.Conversations
                .Include(item => item.Members)
                .ThenInclude(member => member.User)
                .FirstOrDefaultAsync(
                    item =>
                        item.Id == conversationId,
                    cancellationToken)
            ?? throw new KeyNotFoundException(
                "Söhbət tapılmadı.");

        if (conversation.Type !=
            ConversationType.Group)
        {
            throw new BadRequestException(
                "Yalnız Group DM məlumatları dəyişdirilə bilər.");
        }

        var isMember =
            conversation.Members.Any(member =>
                member.UserId == userId);

        if (!isMember)
        {
            throw new ForbiddenException(
                "Yalnız Group DM üzvləri qrupun adını dəyişə bilər.");
        }

        conversation.Name = request.Name.Trim();
        conversation.UpdatedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync(
            cancellationToken);

        return conversation.ToResponseDto();
    }


    public async Task<ConversationResponseDto>RemoveGroupMemberAsync(int conversationId,int userId,int memberUserId,
        CancellationToken cancellationToken = default)
    {
        if (conversationId <= 0)
        {
            throw new BadRequestException( "Söhbət ID-si düzgün deyil.");
        }

        if (memberUserId <= 0)
        {
            throw new BadRequestException(
                "Üzv ID-si düzgün deyil.");
        }

        var conversation =await _dbContext.Conversations
                .Include(item => item.Members)
                .ThenInclude(member => member.User)
                .FirstOrDefaultAsync(
                    item => item.Id == conversationId,
                    cancellationToken)
            ?? throw new KeyNotFoundException(
                "Söhbət tapılmadı.");

        if (conversation.Type !=
            ConversationType.Group)
        {
            throw new BadRequestException(
                "Üzv yalnız Group DM-dən çıxarıla bilər.");
        }

        var currentUserIsMember = conversation.Members.Any(member =>member.UserId == userId);

        if (!currentUserIsMember)
        {
            throw new ForbiddenException(
                "Bu Group DM-in üzvü deyilsiniz.");
        }

        if (conversation.OwnerId != userId)
        {
            throw new ForbiddenException(
                "Yalnız Group DM sahibi üzv çıxara bilər.");
        }

        if (memberUserId == userId)
        {
            throw new BadRequestException(
                "Özünüzü bu əməliyyatla çıxara bilməzsiniz. Group DM-dən ayrılma əməliyyatından istifadə edin.");
        }

        var memberToRemove = conversation.Members.FirstOrDefault(member => member.UserId == memberUserId)
            ?? throw new KeyNotFoundException(
                "İstifadəçi bu Group DM-in üzvü deyil.");

        _dbContext.ConversationMembers.Remove(memberToRemove);

        conversation.Members.Remove( memberToRemove);

        conversation.UpdatedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);

        return conversation.ToResponseDto();
    }

    public async Task LeaveGroupConversationAsync(int conversationId,int userId,CancellationToken cancellationToken = default)
    {
        if (conversationId <= 0)
        {
            throw new BadRequestException("Söhbət ID-si düzgün deyil.");
        }

        var conversation = await _dbContext.Conversations
                .Include(item => item.Members)
                .ThenInclude(member => member.User)
                .FirstOrDefaultAsync(
                    item => item.Id == conversationId,
                    cancellationToken)
            ?? throw new KeyNotFoundException(
                "Söhbət tapılmadı.");

        if (conversation.Type !=
            ConversationType.Group)
        {
            throw new BadRequestException("Yalnız Group DM-dən ayrılmaq mümkündür.");
        }

        var currentMember =
            conversation.Members.FirstOrDefault(
                member => member.UserId == userId)
            ?? throw new ForbiddenException("Bu Group DM-in üzvü deyilsiniz.");

        if (conversation.Members.Count == 1)
        {
            _dbContext.Conversations.Remove(
                conversation);

            await _dbContext.SaveChangesAsync(
                cancellationToken);

            return;
        }

        _dbContext.ConversationMembers.Remove(currentMember);
        conversation.Members.Remove(currentMember);

        if (conversation.OwnerId == userId)
        {
            var newOwner =conversation.Members
                    .OrderBy(member =>
                        member.CreatedAt)
                    .ThenBy(member =>
                        member.Id)
                    .First();

            conversation.OwnerId = newOwner.UserId;
            conversation.Owner = newOwner.User;
        }
        conversation.UpdatedAt = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task MarkAsReadAsync(int conversationId,int userId,CancellationToken cancellationToken =default)
    {
        if (conversationId <= 0)
        {
            throw new BadRequestException("Söhbət ID-si düzgün deyil.");
        }

        var membership =await _dbContext.ConversationMembers
                .FirstOrDefaultAsync(
                    member => member.ConversationId ==conversationId &&

                        member.UserId == userId,
                    cancellationToken)
            ?? throw new KeyNotFoundException("Söhbət tapılmadı və ya bu söhbətin üzvü deyilsiniz.");

        var latestMessageId =await _dbContext.Messages
                .AsNoTracking()
                .Where(message =>
                    message.ConversationId ==
                        conversationId)
                .MaxAsync(message => (int?)message.Id,cancellationToken);

        if (
            membership.LastReadMessageId ==latestMessageId
        )
        {
            return;}

        membership.LastReadMessageId =latestMessageId;
        membership.LastReadAt =DateTime.UtcNow;
        membership.UpdatedAt = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);
    }


    public async Task UpdateMuteStatusAsync(int conversationId, int userId,UpdateConversationMuteRequestDto request,
    CancellationToken cancellationToken = default)
    {
        if (conversationId <= 0)
        {
            throw new BadRequestException("Söhbət ID-si düzgün deyil.");
        }

        var conversationMember = await _dbContext.ConversationMembers
                .FirstOrDefaultAsync(
                    member =>member.ConversationId == conversationId &&member.UserId == userId,
                    cancellationToken)?? throw new KeyNotFoundException(
                "Söhbət tapılmadı və ya bu söhbətin üzvü deyilsiniz.");

        if (
            conversationMember.IsMuted ==request.IsMuted
        )
        {
            return;}

        conversationMember.IsMuted = request.IsMuted;
        conversationMember.UpdatedAt =  DateTime.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
    private async Task EnsureCanMessageThroughSharedServerAsync(int senderId,int recipientId,
        CancellationToken cancellationToken)
    {
        var privacySettings =await _dbContext.UserPrivacySettings
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    settings =>
                        settings.UserId == recipientId,
                    cancellationToken);

        var globalAllowDirectMessages = privacySettings?
                .AllowDirectMessagesFromServerMembers
            ?? true;

        var hasAllowedSharedServer =await _dbContext.ServerMembers
                .AsNoTracking()
                .Where(recipientMembership =>
                    recipientMembership.UserId ==
                    recipientId)
                .AnyAsync(
                    recipientMembership =>
                        _dbContext.ServerMembers.Any(
                            senderMembership =>
                                senderMembership.UserId ==
                                    senderId &&
                                senderMembership.ServerId ==
                                    recipientMembership.ServerId) &&
                        (
                            recipientMembership
                                .AllowDirectMessages ??
                            globalAllowDirectMessages
                        ),
                    cancellationToken);

        if (!hasAllowedSharedServer)
        {
            throw new ForbiddenException(
                "Bu istifadəçi yalnız dostlarından və icazə verdiyi ortaq server üzvlərindən DM qəbul edir.");
        }
    }

    
}
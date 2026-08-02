using Discord.Core.DTOs.Conversations.Requests;
using Discord.Core.DTOs.Conversations.Responses;

namespace Discord.Core.Interfaces;

public interface IConversationService
{
    Task<IReadOnlyCollection<ConversationResponseDto>>GetMyConversationsAsync(int userId,CancellationToken cancellationToken = default);

    Task<ConversationResponseDto> GetByIdAsync(int conversationId,int userId,CancellationToken cancellationToken = default);

    Task<ConversationResponseDto>CreateDirectConversationAsync(int userId,
            CreateDirectConversationRequestDto request,
            CancellationToken cancellationToken = default);

    Task<ConversationResponseDto>CreateGroupConversationAsync(int userId,
            CreateGroupConversationRequestDto request,
            CancellationToken cancellationToken = default);


    Task<ConversationResponseDto>UpdateGroupConversationAsync(int conversationId,int userId,UpdateGroupConversationRequestDto request,
        CancellationToken cancellationToken = default);

    Task<ConversationResponseDto>AddGroupMemberAsync(int conversationId,int userId,AddGroupMemberRequestDto request,
        CancellationToken cancellationToken = default);

    Task<ConversationResponseDto>RemoveGroupMemberAsync(int conversationId, int userId,int memberUserId,
        CancellationToken cancellationToken = default);

    Task LeaveGroupConversationAsync(int conversationId, int userId,CancellationToken cancellationToken = default);

    Task MarkAsReadAsync( int conversationId, int userId,CancellationToken cancellationToken = default);

    Task UpdateMuteStatusAsync(int conversationId,int userId,UpdateConversationMuteRequestDto request,CancellationToken cancellationToken = default);
}
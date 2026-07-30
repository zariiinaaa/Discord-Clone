using Discord.Core.DTOs.Messages.Requests;
using Discord.Core.DTOs.Messages.Responses;

namespace Discord.Core.Interfaces;

public interface IMessageService
{
    Task<IReadOnlyCollection<MessageResponseDto>>
        GetChannelMessagesAsync(
            int channelId,
            int userId,
            int? beforeMessageId = null,
            int limit = 50,
            CancellationToken cancellationToken = default);

    Task<MessageResponseDto> CreateAsync(int channelId,int userId,
        CreateMessageRequestDto request,
        CancellationToken cancellationToken = default);

    Task<MessageResponseDto> UpdateAsync(int channelId,int messageId,
        int userId,
        UpdateMessageRequestDto request,
        CancellationToken cancellationToken = default);

    Task DeleteAsync(int channelId,int messageId,
        int userId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<MessageResponseDto>> GetConversationMessagesAsync(int conversationId,int userId,
        int? beforeMessageId = null,
        int limit = 50,
        CancellationToken cancellationToken = default);

    Task<MessageResponseDto>CreateConversationMessageAsync(int conversationId,int userId,
            CreateMessageRequestDto request,
            CancellationToken cancellationToken = default);


    Task<MessageResponseDto>UpdateConversationMessageAsync(int conversationId,int messageId,int userId,
            UpdateMessageRequestDto request,
            CancellationToken cancellationToken = default);

    Task DeleteConversationMessageAsync(int conversationId, int messageId,int userId,
        CancellationToken cancellationToken = default);
}
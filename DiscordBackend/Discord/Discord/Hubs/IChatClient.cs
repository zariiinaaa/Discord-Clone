using Discord.Core.DTOs.Messages.Responses;
using Discord.Core.Enums;

namespace Discord.Hubs;

public interface IChatClient
{
    Task MessageCreated(MessageResponseDto message);
    Task MessageUpdated(MessageResponseDto message);
    Task MessageDeleted(int channelId, int messageId);
    Task ConversationMessageCreated(int conversationId,MessageResponseDto message);
    Task ConversationMessageUpdated(int conversationId,MessageResponseDto message);
    Task ConversationMessageDeleted(int conversationId,int messageId);
    Task ConversationTypingChanged(int conversationId,int userId, bool isTyping);
    Task DirectMessageRequestCreated();
    Task FriendDataChanged();
    Task ConversationDataChanged();
    Task UserPresenceChanged(int userId,UserStatus status);
    Task ServerMembersChanged(int serverId);

}
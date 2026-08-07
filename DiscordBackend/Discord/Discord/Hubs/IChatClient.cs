using Discord.Core.DTOs.Messages.Responses;
using Discord.Core.Enums;

namespace Discord.Hubs;

public interface IChatClient
{
    Task MessageCreated(MessageResponseDto message);
    Task MessageUpdated(MessageResponseDto message);
    Task MessageDeleted(int channelId, int messageId);
    Task ConversationMessageCreated(int conversationId, MessageResponseDto message);
    Task ConversationMessageUpdated(int conversationId, MessageResponseDto message);
    Task ConversationMessageDeleted(int conversationId, int messageId);
    Task ConversationTypingChanged(int conversationId, int userId, bool isTyping);
    Task DirectMessageRequestCreated();
    Task FriendDataChanged();
    Task ConversationDataChanged();
    Task UserPresenceChanged(int userId, UserStatus status);
    Task ServerMembersChanged(int serverId);
    Task MessageReactionChanged(int channelId, int messageId, string emoji,int count, int userId, bool isAdded);
    Task ConversationMessageReactionChanged(int conversationId, int messageId,string emoji, int count, int userId, bool isAdded);
    Task ServerMemberRemoved(int serverId, string message);
    Task ServerChannelsChanged(int serverId);
    Task ChannelAccessRevoked(int serverId, int channelId, string message);
}
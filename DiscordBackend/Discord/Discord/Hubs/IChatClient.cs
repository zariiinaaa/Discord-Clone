using Discord.Core.DTOs.Messages.Responses;

namespace Discord.Hubs;

public interface IChatClient
{
    Task MessageCreated(MessageResponseDto message);

    Task MessageUpdated(MessageResponseDto message);

    Task MessageDeleted(int channelId,int messageId);
}
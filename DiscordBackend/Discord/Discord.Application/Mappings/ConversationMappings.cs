using Discord.Core.DTOs.Conversations.Responses;
using Discord.Core.Entities.Conversations;

namespace Discord.Application.Mappings;

public static class ConversationMappings
{
    public static ConversationResponseDto ToResponseDto(this Conversation conversation)
    {
        return new ConversationResponseDto
        {
            Id = conversation.Id,
            Type = conversation.Type,
            Name = conversation.Name,
            IconUrl = conversation.IconUrl,
            OwnerId = conversation.OwnerId,
            CreatedAt = conversation.CreatedAt,

            Members = conversation.Members.OrderBy(member =>
                    member.User.DisplayName).Select(member =>
                    new ConversationMemberResponseDto
                    {
                        UserId = member.UserId,
                        Username =member.User.Username,
                        DisplayName =member.User.DisplayName,
                        AvatarUrl = member.User.AvatarUrl,
                        Status =member.User.Status,
                        IsMuted = member.IsMuted
                    })
                .ToArray()
        };
    }
}
using Discord.Core.DTOs.Messages.Responses;
using Discord.Core.Entities.Messages;

namespace Discord.Application.Mappings;

public static class MessageMappings
{
    public static MessageResponseDto ToResponseDto(this Message message,int? currentUserId = null)
    {
        return new MessageResponseDto
        {
            Id = message.Id,
            Content = message.Content,
            ChannelId = message.ChannelId,
            ConversationId = message.ConversationId,
            AuthorId = message.AuthorId,
            AuthorUsername = message.Author.Username,
            AuthorDisplayName = message.Author.DisplayName,
            AuthorAvatarUrl = message.Author.AvatarUrl,
            ReplyToMessageId = message.ReplyToMessageId,
            IsPinned = message.IsPinned,
            EditedAt = message.EditedAt,
            CreatedAt = message.CreatedAt,

            Attachments = message.Attachments
                .OrderBy(attachment => attachment.Id)
                .Select(attachment =>
                    new MessageAttachmentResponseDto
                    {
                        Id = attachment.Id,
                        FileName = attachment.FileName,
                        FileUrl = attachment.FileUrl,
                        ContentType = attachment.ContentType,
                        FileSize = attachment.FileSize
                    })
                .ToArray(),

            Reactions = message.Reactions
                .GroupBy(reaction => reaction.Emoji)
                .Select(group =>
                    new MessageReactionResponseDto
                    {
                        Emoji = group.Key,
                        Count = group.Count(),

                        HasReacted =
                            currentUserId.HasValue &&
                            group.Any(reaction =>
                                reaction.UserId ==
                                currentUserId.Value)
                    })
                .OrderBy(reaction => reaction.Emoji)
                .ToArray()
        };
    }
}
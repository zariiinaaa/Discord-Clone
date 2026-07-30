using System;
using System.Collections.Generic;
using System.Text;

namespace Discord.Core.DTOs.Messages.Responses;

public class MessageResponseDto
{
    public int Id { get; set; }
    public string Content { get; set; } = string.Empty;
    public int? ChannelId { get; set; }
    public int AuthorId { get; set; }
    public string AuthorUsername { get; set; }= string.Empty;
    public string AuthorDisplayName { get; set; }= string.Empty;
    public string? AuthorAvatarUrl { get; set; }
    public int? ReplyToMessageId { get; set; }
    public bool IsPinned { get; set; }
    public DateTime? EditedAt { get; set; }
    public DateTime CreatedAt { get; set; }

    public int? ConversationId { get; set; }
}
using Discord.Core.Entities.Common;

namespace Discord.Core.Entities.Messages;

public class MessageAttachment : BaseEntity
{
    public string FileName { get; set; } = string.Empty;
    public string StoredFileName { get; set; } = string.Empty;
    public string FileUrl { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public int MessageId { get; set; }
    public Message Message { get; set; } = null!;
} 
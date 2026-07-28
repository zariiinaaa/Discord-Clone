using Discord.Core.Entities.Common;
using Discord.Core.Entities.Servers;

namespace Discord.Core.Entities.Messages;

public class Message : BaseEntity
{
    public string Content { get; set; } = string.Empty;
    public DateTime? EditedAt { get; set; }
    public bool IsPinned { get; set; }
    public int ChannelId { get; set; }
    public Channel Channel { get; set; } = null!;
    public int AuthorId { get; set; }
    public User Author { get; set; } = null!;
    public int? ReplyToMessageId { get; set; }
    public Message? ReplyToMessage { get; set; }
    public ICollection<Message> Replies { get; set; } = new List<Message>();
}
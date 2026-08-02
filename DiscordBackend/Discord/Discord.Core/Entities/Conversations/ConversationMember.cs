using Discord.Core.Entities.Common;

namespace Discord.Core.Entities.Conversations;

public class ConversationMember : BaseEntity
{
    public int ConversationId { get; set; }
    public Conversation Conversation { get; set; } = null!;
    public int UserId { get; set; }
    public User User { get; set; } = null!;
    public bool IsMuted { get; set; }
    public DateTime? LastReadAt { get; set; }
    public int? LastReadMessageId { get; set; }
    public bool IsVisibleInList { get; set; } = true;

}
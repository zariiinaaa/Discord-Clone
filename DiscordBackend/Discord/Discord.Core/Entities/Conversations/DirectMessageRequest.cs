using Discord.Core.Entities.Common;
using Discord.Core.Enums;

namespace Discord.Core.Entities.Conversations;

public class DirectMessageRequest : BaseEntity
{
    public int ConversationId { get; set; }
    public int SenderId { get; set; }
    public int RecipientId { get; set; }
    public MessageRequestStatus Status { get; set; }= MessageRequestStatus.Pending;
    public DateTime? RespondedAt { get; set; }
    public Conversation Conversation { get; set; }= null!;
    public User Sender { get; set; }= null!;
    public User Recipient { get; set; }= null!;
}
using System;
using System.Collections.Generic;
using System.Text;
using Discord.Core.Entities.Common;
using Discord.Core.Entities.Messages;
using Discord.Core.Enums;

namespace Discord.Core.Entities.Conversations;

public class Conversation : BaseEntity
{
    public ConversationType Type { get; set; }
    public string? Name { get; set; }
    public string? IconUrl { get; set; }
    public int? OwnerId { get; set; }
    public User? Owner { get; set; }
    public ICollection<ConversationMember> Members { get; set; }= new List<ConversationMember>();
    public ICollection<Message> Messages { get; set; } = new List<Message>();
    public ICollection<DirectMessageRequest>MessageRequests{ get; set; }= new List<DirectMessageRequest>();
}

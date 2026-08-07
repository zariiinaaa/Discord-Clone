using System;
using System.Collections.Generic;
using System.Text;
using Discord.Core.Entities.Common;

namespace Discord.Core.Entities.Messages;

public class MessageReaction : BaseEntity
{
    public int MessageId { get; set; }
    public Message Message { get; set; } =null!;
    public int UserId { get; set; }
    public User User { get; set; } =null!;
    public string Emoji { get; set; } =string.Empty;
}
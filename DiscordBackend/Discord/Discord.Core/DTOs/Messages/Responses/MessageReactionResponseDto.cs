using System;
using System.Collections.Generic;
using System.Text;

namespace Discord.Core.DTOs.Messages.Responses;

public class MessageReactionResponseDto
{
    public string Emoji { get; set; } =string.Empty;
    public int Count { get; set; }
    public bool HasReacted { get; set; }
}
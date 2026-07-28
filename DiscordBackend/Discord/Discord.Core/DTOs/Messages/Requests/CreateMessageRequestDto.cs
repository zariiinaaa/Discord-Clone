using System;
using System.Collections.Generic;
using System.Text;

namespace Discord.Core.DTOs.Messages.Requests;

public class CreateMessageRequestDto
{
    public string Content { get; set; } = string.Empty;
    public int? ReplyToMessageId { get; set; }
}
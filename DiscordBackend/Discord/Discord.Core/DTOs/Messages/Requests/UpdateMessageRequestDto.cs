using System;
using System.Collections.Generic;
using System.Text;

namespace Discord.Core.DTOs.Messages.Requests;

public class UpdateMessageRequestDto
{
    public string Content { get; set; } = string.Empty;
}
using System;
using System.Collections.Generic;
using System.Text;

namespace Discord.Core.DTOs.Conversations.Requests;

public class UpdateConversationMuteRequestDto
{
    public bool IsMuted { get; set; }
}

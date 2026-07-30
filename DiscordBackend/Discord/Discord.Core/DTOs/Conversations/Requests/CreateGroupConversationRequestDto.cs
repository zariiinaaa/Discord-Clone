using System;
using System.Collections.Generic;
using System.Text;

namespace Discord.Core.DTOs.Conversations.Requests;

public class CreateGroupConversationRequestDto
{
    public string Name { get; set; } = string.Empty;
    public ICollection<int> MemberUserIds { get; set; } = new List<int>();
}

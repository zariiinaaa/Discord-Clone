using System;
using System.Collections.Generic;
using System.Text;

using Discord.Core.Enums;

namespace Discord.Core.DTOs.Conversations.Responses;

public class ConversationResponseDto
{
    public int Id { get; set; }
    public ConversationType Type { get; set; }
    public string? Name { get; set; }
    public string? IconUrl { get; set; }
    public int? OwnerId { get; set; }
    public IReadOnlyCollection<ConversationMemberResponseDto> Members{ get; set; }=
        Array.Empty<ConversationMemberResponseDto>();
    public DateTime CreatedAt { get; set; }
}
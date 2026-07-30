using System;
using System.Collections.Generic;
using System.Text;

using Discord.Core.Enums;

namespace Discord.Core.DTOs.Conversations.Responses;

public class ConversationMemberResponseDto
{
    public int UserId { get; set; }
    public string Username { get; set; }= string.Empty;
    public string DisplayName { get; set; }= string.Empty;
    public string? AvatarUrl { get; set; }
    public UserStatus Status { get; set; }
    public bool IsMuted { get; set; }
}
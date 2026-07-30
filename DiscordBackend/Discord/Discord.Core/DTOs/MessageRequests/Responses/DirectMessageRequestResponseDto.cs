using Discord.Core.Enums;

namespace Discord.Core.DTOs.MessageRequests.Responses;

public class DirectMessageRequestResponseDto
{
    public int Id { get; set; }
    public int ConversationId { get; set; }
    public int SenderId { get; set; }
    public string SenderUsername { get; set; } = string.Empty;
    public string SenderDisplayName { get; set; }= string.Empty;
    public string? SenderAvatarUrl { get; set; }
    public MessageRequestStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
}
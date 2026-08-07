namespace Discord.Core.DTOs.Admin.Responses;

public class AdminDashboardResponseDto
{
    public int TotalUsers { get; set; }
    public int TotalAdmins { get; set; }
    public int ActiveBannedUsers { get; set; }
    public int TotalServers { get; set; }
    public int PublicServers { get; set; }
    public int TotalChannels { get; set; }
    public int TotalMessages { get; set; }
    public int TotalConversations { get; set; }
    public int TotalAttachments { get; set; }
    public int TotalServerMemberships { get; set; }
    public int UsersCreatedLast7Days { get; set; }
    public int MessagesCreatedLast24Hours { get; set; }
}

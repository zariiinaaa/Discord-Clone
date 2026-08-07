using Discord.Core.Enums;

namespace Discord.Core.DTOs.Admin.Requests;

public class AdminUserListQueryDto
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public string? Search { get; set; }
    public bool? IsBanned { get; set; }
    public PlatformRole? Role { get; set; }
}

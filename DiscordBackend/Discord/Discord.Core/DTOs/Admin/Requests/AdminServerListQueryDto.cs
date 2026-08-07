namespace Discord.Core.DTOs.Admin.Requests;

public class AdminServerListQueryDto
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public string? Search { get; set; }
    public bool? IsPublic { get; set; }
}

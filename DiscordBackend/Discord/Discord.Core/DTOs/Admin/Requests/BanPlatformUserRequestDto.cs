namespace Discord.Core.DTOs.Admin.Requests;

public class BanPlatformUserRequestDto
{
    public string Reason { get; set; } = string.Empty;
    public DateTime? BannedUntil { get; set; }
}

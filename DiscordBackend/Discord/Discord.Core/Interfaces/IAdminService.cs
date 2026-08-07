using Discord.Core.DTOs.Admin.Requests;
using Discord.Core.DTOs.Admin.Responses;
using Discord.Core.DTOs.Common;
using Discord.Core.Models.Admin;

namespace Discord.Core.Interfaces;

public interface IAdminService
{
    Task<AdminDashboardResponseDto> GetDashboardAsync(CancellationToken cancellationToken = default);
    Task<PagedResponseDto<AdminUserListItemResponseDto>> GetUsersAsync(AdminUserListQueryDto query,
        CancellationToken cancellationToken = default);
    Task<AdminUserDetailsResponseDto> GetUserByIdAsync(int userId,
        CancellationToken cancellationToken = default);
    Task<AdminUserDetailsResponseDto> BanUserAsync(int userId, int currentAdminId,
        BanPlatformUserRequestDto request, CancellationToken cancellationToken = default);
    Task UnbanUserAsync(int userId, CancellationToken cancellationToken = default);
    Task<PagedResponseDto<AdminServerListItemResponseDto>> GetServersAsync(AdminServerListQueryDto query,
        CancellationToken cancellationToken = default);
    Task<AdminServerDetailsResponseDto> GetServerByIdAsync(int serverId,
        CancellationToken cancellationToken = default);
    Task<AdminServerDeletionResult> DeleteServerAsync(int serverId,
        CancellationToken cancellationToken = default);
}

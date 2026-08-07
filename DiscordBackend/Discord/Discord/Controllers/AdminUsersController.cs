using Discord.Authorization;
using Discord.Core.DTOs.Admin.Requests;
using Discord.Core.DTOs.Admin.Responses;
using Discord.Core.DTOs.Common;
using Discord.Core.Exceptions;
using Discord.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Discord.Controllers;

[ApiController]
[Route("api/v1/admin/users")]
[Authorize(Policy = AuthorizationPolicyNames.PlatformAdmin)]
public class AdminUsersController : ControllerBase
{
    private readonly IAdminService _adminService;

    public AdminUsersController(IAdminService adminService)
    {
        _adminService = adminService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(PagedResponseDto<AdminUserListItemResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetUsers(
        [FromQuery] AdminUserListQueryDto query,
        CancellationToken cancellationToken)
    {
        return Ok(await _adminService.GetUsersAsync(query, cancellationToken));
    }

    [HttpGet("{userId:int}")]
    [ProducesResponseType(typeof(AdminUserDetailsResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetUser(int userId, CancellationToken cancellationToken)
    {
        return Ok(await _adminService.GetUserByIdAsync(userId, cancellationToken));
    }

    [HttpPut("{userId:int}/ban")]
    [ProducesResponseType(typeof(AdminUserDetailsResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> BanUser(
        int userId,
        BanPlatformUserRequestDto request,
        CancellationToken cancellationToken)
    {
        var result = await _adminService.BanUserAsync(
            userId,
            GetCurrentUserId(),
            request,
            cancellationToken);

        return Ok(result);
    }

    [HttpDelete("{userId:int}/ban")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UnbanUser(int userId, CancellationToken cancellationToken)
    {
        await _adminService.UnbanUserAsync(userId, cancellationToken);
        return NoContent();
    }

    private int GetCurrentUserId()
    {
        var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(userIdValue, out var userId))
        {
            throw new UnauthorizedException("Access token etibarsızdır.");
        }

        return userId;
    }
}

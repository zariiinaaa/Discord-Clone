using Discord.Authorization;
using Discord.Core.DTOs.Admin.Responses;
using Discord.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Discord.Controllers;

[ApiController]
[Route("api/v1/admin/dashboard")]
[Authorize(Policy = AuthorizationPolicyNames.PlatformAdmin)]
public class AdminDashboardController : ControllerBase
{
    private readonly IAdminService _adminService;

    public AdminDashboardController(IAdminService adminService)
    {
        _adminService = adminService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(AdminDashboardResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetDashboard(CancellationToken cancellationToken)
    {
        return Ok(await _adminService.GetDashboardAsync(cancellationToken));
    }
}

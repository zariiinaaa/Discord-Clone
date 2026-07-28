using Discord.Core.Exceptions;
using Discord.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Discord.Controllers;

[ApiController]
[Route("api/v1/invites")]
[Authorize]
public class InvitesController : ControllerBase
{
    private readonly IServerInviteService
        _serverInviteService;

    public InvitesController(
        IServerInviteService serverInviteService)
    {
        _serverInviteService = serverInviteService;
    }

    [HttpPost("{code}/join")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Join(
        string code,
        CancellationToken cancellationToken)
    {
        await _serverInviteService.JoinAsync(
            code,GetCurrentUserId(),cancellationToken);

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
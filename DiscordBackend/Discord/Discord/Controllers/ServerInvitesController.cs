using Discord.Core.DTOs.Invites.Requests;
using Discord.Core.DTOs.Invites.Responses;
using Discord.Core.Exceptions;
using Discord.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Discord.Controllers;

[ApiController]
[Route("api/v1/servers/{serverId:int}/invites")]
[Authorize]
public class ServerInvitesController : ControllerBase
{
    private readonly IServerInviteService _serverInviteService;

    public ServerInvitesController(IServerInviteService serverInviteService)
    {
        _serverInviteService = serverInviteService;
    }

    [HttpPost]
    [ProducesResponseType(typeof(ServerInviteResponseDto),StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Create(
        int serverId,
        CreateServerInviteRequestDto request,
        CancellationToken cancellationToken)
    {
        var result = await _serverInviteService.CreateAsync(serverId,GetCurrentUserId(),request,cancellationToken);

        return StatusCode(StatusCodes.Status201Created,result);
    }

    private int GetCurrentUserId()
    {
        var userIdValue = User.FindFirstValue( ClaimTypes.NameIdentifier);

        if (!int.TryParse(userIdValue, out var userId))
        {
            throw new UnauthorizedException("Access token etibarsızdır.");
        }

        return userId;
    }
}
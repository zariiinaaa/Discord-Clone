using Discord.Core.DTOs.ServerMembers.Responses;
using Discord.Core.Exceptions;
using Discord.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Discord.Controllers;

[ApiController]
[Route("api/v1/servers/{serverId:int}/members")]
[Authorize]
public class ServerMembersController : ControllerBase
{
    private readonly IServerMemberService _serverMemberService;

    public ServerMembersController(IServerMemberService serverMemberService)
    {
        _serverMemberService = serverMemberService;
    }

    [HttpGet]
    [ProducesResponseType(
        typeof(IReadOnlyCollection<ServerMemberResponseDto>),
        StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetMembers(int serverId,
        CancellationToken cancellationToken)
    {
        var result =
            await _serverMemberService.GetMembersAsync(
                serverId,
                GetCurrentUserId(),
                cancellationToken);

        return Ok(result);
    }

    [HttpDelete("me")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Leave(
        int serverId,
        CancellationToken cancellationToken)
    {
        await _serverMemberService.LeaveAsync(
            serverId,
            GetCurrentUserId(),
            cancellationToken);

        return NoContent();
    }

    [HttpDelete("{memberUserId:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Kick(
        int serverId,
        int memberUserId,
        CancellationToken cancellationToken)
    {
        await _serverMemberService.KickAsync(serverId, memberUserId,GetCurrentUserId(),
            cancellationToken);

        return NoContent();
    }

    private int GetCurrentUserId()
    {
        var userIdValue = User.FindFirstValue(
            ClaimTypes.NameIdentifier);

        if (!int.TryParse(userIdValue, out var userId))
        {
            throw new UnauthorizedException("Access token etibarsızdır.");
        }

        return userId;
    }
}
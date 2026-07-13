using Discord.Core.DTOs.Servers.Requests;
using Discord.Core.DTOs.Servers.Responses;
using Discord.Core.Exceptions;
using Discord.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Discord.Controllers;

[ApiController]
[Route("api/v1/servers")]
[Authorize]
public class ServersController : ControllerBase
{
    private readonly IServerService _serverService;

    public ServersController(IServerService serverService)
    {
        _serverService = serverService;
    }

    [HttpPost]
    [ProducesResponseType(
        typeof(ServerResponseDto),
        StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> CreateServer(
        CreateServerRequestDto request,
        CancellationToken cancellationToken)
    {
        var ownerId = GetCurrentUserId();

        var result = await _serverService.CreateAsync(
            ownerId,
            request,
            cancellationToken);

        return StatusCode(
            StatusCodes.Status201Created,
            result);
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<ServerResponseDto>),StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetMyServers(
    CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();

        var result = await _serverService.GetMyServersAsync(
            userId,
            cancellationToken);

        return Ok(result);
    }

    [HttpGet("{serverId:int}")]
    [ProducesResponseType(typeof(ServerDetailsResponseDto),StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetServerById(
    int serverId,
    CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();

        var result = await _serverService.GetByIdAsync(
            serverId,
            userId,
            cancellationToken);

        return Ok(result);
    }


    [HttpPatch("{serverId:int}")]
    [ProducesResponseType(typeof(ServerResponseDto),StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateServer(
    int serverId,
    UpdateServerRequestDto request,
    CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();

        var result = await _serverService.UpdateAsync(
            serverId,
            userId,
            request,
            cancellationToken);

        return Ok(result);
    }

    [HttpDelete("{serverId:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteServer(int serverId,CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();

        await _serverService.DeleteAsync(
            serverId,
            userId,
            cancellationToken);

        return NoContent();
    }

    private int GetCurrentUserId()
    {
        var userIdValue = User.FindFirstValue(
            ClaimTypes.NameIdentifier);

        if (!int.TryParse(userIdValue, out var userId))
        {
            throw new UnauthorizedException(
                "Access token etibarsızdır.");
        }

        return userId;
    }
}
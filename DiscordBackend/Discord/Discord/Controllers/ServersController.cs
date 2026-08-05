using Discord.Core.DTOs.Servers.Requests;
using Discord.Core.DTOs.Servers.Responses;
using Discord.Core.Exceptions;
using Discord.Core.Interfaces;
using Discord.Services.Interfaces;
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
    private readonly IFileStorageService _fileStorageService;

    public ServersController(IServerService serverService, IFileStorageService fileStorageService)
    {
        _serverService = serverService;
        _fileStorageService = fileStorageService;
        
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

    [HttpPut("{serverId:int}/icon")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(
    typeof(ServerResponseDto),
    StatusCodes.Status200OK)]
    [ProducesResponseType(
    StatusCodes.Status400BadRequest)]
    [ProducesResponseType(
    StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(
    StatusCodes.Status403Forbidden)]
    [ProducesResponseType(
    StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateServerIcon(int serverId,  IFormFile icon,
    CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();

        var currentServer =
            await _serverService.GetByIdAsync(serverId,userId,
      cancellationToken);

        if (currentServer.OwnerId != userId)
        {
            throw new ForbiddenException(
                "Yalnız server sahibi server iconunu dəyişə bilər.");
        }

        var newIconUrl = await _fileStorageService.SaveServerIconAsync( icon,
        cancellationToken);

        ServerResponseDto result;

        try
        {
            result = await _serverService.UpdateIconAsync(serverId,
            userId, newIconUrl,cancellationToken);
        }
        catch
        {
            _fileStorageService.DeleteServerIcon(newIconUrl);
            throw;
        }

        _fileStorageService.DeleteServerIcon( currentServer.IconUrl);

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
using Discord.Core.DTOs.ServerRoles.Requests;
using Discord.Core.DTOs.ServerRoles.Responses;
using Discord.Core.Exceptions;
using Discord.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Discord.Controllers;

[ApiController]
[Route("api/v1/servers/{serverId:int}/roles")]
[Authorize]
public class ServerRolesController : ControllerBase
{
    private readonly IServerRoleService _serverRoleService;

    public ServerRolesController(IServerRoleService serverRoleService)
    {
        _serverRoleService = serverRoleService;
    }

    [HttpGet]
    [ProducesResponseType(
        typeof(IReadOnlyCollection<ServerRoleResponseDto>),
        StatusCodes.Status200OK)]
    [ProducesResponseType(
        StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(
        StatusCodes.Status403Forbidden)]
    [ProducesResponseType(
        StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetRoles(int serverId,CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        var result =await _serverRoleService.GetRolesAsync( serverId,userId,
        cancellationToken);
        return Ok(result);
    }

    [HttpPost]
    [ProducesResponseType(
        typeof(ServerRoleResponseDto),
        StatusCodes.Status201Created)]
    [ProducesResponseType(
        StatusCodes.Status400BadRequest)]
    [ProducesResponseType(
        StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(
        StatusCodes.Status403Forbidden)]
    [ProducesResponseType(
        StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CreateRole(
        int serverId,
        CreateServerRoleRequestDto request,
        CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();

        var result =await _serverRoleService.CreateRoleAsync(serverId,userId,
         request,cancellationToken);

        return StatusCode(StatusCodes.Status201Created,result);
    }

    [HttpPatch("{roleId:int}")]
    [ProducesResponseType(
        typeof(ServerRoleResponseDto),
        StatusCodes.Status200OK)]
    [ProducesResponseType(
        StatusCodes.Status400BadRequest)]
    [ProducesResponseType(
        StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(
        StatusCodes.Status403Forbidden)]
    [ProducesResponseType(
        StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateRole( int serverId,int roleId,UpdateServerRoleRequestDto request,
        CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();

        var result =await _serverRoleService.UpdateRoleAsync(serverId,roleId, userId,
        request, cancellationToken);

        return Ok(result);
    }

    [HttpDelete("{roleId:int}")]
    [ProducesResponseType(
        StatusCodes.Status204NoContent)]
    [ProducesResponseType(
        StatusCodes.Status400BadRequest)]
    [ProducesResponseType(
        StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(
        StatusCodes.Status403Forbidden)]
    [ProducesResponseType(
        StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteRole(int serverId,int roleId,CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();

        await _serverRoleService.DeleteRoleAsync(serverId,roleId,userId,cancellationToken);

        return NoContent();
    }

    [HttpPut(
        "{roleId:int}/members/{memberUserId:int}")]
    [ProducesResponseType(
        StatusCodes.Status204NoContent)]
    [ProducesResponseType(
        StatusCodes.Status400BadRequest)]
    [ProducesResponseType(
        StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(
        StatusCodes.Status403Forbidden)]
    [ProducesResponseType(
        StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AssignRole(int serverId, int roleId, int memberUserId,CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();

        await _serverRoleService.AssignRoleAsync(serverId,memberUserId,roleId, userId,
        cancellationToken);

        return NoContent();
    }

    [HttpDelete(
        "{roleId:int}/members/{memberUserId:int}")]
    [ProducesResponseType(
        StatusCodes.Status204NoContent)]
    [ProducesResponseType(
        StatusCodes.Status400BadRequest)]
    [ProducesResponseType(
        StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(
        StatusCodes.Status403Forbidden)]
    [ProducesResponseType(
        StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RemoveRole(int serverId,int roleId,int memberUserId,
        CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();

        await _serverRoleService.RemoveRoleAsync(serverId,memberUserId,roleId,userId,
        cancellationToken);

        return NoContent();
    }

    private int GetCurrentUserId()
    {
        var userIdValue =User.FindFirstValue(
                ClaimTypes.NameIdentifier);

        if (!int.TryParse(
                userIdValue,
                out var userId))
        {
            throw new UnauthorizedException(
                "Access token etibarsızdır.");
        }

        return userId;
    }
}
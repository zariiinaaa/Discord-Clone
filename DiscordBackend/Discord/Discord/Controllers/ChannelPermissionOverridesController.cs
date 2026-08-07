using Discord.Core.DTOs.ChannelPermissions.Requests;
using Discord.Core.DTOs.ChannelPermissions.Responses;
using Discord.Core.Exceptions;
using Discord.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Discord.Controllers;

[ApiController]
[Route("api/v1/channels/{channelId:int}/permission-overrides")]
[Authorize]
public class ChannelPermissionOverridesController : ControllerBase
{
    private readonly IChannelPermissionOverrideService _channelPermissionOverrideService;

    public ChannelPermissionOverridesController(IChannelPermissionOverrideService
            channelPermissionOverrideService)
    {
        _channelPermissionOverrideService =channelPermissionOverrideService;
    }

    [HttpGet]
    [ProducesResponseType(
        typeof(ChannelPermissionOverridesResponseDto),
        StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Get(
        int channelId,
        CancellationToken cancellationToken)
    {
        var result =
            await _channelPermissionOverrideService
                .GetAsync(
                    channelId,
                    GetCurrentUserId(),
                    cancellationToken);

        return Ok(result);
    }

    [HttpPut("roles/{roleId:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateRole(
        int channelId,
        int roleId,
        UpdateChannelPermissionOverridesRequestDto request,
        CancellationToken cancellationToken)
    {
        await _channelPermissionOverrideService
            .UpdateRoleAsync(
                channelId,
                roleId,
                GetCurrentUserId(),
                request,
                cancellationToken);

        return NoContent();
    }

    [HttpDelete("roles/{roleId:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteRole(
        int channelId,
        int roleId,
        CancellationToken cancellationToken)
    {
        await _channelPermissionOverrideService
            .DeleteRoleAsync(
                channelId,
                roleId,
                GetCurrentUserId(),
                cancellationToken);

        return NoContent();
    }

    [HttpPut("members/{memberUserId:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateMember(
        int channelId,
        int memberUserId,
        UpdateChannelPermissionOverridesRequestDto request,
        CancellationToken cancellationToken)
    {
        await _channelPermissionOverrideService
            .UpdateMemberAsync(
                channelId,
                memberUserId,
                GetCurrentUserId(),
                request,
                cancellationToken);

        return NoContent();
    }

    [HttpDelete("members/{memberUserId:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteMember(
        int channelId,
        int memberUserId,
        CancellationToken cancellationToken)
    {
        await _channelPermissionOverrideService
            .DeleteMemberAsync(
                channelId,
                memberUserId,
                GetCurrentUserId(),
                cancellationToken);

        return NoContent();
    }


    [HttpPut("sync")]
    //[ProducesResponseType(StatusCodes.Status204NoContent)]
    //[ProducesResponseType(StatusCodes.Status400BadRequest)]
    //[ProducesResponseType(StatusCodes.Status401Unauthorized)]
    //[ProducesResponseType(StatusCodes.Status403Forbidden)]
    //[ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SyncWithCategory(
    int channelId,
    CancellationToken cancellationToken)
    {
        await _channelPermissionOverrideService
            .SyncWithCategoryAsync(
                channelId,
                GetCurrentUserId(),
                cancellationToken);

        return NoContent();
    }

    private int GetCurrentUserId()
    {
        var userIdValue =
            User.FindFirstValue(
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
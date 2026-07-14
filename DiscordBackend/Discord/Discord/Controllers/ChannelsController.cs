using Discord.Core.DTOs.Channels.Requests;
using Discord.Core.DTOs.Channels.Responses;
using Discord.Core.Exceptions;
using Discord.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Discord.Controllers;

[ApiController]
[Route("api/v1/servers/{serverId:int}/channels")]
[Authorize]
public class ChannelsController : ControllerBase
{
    private readonly IChannelService _channelService;

    public ChannelsController(IChannelService channelService)
    {
        _channelService = channelService;
    }

    [HttpPost]
    [ProducesResponseType(
        typeof(ChannelResponseDto),
        StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CreateChannel(
        int serverId,
        CreateChannelRequestDto request,
        CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();

        var result = await _channelService.CreateAsync(
            serverId,
            userId,
            request,
            cancellationToken);

        return StatusCode(
            StatusCodes.Status201Created,
            result);
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


    [HttpPatch("{channelId:int}")]
    [ProducesResponseType(
    typeof(ChannelResponseDto),
    StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateChannel(int serverId,int channelId,UpdateChannelRequestDto request,
    CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();

        var result = await _channelService.UpdateAsync(
            serverId,
            channelId,
            userId,
            request,
            cancellationToken);

        return Ok(result);
    }

    [HttpDelete("{channelId:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int serverId,int channelId,CancellationToken cancellationToken)
    {
        await _channelService.DeleteAsync(
            serverId,
            channelId,
            GetCurrentUserId(),
            cancellationToken);

        return NoContent();
    }
}
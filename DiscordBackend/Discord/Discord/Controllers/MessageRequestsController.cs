using Discord.Core.DTOs.MessageRequests.Responses;
using Discord.Core.Exceptions;
using Discord.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Discord.Controllers;

[ApiController]
[Route("api/v1/message-requests")]
[Authorize]
public class MessageRequestsController : ControllerBase
{
    private readonly IDirectMessageRequestService _messageRequestService;

    public MessageRequestsController( IDirectMessageRequestService messageRequestService)
    {
        _messageRequestService = messageRequestService;
    }

    [HttpGet("incoming")]
    [ProducesResponseType(
        typeof(IReadOnlyCollection<
            DirectMessageRequestResponseDto>),
        StatusCodes.Status200OK)]
    [ProducesResponseType(
        StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetIncoming(
        CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();

        var result =await _messageRequestService.GetIncomingAsync(userId,cancellationToken);

        return Ok(result);
    }

    [HttpPost("{requestId:int}/accept")]
    [ProducesResponseType(
        StatusCodes.Status204NoContent)]
    [ProducesResponseType(
        StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(
        StatusCodes.Status404NotFound)]
    [ProducesResponseType(
        StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Accept( int requestId,CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();

        await _messageRequestService.AcceptAsync(requestId, userId,cancellationToken);

        return NoContent();
    }

    [HttpPost("{requestId:int}/ignore")]
    [ProducesResponseType(
        StatusCodes.Status204NoContent)]
    [ProducesResponseType(
        StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(
        StatusCodes.Status404NotFound)]
    [ProducesResponseType(
        StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Ignore(int requestId,CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();

        await _messageRequestService.IgnoreAsync(
            requestId,
            userId,
            cancellationToken);

        return NoContent();
    }

    [HttpPost("{requestId:int}/spam")]
    [ProducesResponseType(
        StatusCodes.Status204NoContent)]
    [ProducesResponseType(
        StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(
        StatusCodes.Status404NotFound)]
    [ProducesResponseType(
        StatusCodes.Status409Conflict)]
    public async Task<IActionResult> MarkAsSpam( int requestId,CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();

        await _messageRequestService.MarkAsSpamAsync( requestId,userId, cancellationToken);

        return NoContent();
    }

    private int GetCurrentUserId()
    {
        var userIdValue =User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!int.TryParse(userIdValue,out var userId))
        {
            throw new UnauthorizedException("Access token etibarsızdır.");
        }

        return userId;
    }
}
using Discord.Core.DTOs.Invites.Responses;
using Discord.Core.Exceptions;
using Discord.Core.Interfaces;
using Discord.Hubs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;

namespace Discord.Controllers;

[ApiController]
[Route("api/v1/invites")]
[Authorize]
public class InvitesController : ControllerBase
{
    private readonly IServerInviteService _serverInviteService;

    private readonly IHubContext<ChatHub,IChatClient> _chatHubContext;

    public InvitesController(IServerInviteService serverInviteService, IHubContext<ChatHub, IChatClient>
        chatHubContext)
    {
        _serverInviteService = serverInviteService;
        _chatHubContext = chatHubContext;

    }

    [HttpGet("{code}")]
    [ProducesResponseType(
    typeof(ServerInviteDetailsResponseDto),
    StatusCodes.Status200OK)]
    [ProducesResponseType(
    StatusCodes.Status400BadRequest)]
    [ProducesResponseType(
    StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(
    StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetDetails(
    string code,
    CancellationToken cancellationToken)
    {
        var result =await _serverInviteService.GetInviteDetailsAsync(code,GetCurrentUserId(),
        cancellationToken);

        return Ok(result);
    }

    [HttpPost("{code}/join")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Join(string code, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();

        var inviteDetails =await _serverInviteService.GetInviteDetailsAsync(
         code,userId,cancellationToken);

        await _serverInviteService.JoinAsync( code,userId,cancellationToken);

        await _chatHubContext.Clients.All .ServerMembersChanged(inviteDetails.ServerId);

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
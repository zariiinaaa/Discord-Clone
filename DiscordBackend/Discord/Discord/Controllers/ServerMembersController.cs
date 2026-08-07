using Discord.Core.DTOs.ServerMembers.Responses;
using Discord.Core.Exceptions;
using Discord.Core.Interfaces;
using Discord.Hubs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;

namespace Discord.Controllers;

[ApiController]
[Route("api/v1/servers/{serverId:int}/members")]
[Authorize]
public class ServerMembersController : ControllerBase
{
    private readonly IServerMemberService _serverMemberService;
    private readonly IHubContext<ChatHub, IChatClient> _chatHubContext;

    public ServerMembersController( IServerMemberService serverMemberService,
        IHubContext<ChatHub, IChatClient> chatHubContext)
    {
        _serverMemberService = serverMemberService;
        _chatHubContext = chatHubContext;
    }

    [HttpGet]
    [ProducesResponseType(
        typeof(IReadOnlyCollection<ServerMemberResponseDto>),
        StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetMembers(
        int serverId,
        CancellationToken cancellationToken)
    {
        var result = await _serverMemberService.GetMembersAsync(
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
        var currentUserId = GetCurrentUserId();

        var membersBeforeLeaving =
            await _serverMemberService.GetMembersAsync(
                serverId,
                currentUserId,
                cancellationToken);

        await _serverMemberService.LeaveAsync(
            serverId,
            currentUserId,
            cancellationToken);

        await NotifyMembersChangedAsync(
            serverId,
            membersBeforeLeaving
                .Where(member =>
                    member.UserId != currentUserId)
                .Select(member =>
                    member.UserId));

        await _chatHubContext.Clients
            .Group(
                ChatHub.GetUserGroupName(
                    currentUserId))
            .ServerMemberRemoved(
                serverId,
                "Serverdən ayrıldınız.");

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
        var currentUserId = GetCurrentUserId();

        await _serverMemberService.KickAsync(
            serverId,
            memberUserId,
            currentUserId,
            cancellationToken);

        var remainingMembers =
            await _serverMemberService.GetMembersAsync(
                serverId,
                currentUserId,
                cancellationToken);

        await NotifyMembersChangedAsync(
            serverId,
            remainingMembers.Select(member =>
                member.UserId));

        await _chatHubContext.Clients
            .Group(
                ChatHub.GetUserGroupName(
                    memberUserId))
            .ServerMemberRemoved(
                serverId,
                "Bu serverdən çıxarıldınız.");

        return NoContent();
    }

    private async Task NotifyMembersChangedAsync(
        int serverId,
        IEnumerable<int> userIds)
    {
        var notifications = userIds
            .Distinct()
            .Select(userId =>
                _chatHubContext.Clients
                    .Group(
                        ChatHub.GetUserGroupName(
                            userId))
                    .ServerMembersChanged(
                        serverId));

        await Task.WhenAll(notifications);
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
using Discord.Core.DTOs.ServerMembers.Requests;
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
[Route("api/v1/servers/{serverId:int}/bans")]
[Authorize]
public class ServerBansController : ControllerBase
{
    private readonly IServerMemberService _serverMemberService;
    private readonly IHubContext<ChatHub, IChatClient> _chatHubContext;

    public ServerBansController(IServerMemberService serverMemberService, IHubContext<ChatHub, IChatClient> chatHubContext)
    {
        _serverMemberService = serverMemberService;
        _chatHubContext = chatHubContext;
    }

    [HttpGet]
    public async Task<IActionResult> GetBans(int serverId,
        CancellationToken cancellationToken)
    {
        var bans = await _serverMemberService.GetBansAsync(serverId, GetCurrentUserId(),
         cancellationToken);

        return Ok(bans);
    }

    [HttpPost("{memberUserId:int}")]
    public async Task<IActionResult> Ban(int serverId, int memberUserId,
     [FromBody] BanServerMemberRequestDto request,CancellationToken cancellationToken)
    {
        var currentUserId =GetCurrentUserId();

        await _serverMemberService.BanAsync(serverId,memberUserId,currentUserId,request.Reason,
            cancellationToken);

        var remainingMembers =await _serverMemberService.GetMembersAsync(
       serverId, currentUserId, cancellationToken);

        await NotifyMembersChangedAsync( serverId,remainingMembers.Select(member => member.UserId));

        await _chatHubContext.Clients.Group(ChatHub.GetUserGroupName(memberUserId))
            .ServerMemberRemoved( serverId,"Bu serverdən ban edildiniz.");

        return NoContent();
    }

    [HttpDelete("{bannedUserId:int}")]
    public async Task<IActionResult> Unban(
        int serverId,
        int bannedUserId,
        CancellationToken cancellationToken)
    {
        await _serverMemberService.UnbanAsync(
            serverId,
            bannedUserId,
            GetCurrentUserId(),
            cancellationToken);

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
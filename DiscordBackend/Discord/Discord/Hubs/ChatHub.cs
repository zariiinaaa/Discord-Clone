using Discord.Core.Exceptions;
using Discord.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;

namespace Discord.Hubs;

[Authorize]
public class ChatHub : Hub<IChatClient>
{
    private readonly IChannelAccessService _channelAccessService;

    public ChatHub(
        IChannelAccessService channelAccessService)
    {
        _channelAccessService = channelAccessService;
    }

    public async Task JoinChannel(int channelId)
    {
        var userId = GetCurrentUserId();

        await _channelAccessService
            .GetAccessibleTextChannelAsync(channelId,userId,
                Context.ConnectionAborted);

        await Groups.AddToGroupAsync( Context.ConnectionId,GetChannelGroupName(channelId),
            Context.ConnectionAborted);
    }

    public async Task LeaveChannel(int channelId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId,GetChannelGroupName(channelId),
            Context.ConnectionAborted);
    }

    public static string GetChannelGroupName(int channelId)
    {
        return $"channel:{channelId}";
    }

    private int GetCurrentUserId()
    {
        var userIdValue = Context.User?.FindFirstValue(
                ClaimTypes.NameIdentifier);

        if (!int.TryParse(userIdValue, out var userId))
        {
            throw new HubException( "Access token etibarsızdır.");
        }

        return userId;
    }
}
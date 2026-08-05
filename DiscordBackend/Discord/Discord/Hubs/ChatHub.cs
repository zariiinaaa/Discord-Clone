using Discord.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;

namespace Discord.Hubs;

[Authorize]
public class ChatHub : Hub<IChatClient>
{
    private readonly IChannelAccessService _channelAccessService;
    private readonly IConversationAccessService _conversationAccessService;
    private readonly IUserPresenceService _userPresenceService;
    private readonly UserConnectionTracker _userConnectionTracker;

    public ChatHub( IChannelAccessService channelAccessService,IConversationAccessService conversationAccessService,
     IUserPresenceService userPresenceService,
     UserConnectionTracker userConnectionTracker)
    {
        _channelAccessService =channelAccessService;
        _conversationAccessService = conversationAccessService;
        _userPresenceService = userPresenceService;
        _userConnectionTracker = userConnectionTracker;
    }



    public override async Task OnConnectedAsync()
    {
        var userId =GetCurrentUserId();
        var isFirstConnection =_userConnectionTracker.AddConnection(
        userId, Context.ConnectionId);
        System.Diagnostics.Debug.WriteLine(
    $"[PRESENCE] Connected: " +
    $"UserId={userId}, " +
    $"ConnectionId={Context.ConnectionId}, " +
    $"IsFirst={isFirstConnection}");

        if (isFirstConnection)
        {
            var visibleStatus = await _userPresenceService.MarkConnectedAsync(
            userId,Context.ConnectionAborted);

            await Clients.All.UserPresenceChanged(
            userId, visibleStatus);
        }

        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync( Exception? exception)
    {
        try
        {
            var userId = GetCurrentUserId();
            var isLastConnection = _userConnectionTracker.RemoveConnection(userId,Context.ConnectionId);

            System.Diagnostics.Debug.WriteLine(
    $"[PRESENCE] Disconnected: " +
    $"UserId={userId}, " +
    $"ConnectionId={Context.ConnectionId}, " +
    $"IsLast={isLastConnection}");

            if (isLastConnection)
            {
                var visibleStatus = await _userPresenceService.MarkDisconnectedAsync(
                  userId,CancellationToken.None);

                await Clients.All.UserPresenceChanged(userId,visibleStatus);
            }
        }
        finally
        {
            await base.OnDisconnectedAsync( exception);
        }
    }

    public async Task JoinChannel(int channelId)
    {
        var userId = GetCurrentUserId();

        await _channelAccessService.GetAccessibleTextChannelAsync( channelId,
                userId,
                Context.ConnectionAborted);

        await Groups.AddToGroupAsync(Context.ConnectionId,GetChannelGroupName(channelId),
            Context.ConnectionAborted);
    }

    public async Task LeaveChannel(int channelId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, GetChannelGroupName(channelId),
            Context.ConnectionAborted);
    }

    public async Task JoinConversation( int conversationId)
    {
        var userId = GetCurrentUserId();

        await _conversationAccessService .GetAccessibleConversationAsync(
                conversationId,
                userId,
                Context.ConnectionAborted);

        await Groups.AddToGroupAsync( Context.ConnectionId,GetConversationGroupName(conversationId),
            Context.ConnectionAborted);
    }

    public async Task LeaveConversation( int conversationId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId,GetConversationGroupName(conversationId),
            Context.ConnectionAborted);
    }


    public async Task SetConversationTyping(int conversationId,bool isTyping)
    {
        var userId = GetCurrentUserId();

        await _conversationAccessService.GetAccessibleConversationAsync(conversationId,userId,
                Context.ConnectionAborted);

        await Clients.OthersInGroup(GetConversationGroupName( conversationId)) .ConversationTypingChanged(
                conversationId,
                userId,
                isTyping);
    }


    public static string GetChannelGroupName(
        int channelId)
    {
        return $"channel:{channelId}";
    }

    public static string GetConversationGroupName(int conversationId)
    {
        return $"conversation:{conversationId}";
    }

    private int GetCurrentUserId()
    {
        var userIdValue =Context.User?.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!int.TryParse(
                userIdValue,
                out var userId))
        {
            throw new HubException(
                "Access token etibarsızdır.");
        }

        return userId;
    }
}
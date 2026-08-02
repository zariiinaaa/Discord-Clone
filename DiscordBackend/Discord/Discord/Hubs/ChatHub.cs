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

    public ChatHub(IChannelAccessService channelAccessService, IConversationAccessService conversationAccessService)
    {
        _channelAccessService =channelAccessService;

        _conversationAccessService = conversationAccessService;
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
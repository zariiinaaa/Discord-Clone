using Discord.Core.Interfaces;
using Discord.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace Discord.Realtime;

public class ChannelPermissionRealtimeNotifier: IChannelPermissionRealtimeNotifier
{
    private readonly IHubContext<ChatHub, IChatClient> _hubContext;

    public ChannelPermissionRealtimeNotifier(IHubContext<ChatHub, IChatClient> hubContext)
    {
        _hubContext = hubContext;
    }

    public Task NotifyServerChannelsChangedAsync(int serverId,IReadOnlyCollection<int> userIds,
     CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var notifications = userIds.Distinct().Select(userId => _hubContext.Clients.Group(ChatHub.GetUserGroupName(userId))
        .ServerChannelsChanged(serverId));

        return Task.WhenAll(notifications);
    }

    public Task NotifyChannelAccessRevokedAsync(int serverId, int channelId,IReadOnlyCollection<int> userIds, string message,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var notifications = userIds.Distinct()
            .Select(userId =>_hubContext.Clients
            .Group(ChatHub.GetUserGroupName(userId))
            .ChannelAccessRevoked( serverId,channelId, message));

        return Task.WhenAll(notifications);
    }
}
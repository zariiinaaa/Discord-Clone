using System;
using System.Collections.Generic;
using System.Text;

namespace Discord.Core.Interfaces;

public interface IChannelPermissionRealtimeNotifier
{
    Task NotifyServerChannelsChangedAsync(int serverId,IReadOnlyCollection<int> userIds,
    CancellationToken cancellationToken = default);

    Task NotifyChannelAccessRevokedAsync(int serverId, int channelId,IReadOnlyCollection<int> userIds, string message,
    CancellationToken cancellationToken = default);
}
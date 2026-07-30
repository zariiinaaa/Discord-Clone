using Discord.Core.Entities.Servers;

namespace Discord.Core.Interfaces;

public interface IChannelAccessService
{
    Task<Channel> GetAccessibleTextChannelAsync(int channelId,int userId,
        CancellationToken cancellationToken = default);

    Task<Channel> GetAccessibleVoiceChannelAsync(int channelId,int userId,
        CancellationToken cancellationToken = default);
}
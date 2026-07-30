using System.Collections.Concurrent;

namespace Discord.Hubs;

public sealed record VoiceParticipant(string ConnectionId,int UserId,
    string Username,
    string DisplayName);

public sealed record VoiceConnectionInfo( string ConnectionId,int ChannelId,int UserId,
    string Username,
    string DisplayName);

public class VoiceConnectionTracker
{
    private readonly ConcurrentDictionary<string,VoiceConnectionInfo> _connections = new();

    public VoiceConnectionInfo? GetConnection(string connectionId)
    {
        return _connections.TryGetValue(connectionId,out var connection)? connection: null;
    }

    public void AddConnection(string connectionId,int channelId,int userId, string username,
        string displayName)
    {
        var connection = new VoiceConnectionInfo(
            connectionId,
            channelId,
            userId,
            username,
            displayName);

        _connections[connectionId] = connection;
    }

    public VoiceConnectionInfo? RemoveConnection(string connectionId)
    {
        return _connections.TryRemove(
            connectionId,
            out var connection)
                ? connection
                : null;
    }

    public IReadOnlyCollection<VoiceParticipant>GetParticipants(int channelId)
    {
        return _connections.Values
            .Where(connection =>
                connection.ChannelId == channelId)
            .Select(connection =>
                new VoiceParticipant(
                    connection.ConnectionId,
                    connection.UserId,
                    connection.Username,
                    connection.DisplayName))
            .OrderBy(participant =>
                participant.DisplayName)
            .ToArray();
    }
}
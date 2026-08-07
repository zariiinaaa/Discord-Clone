using System.Collections.Concurrent;

namespace Discord.Hubs;

public sealed record VoiceParticipant(
    string ConnectionId,
    int UserId,
    string Username,
    string DisplayName);

public sealed record VoiceConnectionInfo(
    string ConnectionId,
    int ChannelId,
    int? ConversationId,
    int UserId,
    string Username,
    string DisplayName);

public class VoiceConnectionTracker
{
    private readonly ConcurrentDictionary<string, VoiceConnectionInfo> _connections = new();

    public VoiceConnectionInfo? GetConnection(string connectionId)
    {
        return _connections.TryGetValue(connectionId, out var connection)
            ? connection
            : null;
    }

    public void AddConnection(
        string connectionId,
        int channelId,
        int userId,
        string username,
        string displayName)
    {
        var connection = new VoiceConnectionInfo(
            connectionId,
            channelId,
            null,
            userId,
            username,
            displayName);

        _connections[connectionId] = connection;
    }

    public void AddConversationConnection(
        string connectionId,
        int conversationId,
        int userId,
        string username,
        string displayName)
    {
        var connection = new VoiceConnectionInfo(
            connectionId,
            0,
            conversationId,
            userId,
            username,
            displayName);

        _connections[connectionId] = connection;
    }

    public VoiceConnectionInfo? RemoveConnection(string connectionId)
    {
        return _connections.TryRemove(connectionId, out var connection)
            ? connection
            : null;
    }

    public IReadOnlyCollection<VoiceParticipant> GetParticipants(int channelId)
    {
        return _connections.Values
            .Where(connection =>
                connection.ConversationId is null &&
                connection.ChannelId == channelId)
            .Select(connection => new VoiceParticipant(
                connection.ConnectionId,
                connection.UserId,
                connection.Username,
                connection.DisplayName))
            .OrderBy(participant => participant.DisplayName)
            .ToArray();
    }

    public IReadOnlyCollection<VoiceParticipant> GetConversationParticipants(
        int conversationId)
    {
        return _connections.Values
            .Where(connection =>
                connection.ConversationId == conversationId)
            .Select(connection => new VoiceParticipant(
                connection.ConnectionId,
                connection.UserId,
                connection.Username,
                connection.DisplayName))
            .OrderBy(participant => participant.DisplayName)
            .ToArray();
    }

    public bool AreInSameRoom(
        string firstConnectionId,
        string secondConnectionId)
    {
        var firstConnection = GetConnection(firstConnectionId);
        var secondConnection = GetConnection(secondConnectionId);

        if (firstConnection is null || secondConnection is null)
        {
            return false;
        }

        var areInSameChannel =
            firstConnection.ConversationId is null &&
            secondConnection.ConversationId is null &&
            firstConnection.ChannelId == secondConnection.ChannelId;

        var areInSameConversation =
            firstConnection.ConversationId.HasValue &&
            secondConnection.ConversationId.HasValue &&
            firstConnection.ConversationId == secondConnection.ConversationId;

        return areInSameChannel || areInSameConversation;
    }
}
namespace Discord.Hubs;

public class UserConnectionTracker
{
    private readonly object _syncRoot =new();

    private readonly Dictionary<int,HashSet<string>>_connections = new();

    public bool AddConnection(int userId,string connectionId)
    {
        lock (_syncRoot)
        {
            if (!_connections.TryGetValue( userId,out var userConnections))
            {
                userConnections = new HashSet<string>();

                _connections[userId] = userConnections;

            }

            var wasOffline = userConnections.Count == 0;


            var wasAdded = userConnections.Add(connectionId);
            return wasOffline && wasAdded;

        }
    }

    
    public bool RemoveConnection(int userId,string connectionId)
    {
        lock (_syncRoot)
        {
            if (!_connections.TryGetValue( userId, out var userConnections))
            {
                return false;
            }

            var wasRemoved =userConnections.Remove(connectionId);
            if ( !wasRemoved ||userConnections.Count > 0)
            {
                return false;
            }

            _connections.Remove(userId);

            return true;
        }
    }
}
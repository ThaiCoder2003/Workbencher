using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

public static class UserConnectionManager
{
    private static readonly ConcurrentDictionary<int, HashSet<string>> _connections = new();

    public static void AddConnection(int userId, string connectionId)
    {
        var connections = _connections.GetOrAdd(userId, _ => new HashSet<string>());

        lock (connections)
        {
            connections.Add(connectionId);
        }
    }

    public static void RemoveConnection(int userId, string connectionId)
    {
        if (_connections.TryGetValue(userId, out var connections))
        {
            lock (connections)
            {
                connections.Remove(connectionId);

                if (connections.Count == 0)
                    _connections.TryRemove(userId, out _);
            }
        }
    }

    public static IEnumerable<string> GetConnections(int userId)
    {
        if (_connections.TryGetValue(userId, out var connections))
        {
            lock (connections)
            {
                return connections.ToList();
            }
        }

        return Enumerable.Empty<string>();
    }
}
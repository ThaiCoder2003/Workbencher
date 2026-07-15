using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

public class ConnectionManager
{
    private readonly ConcurrentDictionary<int, HashSet<string>> _connections = new();

    public void AddConnection(int userId, string connectionId)
    {
        var set = _connections.GetOrAdd(userId, _ => new HashSet<string>());

        lock (set)
        {
            set.Add(connectionId);
        }
    }

    public void RemoveConnection(int userId, string connectionId)
    {
        if (_connections.TryGetValue(userId, out var set))
        {
            lock (set)
            {
                set.Remove(connectionId);

                if (set.Count == 0)
                    _connections.TryRemove(userId, out _);
            }
        }
    }

    public IEnumerable<string> GetConnections(int userId)
    {
        if (_connections.TryGetValue(userId, out var set))
        {
            lock (set)
            {
                return set.ToList();
            }
        }

        return Enumerable.Empty<string>();
    }
}

public static class ChatConnectionManager
{
    public static readonly ConnectionManager Instance = new();
}

public static class ProjectConnectionManager
{
    public static readonly ConnectionManager Instance = new();
}

public static class TaskConnectionManager
{
    public static readonly ConnectionManager Instance = new();
}
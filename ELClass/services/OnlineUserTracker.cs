using System.Collections.Concurrent;

namespace ELClass.services
{
    public class OnlineUserTracker
    {
        // userId -> set of connectionIds
        private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, byte>> _map
            = new();

   
        public bool Add(string userId, string connectionId)
        {
            var conns = _map.GetOrAdd(userId, _ => new ConcurrentDictionary<string, byte>());
            conns.TryAdd(connectionId, 0);
            return conns.Count == 1;
        }

        public bool Remove(string userId, string connectionId)
        {
            if (!_map.TryGetValue(userId, out var conns))
                return false;

            conns.TryRemove(connectionId, out _);

            if (conns.IsEmpty)
            {
                _map.TryRemove(userId, out _);
                return true;
            }

            return false;
        }

        public bool IsOnline(string userId) => _map.ContainsKey(userId);
    }
}

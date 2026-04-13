#nullable enable
using Larnix.Core;
using System.Collections.Generic;
using System.Linq;

namespace Larnix.Socket.Client.Cache;

internal class Cacher<TKey, TValue>
{
    private record CacheValue(TValue Info, long Time);

    private readonly Dictionary<TKey, CacheValue> _infoDict = new();
    private readonly object _lock = new();

    public void Update(TKey key, TValue info)
    {
        CleanOld();

        lock (_lock)
        {
            long time = Timestamp.Now();
            _infoDict[key] = new CacheValue(info, time);
        }
    }

    public void Remove(TKey key)
    {
        CleanOld();

        lock (_lock)
        {
            _infoDict.Remove(key);
        }
    }

    public bool TryGet(TKey key, out TValue info)
    {
        CleanOld();

        lock (_lock)
        {
            if (_infoDict.TryGetValue(key, out CacheValue value))
            {
                info = value.Info;
                return true;
            }

            info = default!;
            return false;
        }
    }

    private void CleanOld()
    {
        lock (_lock)
        {
            foreach (var (key, value) in _infoDict.ToList())
            {
                if (!Timestamp.IsWithin(value.Time))
                {
                    _infoDict.Remove(key);
                }
            }
        }
    }
}

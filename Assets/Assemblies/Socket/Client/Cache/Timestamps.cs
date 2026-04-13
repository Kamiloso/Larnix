#nullable enable
using System;
using System.Collections.Concurrent;
using Larnix.Core;

namespace Larnix.Socket.Client.Cache;

internal class Timestamps<T>
{
    private readonly ConcurrentDictionary<T, long> _offsets = new();

    public void Update(T key, long serverTimestamp)
    {
        long offset = serverTimestamp - Timestamp.Now();
        _offsets[key] = offset;
    }

    public long Get(T key)
    {
        if (!_offsets.TryGetValue(key, out long offset))
            throw new InvalidOperationException($"Server timestamp not initialized.");

        return Timestamp.Now() + offset;
    }

    public bool IsWithin(T key, long timestamp, long windowMs = Timestamp.DEFAULT_WINDOW)
    {
        long serverNow = Get(key);
        return timestamp >= serverNow - windowMs && timestamp <= serverNow;
    }
}
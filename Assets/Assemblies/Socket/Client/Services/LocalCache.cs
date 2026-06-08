#nullable enable
using Larnix.Core;
using Larnix.Socket.Client.Records;
using Larnix.Socket.Payload.Packets;
using System;
using System.Collections.Generic;
using ServerInfoStruct = Larnix.Socket.Payload.Structs.ServerInfo;

namespace Larnix.Socket.Client.Services;

internal static class LocalCache
{
    private static readonly List<CacheEntry> _cache = new();
    private static readonly object _lock = new();

    private record CacheEntry(
        long Timestamp,
        ServerDiscovery Discovery,
        A_ServerInfo Info
        );

    public static void Received(ServerDiscovery discovery, in A_ServerInfo info)
    {
        lock (_lock)
        {
            _cache.RemoveAll(entry =>
                entry.Discovery == discovery ||
                !Timestamp.IsWithin(entry.Timestamp)
                );

            _cache.Add(
                new CacheEntry(
                    Timestamp: Timestamp.Now(),
                    Discovery: discovery,
                    Info: info
                ));
        }
    }

    public static void RemoveWhere(Predicate<ServerDiscovery> predicate)
    {
        lock (_lock)
        {
            _cache.RemoveAll(entry => predicate(entry.Discovery));
        }
    }

    public static bool TryGetTimestamp(ServerIdentity identity, out long result)
    {
        result = default;

        CacheEntry? fitEntry = FindEntry(entry => entry.Discovery.ToServerIdentity() == identity);
        if (fitEntry == null) { return false; }

        long timeNow = Timestamp.Now();
        long recvTime = fitEntry.Timestamp;
        long recvTell = fitEntry.Info.Info.Timestamp;

        result = recvTell + (timeNow - recvTime);
        return true;
    }

    public static bool TryGetInfoStruct(ServerIdentity identity, out ServerInfoStruct result)
    {
        CacheEntry? fitEntry = FindEntry(entry => entry.Discovery.ToServerIdentity() == identity);

        result = fitEntry?.Info.Info ?? default!;
        return fitEntry != null;
    }

    public static bool TryGetFullInfo(ServerDiscovery discovery, out A_ServerInfo result)
    {
        CacheEntry? fitEntry = FindEntry(entry => entry.Discovery == discovery);

        result = fitEntry?.Info ?? default!;
        return fitEntry != null;
    }

    private static CacheEntry? FindEntry(Predicate<CacheEntry> predicate)
    {
        lock (_lock)
        {
            return _cache.Find(entry => predicate(entry));
        }
    }
}
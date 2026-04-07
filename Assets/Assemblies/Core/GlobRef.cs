#nullable enable
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Larnix.Core.Utils;

namespace Larnix.Core;

public static class GlobRef
{
    private static readonly ConcurrentDictionary<long, Dictionary<Type, object>> _keyToData = new();
    private static readonly ConcurrentDictionary<int, long> _threadToKey = new();
    private static int ThreadID => Environment.CurrentManagedThreadId;

    public static long GetKey() => _threadToKey.GetOrAdd(ThreadID, _ => RandUtils.SecureLong());
    public static void SetKey(long key) => _threadToKey[ThreadID] = key;

    public static long NewScope()
    {
        long newKey = RandUtils.SecureLong();
        _threadToKey[ThreadID] = newKey;
        return newKey;
    }

    public static T Set<T>(T instance) where T : class
    {
        var key = GetKey();
        var dict = _keyToData.GetOrAdd(key, _ => new());
        dict[typeof(T)] = instance;
        return instance;
    }

    public static T New<T, U>() where U : T, new() where T : class
    {
        var instance = new U();
        return Set<T>(instance);
    }

    public static T Get<T>() where T : class
    {
        var key = GetKey();
        if (_keyToData.TryGetValue(key, out var dict) &&
            dict.TryGetValue(typeof(T), out var instance))
        {
            return (T)instance;
        }
        throw new KeyNotFoundException($"No instance of type {typeof(T)} found for current thread.");
    }

    public static bool Has<T>() where T : class
    {
        var key = GetKey();
        return (_keyToData.TryGetValue(key, out var dict) &&
            dict.ContainsKey(typeof(T)));
    }

    public static void Clear()
    {
        var key = GetKey();
        _keyToData.TryRemove(key, out _);
        _threadToKey.TryRemove(ThreadID, out _);
    }
}

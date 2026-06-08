#nullable enable
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Larnix.Core.Utils;

namespace Larnix.Core;

/// <summary>
/// You, as a user of this class are fully responsible for ensuring that a specific key
/// is being held by at most one thread at a time. By the way,
/// cleaning resources is not done automagically. You need to do it yourself.
/// </summary>
public static class GlobRef
{
    private static readonly ConcurrentDictionary<int, long> _threadIdToKey = new();
    private static readonly ConcurrentDictionary<long, ConcurrentDictionary<Type, object>> _keyToData = new();

    private static int ThreadId => Environment.CurrentManagedThreadId;

    // --- Key Management ---

    public static long NewScope()
    {
        return SetKey(RandUtils.SecureLong());
    }

    public static long SetKey(long key)
    {
        return _threadIdToKey[ThreadId] = key;
    }

    public static long GetKey()
    {
        return _threadIdToKey.GetOrAdd(ThreadId, _ => RandUtils.SecureLong());
    }

    public static void Clear()
    {
        long key = GetKey();

        _keyToData.TryRemove(key, out _);
        _threadIdToKey.TryRemove(ThreadId, out _);
    }

    // --- Data Management ---

    public static T Set<T>(T instance) where T : class
    {
        long key = GetKey();
        var dict = _keyToData.GetOrAdd(key, _ => new());

        return (T)(dict[typeof(T)] = instance);
    }

    public static T New<T, U>() where T : class where U : T, new()
    {
        return Set<T>(new U());
    }

    public static T Get<T>() where T : class
    {
        long key = GetKey();
        var dict = _keyToData.GetOrAdd(key, _ => new());

        if (!dict.TryGetValue(typeof(T), out var instance))
            throw new KeyNotFoundException($"No instance of type {typeof(T)} found for current thread.");

        return (T)instance;
    }
    
    public static bool Has<T>() where T : class
    {
        long key = GetKey();
        var dict = _keyToData.GetOrAdd(key, _ => new());

        return dict.ContainsKey(typeof(T));
    }
}

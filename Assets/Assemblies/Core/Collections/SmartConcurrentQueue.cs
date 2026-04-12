#nullable enable
using System.Collections.Concurrent;
using System.Threading;

namespace Larnix.Core.Collections;

public class SmartConcurrentQueue<T>
{
    private readonly ConcurrentQueue<T> _queue = new();
    private volatile int _count = 0;

    public int Count => _count;

    public void Enqueue(T item)
    {
        _queue.Enqueue(item);
        Interlocked.Increment(ref _count);
    }

    public bool TryDequeue(out T result)
    {
        if (_queue.TryDequeue(out result))
        {
            Interlocked.Decrement(ref _count);
            return true;
        }

        return false;
    }

    public void DropUntilCount(int maxLimit)
    {
        while (_count > maxLimit)
        {
            TryDequeue(out _);
        }
    }
}

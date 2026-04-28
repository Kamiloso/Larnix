#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;

namespace Larnix.Core;

public class Coroutines : ITickable, IDisposable
{
    private interface ICoroutine
    {
        bool IsFinished { get; }
        void MoveNext();
        void Break();
    }

    private class Coroutine<T> : ICoroutine where T : struct
    {
        public bool IsFinished { get; private set; }

        private readonly IEnumerator<T?> _enumerator;
        private readonly Action<T>? _onResult;

        public Coroutine(IEnumerator<T?> enumerator, Action<T>? onResult = null)
        {
            _enumerator = enumerator;
            _onResult = onResult;
        }

        public void MoveNext()
        {
            if (IsFinished) return;

            if (_enumerator.MoveNext())
            {
                T? current = _enumerator.Current;

                if (current.HasValue)
                {
                    _onResult?.Invoke(current.Value);
                    Break();
                }
            }
            else
            {
                Break();
            }
        }

        public void Break()
        {
            if (IsFinished) return;

            _enumerator.Dispose();
            IsFinished = true;
        }
    }

    private readonly List<ICoroutine> _coroutines = new();

    public void Start<T>(IEnumerable<T?> method, Action<T>? onResult = null) where T : struct
    {
        ICoroutine coroutine = new Coroutine<T>(
            enumerator: method.GetEnumerator(),
            onResult: onResult
            );

        coroutine.MoveNext();

        if (!coroutine.IsFinished)
        {
            _coroutines.Add(coroutine);
        }
    }

    public void Tick(float deltaTime)
    {
        foreach (var coroutine in _coroutines.ToList())
        {
            coroutine.MoveNext();

            if (coroutine.IsFinished)
            {
                _coroutines.Remove(coroutine);
            }
        }
    }

    public void Dispose()
    {
        foreach (var coroutine in _coroutines.ToList())
        {
            _coroutines.Remove(coroutine);

            coroutine.Break();
        }
    }
}

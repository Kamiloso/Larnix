using System;
using System.Collections;
using System.Collections.Generic;
using Larnix.Core;

namespace Larnix.Core.Coroutines
{
    public class CoroutineRunner : ITickable, IDisposable
    {
        private class Coroutine : IDisposable
        {
            private IEnumerator _routine;
            private Action<object> _onResult;
            private bool _ended = false; // obtained result
            private bool _disposed = false;

            public static Coroutine Create<T>(IEnumerator<Box<T>> routine, Action<T> onResult = null)
            {
                return new Coroutine(routine, obj => onResult?.Invoke((T)obj));
            }

            private Coroutine(IEnumerator routine, Action<object> onResult)
            {
                if (routine == null || onResult == null)
                    throw new ArgumentNullException(routine == null ? nameof(routine) : nameof(onResult));

                _routine = routine;
                _onResult = onResult;
            }

            public bool MoveNext()
            {
                if (_disposed)
                    throw new ObjectDisposedException(nameof(Coroutine));

                if (_ended)
                    return false;

                bool crAlive = _routine.MoveNext();
                if (crAlive)
                {
                    Box<object> result = ((IBox)_routine.Current)?.AsObject();
                    if (result == null)
                    {
                        return true;
                    }
                    
                    _onResult?.Invoke(result.Value);
                }

                _ended = true;
                return false;
            }

            public void Dispose()
            {
                if (!_disposed)
                {
                    _disposed = true;
                    (_routine as IDisposable)?.Dispose();
                }
            }
        }

        public int Count => _routines.Count;
        private readonly List<Coroutine> _routines = new();
        private bool _disposed = false;

        public void Start<T>(IEnumerator<Box<T>> routine, Action<T> onResult = null)
        {
            if (routine == null)
                throw new ArgumentNullException(nameof(routine));

            _routines.Add(
                Coroutine.Create(routine, onResult)
            );
        }

        public void Tick(float deltaTime)
        {
            for (int i = _routines.Count - 1; i >= 0; i--)
            {
                var rt = _routines[i];
                bool alive = rt.MoveNext();

                if (!alive)
                {
                    rt.Dispose();
                    _routines.RemoveAt(i);
                }
            }
        }

        public void StopAll()
        {
            foreach (var rt in _routines)
            {
                rt.Dispose();
            }
            _routines.Clear();
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _disposed = true;
                StopAll();
            }
        }
    }
}

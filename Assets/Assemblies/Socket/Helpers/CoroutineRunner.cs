using System;
using System.Collections;
using System.Collections.Generic;

namespace Larnix.Socket.Helpers
{
    internal class CoroutineRunner : IDisposable
    {
        private readonly List<IEnumerator> _routines = new();

        public int Count => _routines.Count;

        public void Start(IEnumerator routine)
        {
            if (routine == null) throw new ArgumentNullException(nameof(routine));
            _routines.Add(routine);
        }

        public void StopAll()
        {
            _routines.Clear();
        }

        public void Tick()
        {
            for (int i = _routines.Count - 1; i >= 0; i--)
            {
                var r = _routines[i];
                bool alive = r.MoveNext();

                if (!alive)
                {
                    _routines.RemoveAt(i);
                }
            }
        }

        public void Dispose()
        {
            StopAll();
        }
    }
}

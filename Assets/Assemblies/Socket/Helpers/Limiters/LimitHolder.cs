using System;

namespace Larnix.Socket.Helpers.Limiters
{
    public struct LimitHolder : IDisposable
    {
        private readonly Action _remove;
        private bool _disposed;

        public LimitHolder(Func<bool> tryAdd, Action remove, out bool acquired)
        {
            acquired = tryAdd();

            _remove = acquired ?
                remove : null;

            _disposed = false;
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _disposed = true;
                _remove?.Invoke();
            }
        }
    }
}
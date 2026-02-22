using Larnix.Core;
using System;
using System.Collections.Generic;

namespace Larnix.Core.DataStructures
{
    public class DisposableStack : IDisposable
    {
        private Stack<IDisposable> _disposables = new();
        private bool _disposed;

        public void Push(params IDisposable[] disposables)
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(DisposableStack));
            }

            if (disposables == null)
            {
                throw new ArgumentNullException(nameof(disposables));
            }

            foreach (var disposable in disposables)
            {
                if (disposable == null)
                {
                    throw new ArgumentException("Disposables cannot contain null.", nameof(disposables));
                }

                _disposables.Push(disposable);
            }
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _disposed = true;

                while (_disposables.Count > 0)
                {
                    _disposables.Pop().Dispose();
                }
            }
        }
    }
}

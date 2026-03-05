using System;

namespace Larnix.Core
{
    public interface IDisposable2 : IDisposable
    {
        void Dispose(bool emergency);
        void IDisposable.Dispose() => Dispose(false);
    }
}

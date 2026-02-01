using System;

namespace Larnix.Core.References
{
    public abstract class RefObject<R> where R : RefRoot
    {
        protected readonly R ThisRoot;

        protected RefObject(R root) => ThisRoot = root;
        protected RefObject(RefObject<R> reff) => ThisRoot = reff.ThisRoot;

        protected T Ref<T>() where T : class
        {
            return ThisRoot.Ref<T>();
        }
    }
}

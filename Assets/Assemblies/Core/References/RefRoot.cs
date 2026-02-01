using System;
using System.Collections.Generic;

namespace Larnix.Core.References
{
    public abstract class RefRoot
    {
        private readonly LinkedList<Type> _refOrder = new();
        private readonly Dictionary<Type, Object> _registeredRefs = new();

        public void AddRef(Object obj)
        {
            Type type = obj.GetType();
            if (!_registeredRefs.ContainsKey(type))
            {
                _registeredRefs[type] = obj;
                _refOrder.AddLast(type);
            }
        }

        public T Ref<T>() where T : class
        {
            return _registeredRefs.TryGetValue(typeof(T), out Object obj) ? (T)obj : null;
        }

        protected LinkedList<Object> TakeRefSnapshot()
        {
            LinkedList<Object> snapshot = new();
            foreach (Type t in _refOrder)
            {
                snapshot.AddLast(_registeredRefs[t]);
            }
            return snapshot;
        }
    }
}
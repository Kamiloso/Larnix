using System;
using System.Collections.Generic;

namespace Larnix.Core.References
{
    public abstract class RefRoot
    {
        private readonly LinkedList<Type> _refOrder = new();
        private readonly Dictionary<Type, object> _registeredRefs = new();

        public void AddRef(object obj)
        {
            Type type = obj.GetType();
            if (!_registeredRefs.ContainsKey(type))
            {
                _registeredRefs[type] = obj;
                _refOrder.AddLast(type);
            }
        }

        public void AddRefs(params object[] objs)
        {
            foreach (object obj in objs)
            {
                AddRef(obj);
            }
        }

        public T Ref<T>() where T : class
        {
            return _registeredRefs.TryGetValue(typeof(T), out object obj) ? (T)obj : null;
        }

        protected LinkedList<object> TakeRefSnapshot()
        {
            LinkedList<object> snapshot = new();
            foreach (Type t in _refOrder)
            {
                snapshot.AddLast(_registeredRefs[t]);
            }
            return snapshot;
        }
    }
}
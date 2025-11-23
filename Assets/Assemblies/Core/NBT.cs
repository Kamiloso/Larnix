using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.Serialization;
using Socket;

namespace Larnix.Core
{
    public class NBT
    {
        private byte[] _data;
        private Dictionary<string, object> _variables = new(); // <string, Variable<T>>

        public byte[] Data => _data;

        private class Variable<T> where T : unmanaged
        {
            public Action<T, int> Set;
            public Func<int, T> Get;
        }

        protected NBT(int capacity = 0)
        {
            _data = new byte[capacity];
        }

        protected NBT(byte[] data)
        {
            _data = data;
        }

        public U Copy<U>() where U : NBT
        {
            byte[] cloned = new byte[_data.Length];
            Buffer.BlockCopy(_data, 0, cloned, 0, _data.Length);
            U nbt = (U)FormatterServices.GetSafeUninitializedObject(typeof(U));

            nbt._data = cloned;
            nbt._variables = new(_variables);

            return nbt;
        }

        protected void Define<T>(string name, int pos) where T : unmanaged
        {
            _variables[name] = new Variable<T>
            {
                Set = (value, delta) => _Set(pos + delta, value),
                Get = (delta) => _Get<T>(pos + delta)
            };

            Set<T>(name, default);
        }

        public void Set<T>(string name, T value, int index = 0) where T : unmanaged
        {
            if (!_variables.TryGetValue(name, out var val))
                throw new KeyNotFoundException($"Variable {name} doesn't exist!");

            if (val is Variable<T> val2)
            {
                val2.Set(value, index * _SizeOf<T>());
            }
            else throw new InvalidOperationException(
                $"Variable '{name}' has type {val.GetType().Name}, but tried to assign {typeof(T).Name}");
        }

        public T Get<T>(string name, int index = 0) where T : unmanaged
        {
            if (!_variables.TryGetValue(name, out var val))
                throw new KeyNotFoundException($"Variable {name} doesn't exist!");

            if (val is Variable<T> val2)
            {
                return val2.Get(index * _SizeOf<T>());
            }
            else throw new InvalidOperationException(
                $"Variable '{name}' has type {val.GetType().Name}, but tried to assign {typeof(T).Name}");
        }

        private void _Set<T>(int pos, T value) where T : unmanaged
        {
            _ReallocateIfNeeded<T>(pos);

            byte[] serialized = EndianUnsafe.GetBytes(value);
            Buffer.BlockCopy(serialized, 0, _data, pos, serialized.Length);
        }

        private T _Get<T>(int pos) where T : unmanaged
        {
            _ReallocateIfNeeded<T>(pos);

            T value = EndianUnsafe.FromBytes<T>(_data, pos);
            return value;
        }

        private void _ReallocateIfNeeded<T>(int pos) where T : unmanaged
        {
            int lngt = _SizeOf<T>();
            if (pos + lngt >= _data.Length)
            {
                byte[] newArray = new byte[pos + lngt];
                Buffer.BlockCopy(_data, 0, newArray, 0, _data.Length);
                _data = newArray;
            }
        }

        private unsafe int _SizeOf<T>() where T : unmanaged
        {
            return sizeof(T);
        }
    }
}

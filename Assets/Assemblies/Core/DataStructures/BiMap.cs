using System;
using System.Collections.Generic;

namespace Larnix.Core
{
    public class BiMap<K, V>
    {
        private readonly Dictionary<K, V> _keyToValue;
        private readonly Dictionary<V, K> _valueToKey;

        public int Count => _keyToValue.Count;
        public IEnumerable<K> Keys => _keyToValue.Keys;
        public IEnumerable<V> Values => _valueToKey.Keys;

        public V this[K key] => GetValue(key);
        public K this[V value] => GetKey(value);

        public BiMap()
        {
            _keyToValue = new();
            _valueToKey = new();
        }

        public BiMap(int capacity)
        {
            _keyToValue = new(capacity);
            _valueToKey = new(capacity);
        }

        public BiMap(BiMap<K, V> original)
        {
            _keyToValue = new(original._keyToValue);
            _valueToKey = new(original._valueToKey);
        }
        
        public void SetPair(K key, V value)
        {
            if (key == null) throw new ArgumentNullException(nameof(key));
            if (value == null) throw new ArgumentNullException(nameof(value));

            if (_keyToValue.TryGetValue(key, out var oldValue))
            {
                _valueToKey.Remove(oldValue);
            }

            if (_valueToKey.TryGetValue(value, out var oldKey))
            {
                _keyToValue.Remove(oldKey);
            }

            _keyToValue[key] = value;
            _valueToKey[value] = key;
        }

        public bool ContainsKey(K key) => _keyToValue.ContainsKey(key);
        public bool ContainsValue(V value) => _valueToKey.ContainsKey(value);

        public bool TryGetValue(K key, out V value)
        {
            return _keyToValue.TryGetValue(key, out value);
        }

        public bool TryGetKey(V value, out K key)
        {
            return _valueToKey.TryGetValue(value, out key);
        }

        public V GetValue(K key)
        {
            if (!_keyToValue.TryGetValue(key, out var value))
                throw new KeyNotFoundException($"Key '{key}' not found.");

            return value;
        }

        public K GetKey(V value)
        {
            if (!_valueToKey.TryGetValue(value, out var key))
                throw new KeyNotFoundException($"Value '{value}' not found.");

            return key;
        }

        public bool RemoveByKey(K key)
        {
            if (!TryGetValue(key, out var value))
                return false;

            _keyToValue.Remove(key);
            _valueToKey.Remove(value);
            return true;
        }

        public bool RemoveByValue(V value)
        {
            if (!TryGetKey(value, out var key))
                return false;

            _valueToKey.Remove(value);
            _keyToValue.Remove(key);
            return true;
        }

        public void Clear()
        {
            _keyToValue.Clear();
            _valueToKey.Clear();
        }
    }
}

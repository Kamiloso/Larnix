using System;
using System.Linq;

namespace Larnix.Core.Binary
{
    public interface IBinary<T> where T : IBinary<T>, new()
    {
        public byte[] Serialize();
        public bool Deserialize(byte[] data, int offset = 0);

        internal static T Create(byte[] data, int offset = 0)
        {
            return TryCreate(data, out T result, offset) ?
                result : throw new FormatException("Deserialization failed for type " + typeof(T).Name);
        }

        internal static bool TryCreate(byte[] data, out T result, int offset = 0)
        {
            T instance = new T();
            if (instance.Deserialize(data, offset))
            {
                result = instance;
                return true;
            }

            result = default;
            return false;
        }

        public T BinaryCopy() => Create(Serialize());
        public bool BinaryEquals(T other) =>
            other != null && Serialize().SequenceEqual(other.Serialize());
    }
}

using System;

namespace Larnix.Core.Binary
{
    public interface IFixedBinary<T> where T : IFixedBinary<T>, new()
    {
        public int SIZE { get; }

        public byte[] Serialize();
        public bool Deserialize(byte[] data, int offset = 0);
        
        public T BinaryCopy() => Create(Serialize());

        public static T Create(byte[] data, int offset = 0)
        {
            return TryCreate(data, out T result, offset) ?
                result : throw new FormatException("Deserialization failed for type " + typeof(T).Name);
        }

        public static bool TryCreate(byte[] data, out T result, int offset = 0)
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
    }
}

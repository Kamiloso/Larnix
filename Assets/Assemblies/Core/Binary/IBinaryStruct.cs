using System;

namespace Larnix.Core.Binary
{
    public interface IBinaryStruct<T> where T : IBinaryStruct<T>, new()
    {
        public bool IS_FIXED_SIZE => SIZE > 0;
        public virtual int SIZE => 0;

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

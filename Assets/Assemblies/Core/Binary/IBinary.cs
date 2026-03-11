using System;

namespace Larnix.Core.Binary
{
    public interface IBinary<T> where T : struct, IBinary<T>
    {
        public byte[] Serialize();
        public bool Deserialize(byte[] data, int offset, out T result);

        internal static T Create(byte[] data, int offset = 0)
        {
            return TryCreate(data, out T result, offset) ?
                result : throw new FormatException("Deserialization failed for type " + typeof(T).Name);
        }

        internal static bool TryCreate(byte[] data, out T result, int offset = 0)
        {
            T slave = new();
            if (slave.Deserialize(data, offset, out result))
            {
                return true;
            }

            result = default;
            return false;
        }
    }
}

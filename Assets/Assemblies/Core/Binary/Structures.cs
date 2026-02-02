using System;

namespace Larnix.Core.Binary
{
    public static class Structures
    {
        public static byte[] GetBytes<T>(IBinary<T> structure) where T : IBinary<T>, new()
        {
            return structure.Serialize();
        }

        public static T FromBytes<T>(byte[] bytes, int startIndex = 0) where T : IBinary<T>, new()
        {
            return IBinary<T>.Create(bytes, startIndex);
        }

        public static byte[] ArrayGetBytes<T>(T[] values, int SIZE) where T : IBinary<T>, new()
        {
            if (values == null)
                throw new ArgumentNullException(nameof(values));

            byte[] result = new byte[SIZE * values.Length];

            for (int i = 0; i < values.Length; i++)
            {
                byte[] elementBytes = GetBytes(values[i]);
                Buffer.BlockCopy(elementBytes, 0, result, i * SIZE, SIZE);
            }

            return result;
        }

        public static T[] ArrayFromBytes<T>(byte[] bytes, int count, int SIZE, int startIndex = 0) where T : IBinary<T>, new()
        {
            if (bytes == null)
                throw new ArgumentNullException(nameof(bytes));

            int needed = count * SIZE;

            if (bytes.Length < startIndex + needed)
                throw new ArgumentException($"Array too short to read {count} elements of type {typeof(T).Name}");

            T[] result = new T[count];

            for (int i = 0; i < count; i++)
            {
                result[i] = FromBytes<T>(bytes, startIndex + i * SIZE);
            }

            return result;
        }
    }
}

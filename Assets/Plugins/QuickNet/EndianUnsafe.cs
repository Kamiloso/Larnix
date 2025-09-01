using System;

namespace QuickNet
{
    public static unsafe class EndianUnsafe
    {
        private const bool LittleEndian = true;

        public static byte[] GetBytes<T>(T value) where T : unmanaged
        {
            int size = sizeof(T);
            byte[] bytes = new byte[size];

            fixed (byte* b = bytes)
            {
                *(T*)b = value;
            }

            if (BitConverter.IsLittleEndian != LittleEndian && !typeof(IIgnoresEndianness).IsAssignableFrom(typeof(T)))
                Array.Reverse(bytes);

            return bytes;
        }

        public static T FromBytes<T>(byte[] bytes, int startIndex = 0) where T : unmanaged
        {
            int size = sizeof(T);
            if (bytes.Length < startIndex + size)
                throw new ArgumentException("Array too short to read type " + typeof(T).Name);

            Span<byte> tmp = stackalloc byte[size];
            bytes.AsSpan(startIndex, size).CopyTo(tmp);

            if (BitConverter.IsLittleEndian != LittleEndian && !typeof(IIgnoresEndianness).IsAssignableFrom(typeof(T)))
                tmp.Reverse();

            fixed (byte* b = tmp)
            {
                return *(T*)b;
            }
        }

        public static byte[] ArrayGetBytes<T>(T[] values) where T : unmanaged
        {
            if (values == null)
                throw new ArgumentNullException(nameof(values));

            int size = sizeof(T);
            byte[] result = new byte[size * values.Length];

            for (int i = 0; i < values.Length; i++)
            {
                byte[] elementBytes = GetBytes(values[i]);
                Buffer.BlockCopy(elementBytes, 0, result, i * size, size);
            }

            return result;
        }

        public static T[] ArrayFromBytes<T>(byte[] bytes, int count, int startIndex = 0) where T : unmanaged
        {
            if (bytes == null)
                throw new ArgumentNullException(nameof(bytes));

            int size = sizeof(T);
            int needed = count * size;

            if (bytes.Length < startIndex + needed)
                throw new ArgumentException($"Array too short to read {count} elements of type {typeof(T).Name}");

            T[] result = new T[count];

            for (int i = 0; i < count; i++)
            {
                result[i] = FromBytes<T>(bytes, startIndex + i * size);
            }

            return result;
        }
    }
}

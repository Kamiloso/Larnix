using System;

namespace Larnix
{
    public static unsafe class EndianUnsafe
    {
        private const bool LITTLE_ENDIAN = true;

        public static byte[] GetBytes<T>(T value) where T : unmanaged
        {
            int size = sizeof(T);
            byte[] bytes = new byte[size];

            fixed (byte* b = bytes)
            {
                *(T*)b = value;
            }

            if (BitConverter.IsLittleEndian != LITTLE_ENDIAN)
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

            if (BitConverter.IsLittleEndian != LITTLE_ENDIAN)
                tmp.Reverse();

            fixed (byte* b = tmp)
            {
                return *(T*)b;
            }
        }
    }
}

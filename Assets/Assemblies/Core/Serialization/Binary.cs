#nullable enable
using System;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;

namespace Larnix.Core.Serialization;

public static unsafe partial class Binary<T> where T : unmanaged
{
    public static int Size => sizeof(T);

    static Binary()
    {
        if (!BitConverter.IsLittleEndian)
            throw new PlatformNotSupportedException("Only little-endian platforms are supported.");

        if (!IsSupportedType(typeof(T)))
            throw new NotSupportedException($"Type {typeof(T)} is not supported.");
    }

    public static byte[] Serialize(in T obj)
    {
        return MemoryMarshal.AsBytes(
            MemoryMarshal.CreateReadOnlySpan(ref Unsafe.AsRef(in obj), 1)
        ).ToArray();
    }

    public static T Deserialize(byte[] bytes, int offset = 0)
    {
        if (offset < 0 || offset > bytes.Length - sizeof(T))
            throw new ArgumentOutOfRangeException(nameof(offset), "Byte array size mismatch.");

        return Sanitizer.Filter(
            MemoryMarshal.Read<T>(bytes.AsSpan(offset))
            );
    }

    public static byte[] SerializeArray(T[] array)
    {
        return MemoryMarshal.AsBytes(array.AsSpan()).ToArray();
    }

    public static T[] DeserializeArray(byte[] bytes, int count, int offset = 0)
    {
        if (count < 0)
            throw new ArgumentOutOfRangeException(nameof(count), "Count cannot be negative.");

        if (offset < 0 || offset + (long)count * sizeof(T) > bytes.Length)
            throw new ArgumentOutOfRangeException(nameof(offset), "Byte array size mismatch.");

        T[] array = MemoryMarshal.Cast<byte, T>(
            bytes.AsSpan(offset, count * sizeof(T))
        ).ToArray();

        foreach (ref T item in array.AsSpan())
        {
            item = Sanitizer.Filter(item);
        }

        return array;
    }
}

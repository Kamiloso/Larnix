#nullable enable
using System;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;

namespace Larnix.Core;

/// <summary>
/// By applying this interface you promise that the struct that it is being
/// applied has a fully deterministic memory layout. That means it doesn't
/// contain any reference types, bools or other weird members and has a
/// sequential layout with no padding.
/// </summary>
public interface IFixedStruct<T> where T : unmanaged
{
    T Sanitize() => (T)this;
}

public static unsafe class Binary<T> where T : unmanaged
{
    public static int Size => sizeof(T);

    static Binary()
    {
        if (!BitConverter.IsLittleEndian)
            throw new PlatformNotSupportedException("Only little-endian platforms are supported.");

        if (!IsSupportedType())
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

        T desrl = MemoryMarshal.Read<T>(bytes.AsSpan(offset));
        return desrl is IFixedStruct<T> rs ? rs.Sanitize() : desrl;
    }

    public static byte[] SerializeArray(T[] array)
    {
        return MemoryMarshal.AsBytes(array.AsSpan()).ToArray();
    }

    public static T[] DeserializeArray(byte[] bytes, int count, int offset = 0)
    {
        if (count < 0)
            throw new ArgumentOutOfRangeException(nameof(count), "Count cannot be negative.");

        if (offset < 0 || offset + ((long)count * sizeof(T)) > bytes.Length)
            throw new ArgumentOutOfRangeException(nameof(offset), "Byte array size mismatch.");

        T[] results = new T[count];
        for (int i = 0; i < count; i++)
        {
            results[i] = Deserialize(bytes, offset + i * sizeof(T));
        }
        return results;
    }

    private static bool IsSupportedType()
    {
        if (typeof(T) == typeof(bool))
            return false;

        if (typeof(T).IsPrimitive || typeof(T).IsEnum)
            return true;

        if (!typeof(IFixedStruct<T>).IsAssignableFrom(typeof(T)))
            return false;

        StructLayoutAttribute? layout = typeof(T).StructLayoutAttribute;
        if (layout == null || layout.Value != LayoutKind.Sequential || layout.Pack != 1)
        {
            return false;
        }

        return true;
    }
}

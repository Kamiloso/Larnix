#nullable enable
using System;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
using System.Reflection;

namespace Larnix.Core.Serialization;

public interface ISanitizable<T> where T : unmanaged
{
    T Sanitize();
}

public static unsafe class Binary<T> where T : unmanaged
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

        T item = MemoryMarshal.Read<T>(bytes.AsSpan(offset));
        
        if (item is ISanitizable<T> sanitizable)
            item = sanitizable.Sanitize();

        return item;
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
            if (item is ISanitizable<T> sanitizable)
                item = sanitizable.Sanitize();
        }

        return array;
    }

    private static bool IsSupportedType(Type type)
    {
        if (type == typeof(bool) || type == typeof(decimal))
        {
            return false;
        }

        if (type.IsPrimitive || type.IsEnum)
        {
            return true;
        }

        if (!type.IsValueType)
        {
            return false;
        }

        FieldInfo[] fields = type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (fields.Length == 0)
        {
            return false; // something weird, better not allow it
        }

        foreach (FieldInfo field in fields)
        {
            FixedBufferAttribute? fixedBuffer = field.GetCustomAttribute<FixedBufferAttribute>();
            if (fixedBuffer == null) // normal struct field
            {
                if (!IsSupportedType(field.FieldType))
                {
                    return false;
                }
            }
            else // fixed-size buffer field
            {
                if (!IsSupportedType(fixedBuffer.ElementType))
                {
                    return false;
                }
            }
        }

        StructLayoutAttribute? layout = type.StructLayoutAttribute;
        if (layout == null || layout.Value != LayoutKind.Sequential || layout.Pack != 1)
        {
            return false;
        }

        return true;
    }
}

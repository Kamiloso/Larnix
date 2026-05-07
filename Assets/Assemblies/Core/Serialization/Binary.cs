#nullable enable
using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Larnix.Core.Serialization;

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

        return Sanitizer.Filter(
            MemoryMarshal.Read<T>(bytes.AsSpan(offset))
            );
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

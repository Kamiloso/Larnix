#nullable enable
using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Larnix.Core.Serialization;

public static unsafe partial class Binary<T> where T : unmanaged
{
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

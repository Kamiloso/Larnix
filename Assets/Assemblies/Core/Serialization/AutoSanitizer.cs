#nullable enable
using System;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Larnix.Core.Serialization;

internal static unsafe class AutoSanitizer<T> where T : unmanaged
{
    private static readonly delegate*<ref T, T> _sanitizePtr;
    private static readonly T[]? _enumValues;

    static AutoSanitizer()
    {
        _ = Binary<T>.Size; // trigger static constructor

        if (typeof(T).IsEnum) // enum sanitization
        {
            static T ConvertEnum(ref T value)
            {
                int index = Array.IndexOf(_enumValues!, value);
                return index >= 0 ? value : default;
            }

            _sanitizePtr = &ConvertEnum;
            _enumValues = (T[])Enum.GetValues(typeof(T));
        }

        if (typeof(ISanitizable<T>).IsAssignableFrom(typeof(T))) // sanitizable interface
        {
            Type typeSanit = typeof(ISanitizable<T>);

            var interfaceMethod = typeSanit.GetMethod(nameof(ISanitizable<T>.Sanitize));
            var map = typeof(T).GetInterfaceMap(typeSanit);

            int methodIndex = Array.IndexOf(map.InterfaceMethods, interfaceMethod!);
            MethodInfo targetMethod = map.TargetMethods[methodIndex];

            _sanitizePtr = (delegate*<ref T, T>)targetMethod.MethodHandle.GetFunctionPointer();
        }
    }

    public static T Filter(in T value)
    {
        if (_sanitizePtr != null)
        {
            return _sanitizePtr(ref Unsafe.AsRef(in value));
        }

        return value;
    }
}

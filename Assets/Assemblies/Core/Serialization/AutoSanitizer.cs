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

        if (typeof(ISanitizable<T>).IsAssignableFrom(typeof(T))) // explicit
        {
            Type typeSanit = typeof(ISanitizable<T>);

            var interfaceMethod = typeSanit.GetMethod(nameof(ISanitizable<T>.Sanitize));
            var map = typeof(T).GetInterfaceMap(typeSanit);

            int methodIndex = Array.IndexOf(map.InterfaceMethods, interfaceMethod!);
            MethodInfo targetMethod = map.TargetMethods[methodIndex];

            _sanitizePtr = (delegate*<ref T, T>)targetMethod.MethodHandle.GetFunctionPointer();
        }

        if (typeof(IFixedBuffer<>).IsAssignableFrom(typeof(T))) // fixed buffer
        {
            Type elementType = typeof(T).GetInterfaces()
                .First(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IFixedBuffer<>))
                .GetGenericArguments()[0];

            static TT ConvertFixedBuffer<TT, U>(ref TT value)
                where TT : unmanaged, IFixedBuffer<U>
                where U : unmanaged
            {
                TT result = new();
                for (int i = 0; i < value.Count; i++)
                {
                    U elem = value.At(i);
                    U sanitized = Sanitizer.Filter(elem);
                    result.Add(sanitized);
                }

                return result;
            }

            throw new NotImplementedException(); // TODO: implement
        }

        if (typeof(T).IsEnum) // enum
        {
            static T ConvertEnum(ref T value)
            {
                int index = Array.IndexOf(_enumValues!, value);
                return index >= 0 ? value : default;
            }

            _sanitizePtr = &ConvertEnum;
            _enumValues = (T[])Enum.GetValues(typeof(T));
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

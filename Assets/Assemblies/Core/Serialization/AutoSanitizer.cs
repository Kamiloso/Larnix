#nullable enable
using System;
using System.Reflection;

namespace Larnix.Core.Serialization;

internal static class AutoSanitizer<T> where T : unmanaged
{
    private delegate T RefSanitizer(ref T value);
    private delegate T Sanitizer(in T value);

    private static readonly Sanitizer? _sanitizer;

    static AutoSanitizer()
    {
        _ = Binary<T>.Size; // trigger static constructor

        if (typeof(ISanitizable<T>).IsAssignableFrom(typeof(T)))
        {
            Type typeSanit = typeof(ISanitizable<T>);

            var interfaceMethod = typeSanit.GetMethod(nameof(ISanitizable<T>.Sanitize));
            var map = typeof(T).GetInterfaceMap(typeSanit);

            int methodIndex = Array.IndexOf(map.InterfaceMethods, interfaceMethod);
            MethodInfo targetMethod = map.TargetMethods[methodIndex];

            var refSanitizer = (RefSanitizer)targetMethod.CreateDelegate(typeof(RefSanitizer));

            _sanitizer = (in T value) =>
            {
                T tmp = value;
                return refSanitizer(ref tmp);
            };
        }

        else if (typeof(T).IsEnum)
        {
            T[] enumValues = (T[])Enum.GetValues(typeof(T));

            _sanitizer = (in T value) =>
            {
                int index = Array.IndexOf(enumValues, value);
                return index >= 0 ? value : default;
            };
        }
    }

    public static T Filter(in T value)
    {
        return _sanitizer?.Invoke(value) ?? value;
    }
}

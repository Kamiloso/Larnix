#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Larnix.Core.Utils;

namespace Larnix.Socket.Payload;

public delegate void CmdHandler<T>(in T cmd) where T : unmanaged;

[AttributeUsage(AttributeTargets.Struct, AllowMultiple = false)]
public class CmdIdAttribute : Attribute
{
    public short Id { get; }

    public CmdIdAttribute(short id)
    {
        Id = id;
    }
}

internal static class Cmd
{
    // read from dictionary is thread-safe
    private static readonly Dictionary<Type, short> _idByType = new();

    static Cmd() // static ctor guarantees this runs only once and is thread-safe
    {
        AppDomain.CurrentDomain
            .GetAssemblies()
            .SelectMany(a => ReflectionUtils.GetLoadableTypes(a))
            .Where(t => t.GetCustomAttribute<CmdIdAttribute>() != null)
            .Where(t => !t.IsGenericType)
            .ToList()
            .ForEach(type =>
            {
                short id = type.GetCustomAttribute<CmdIdAttribute>()!.Id;
                if (!_idByType.ContainsKey(type))
                {
                    if (_idByType.ContainsValue(id))
                    {
                        string exstName = _idByType.First(kv => kv.Value == id).Key.FullName;
                        string typeName = type.FullName;

                        throw new InvalidOperationException(
                            $"Duplicate CmdID {id} for types {exstName} and {typeName}.");
                    }

                    _idByType[type] = id;
                }
            });
    }

    public static short Id<T>() where T : unmanaged
    {
        if (!_idByType.TryGetValue(typeof(T), out short id))
        {
            throw new InvalidOperationException(
                $"Type {typeof(T).FullName} does not have a CmdId attribute or is a generic type.");
        }
        return id;
    }
}

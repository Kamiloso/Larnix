#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Larnix.Core.Utils;

namespace Larnix.Socket.Payload;

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

    public static short Value<T>() where T : unmanaged
    {
        return _idByType.TryGetValue(typeof(T), out short id) ? id :
            throw new InvalidOperationException($"Type {typeof(T).FullName} does not have a CmdIdAttribute.");
    }
}

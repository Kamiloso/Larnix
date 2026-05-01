#nullable enable
using System.Runtime.InteropServices;

namespace Larnix.Core.Serialization;

/// <summary>
/// Struct that can imitate bool for use in serialization
/// </summary>
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public readonly record struct boolsrl : ISanitizable<boolsrl>
{
    private readonly byte _value;
    private bool Value => _value != 0;

    public boolsrl(bool value)
    {
        _value = (byte)(value ? 1 : 0);
    }

    public boolsrl Sanitize()
    {
        return new boolsrl(Value);
    }

    public static implicit operator boolsrl(bool value) => new(value);
    public static implicit operator bool(boolsrl value) => value.Value;
}

#nullable enable
using System;
using System.Runtime.InteropServices;

namespace Larnix.Core.Binary;

/// <summary>
/// By applying this interface you promise that the struct that it is being
/// applied has a fully deterministic memory layout. That means it doesn't
/// contain any reference types, bools or other weird members and additionally
/// doesn't have any gaps in memory layout. It means: [StructLayout(LayoutKind.Sequential, Pack = 1)]
/// </summary>
public interface IFixedStruct<T> where T : unmanaged, IFixedStruct<T>
{
    T Sanitize() => (T)this;
}

public static unsafe class Serializer
{
    static Serializer()
    {
        if (!BitConverter.IsLittleEndian)
            throw new PlatformNotSupportedException("Only little-endian platforms are supported.");
    }

    public static ReadOnlySpan<byte> Serialize<T>(ref T obj) where T : unmanaged, IFixedStruct<T>
    {
        return MemoryMarshal.AsBytes(
            MemoryMarshal.CreateReadOnlySpan(ref obj, 1)
            );
    }

    public static T Deserialize<T>(ReadOnlySpan<byte> bytes) where T : unmanaged, IFixedStruct<T>
    {
        if (bytes.Length < sizeof(T))
            throw new ArgumentOutOfRangeException(nameof(bytes), "Span is too small.");

        return MemoryMarshal.Read<T>(bytes).Sanitize();
    }
}

#region Primitive Wrappers

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public readonly record struct Byte(byte Value) : IFixedStruct<Byte>
{
    public static implicit operator Byte(byte value) => new(value);
    public static implicit operator byte(Byte value) => value.Value;
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public readonly record struct SByte(sbyte Value) : IFixedStruct<SByte>
{
    public static implicit operator SByte(sbyte value) => new(value);
    public static implicit operator sbyte(SByte value) => value.Value;
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public readonly record struct Short(short Value) : IFixedStruct<Short>
{
    public static implicit operator Short(short value) => new(value);
    public static implicit operator short(Short value) => value.Value;
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public readonly record struct UShort(ushort Value) : IFixedStruct<UShort>
{
    public static implicit operator UShort(ushort value) => new(value);
    public static implicit operator ushort(UShort value) => value.Value;
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public readonly record struct Int(int Value) : IFixedStruct<Int>
{
    public static implicit operator Int(int value) => new(value);
    public static implicit operator int(Int value) => value.Value;
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public readonly record struct UInt(uint Value) : IFixedStruct<UInt>
{
    public static implicit operator UInt(uint value) => new(value);
    public static implicit operator uint(UInt value) => value.Value;
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public readonly record struct Long(long Value) : IFixedStruct<Long>
{
    public static implicit operator Long(long value) => new(value);
    public static implicit operator long(Long value) => value.Value;
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public readonly record struct ULong(ulong Value) : IFixedStruct<ULong>
{
    public static implicit operator ULong(ulong value) => new(value);
    public static implicit operator ulong(ULong value) => value.Value;
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public readonly record struct Float(float Value) : IFixedStruct<Float>
{
    public static implicit operator Float(float value) => new(value);
    public static implicit operator float(Float value) => value.Value;
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public readonly record struct Double(double Value) : IFixedStruct<Double>
{
    public static implicit operator Double(double value) => new(value);
    public static implicit operator double(Double value) => value.Value;
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public readonly record struct Bool(byte Value) : IFixedStruct<Bool>
{
    public Bool(bool value) : this(value ? (byte)1 : (byte)0) { }

    public static implicit operator Bool(bool value) => new(value);
    public static implicit operator bool(Bool value) => value.Value != 0;
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public readonly record struct Char(char Value) : IFixedStruct<Char>
{
    public static implicit operator Char(char value) => new(value);
    public static implicit operator char(Char value) => value.Value;
}

#endregion

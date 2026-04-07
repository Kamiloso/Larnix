#nullable enable
using Larnix.Core.Binary;
using System.Collections.Generic;
using System;
using System.Runtime.InteropServices;
using System.Text;

namespace Larnix.Model.Utils;

public interface IStringStruct : IEndianSafe
{
    public int BinarySize { get; }

    public static T[] Cut<T>(string str, Func<string, T> constructor) where T : IStringStruct, new()
    {
        int binSize = new T().BinarySize;
        int strSize = binSize / sizeof(char);

        List<T> parts = new();
        for (int i = 0; i < str.Length; i += strSize)
        {
            string part = str[i..Math.Min(i + strSize, str.Length)];
            parts.Add(constructor(part));
        }

        if (parts.Count == 0)
        {
            parts.Add(new T());
        }

        return parts.ToArray();
    }

    public static string Join<T>(T[] parts) where T : IStringStruct
    {
        StringBuilder sb = new();
        foreach (T part in parts)
        {
            sb.Append(part.ToString());
        }
        return sb.ToString();
    }

    protected static byte[] StringToFixedBinary(string str, int stringSize)
    {
        if (!BitConverter.IsLittleEndian)
            throw new PlatformNotSupportedException("Only little-endian platforms are supported.");

        int bytesSize = sizeof(char) * stringSize;
        byte[] bytes = new byte[bytesSize];
        Span<byte> target = new(bytes);

        ReadOnlySpan<byte> source = MemoryMarshal.AsBytes(str.AsSpan());
        if (source.Length > bytesSize)
        {
            source = source[..bytesSize];
        }

        source.CopyTo(target);
        return bytes;
    }

    protected static string FixedBinaryToString(ReadOnlySpan<byte> span)
    {
        if (!BitConverter.IsLittleEndian)
            throw new PlatformNotSupportedException("Only little-endian platforms are supported.");

        ReadOnlySpan<char> chars = MemoryMarshal.Cast<byte, char>(span);
        chars = chars.TrimEnd('\0');
        return new string(chars);
    }
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public unsafe struct String32 : IStringStruct, IFixedStruct<String32>
{
    private fixed byte buffer[BYTE_SIZE];
    public readonly int BinarySize => BYTE_SIZE;
    public const int BYTE_SIZE = 32;
    public const int STR_SIZE = BYTE_SIZE / 2;

    public String32(string value) => this = (String32)value;
    public override readonly string ToString() => (string)this;

    public static explicit operator String32(string value)
    {
        byte[] bytes = IStringStruct.StringToFixedBinary(value, STR_SIZE);
        String32 result = default;
        for (int i = 0; i < BYTE_SIZE; i++) result.buffer[i] = bytes[i];
        return result;
    }

    public static implicit operator string(String32 value)
        => IStringStruct.FixedBinaryToString(new ReadOnlySpan<byte>(value.buffer, BYTE_SIZE));
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public unsafe struct String64 : IStringStruct, IFixedStruct<String64>
{
    private fixed byte buffer[BYTE_SIZE];
    public readonly int BinarySize => BYTE_SIZE;
    public const int BYTE_SIZE = 64;
    public const int STR_SIZE = BYTE_SIZE / 2;

    public String64(string value) => this = (String64)value;
    public override readonly string ToString() => (string)this;

    public static explicit operator String64(string value)
    {
        byte[] bytes = IStringStruct.StringToFixedBinary(value, STR_SIZE);
        String64 result = default;
        for (int i = 0; i < BYTE_SIZE; i++) result.buffer[i] = bytes[i];
        return result;
    }

    public static implicit operator string(String64 value)
        => IStringStruct.FixedBinaryToString(new ReadOnlySpan<byte>(value.buffer, BYTE_SIZE));
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public unsafe struct String128 : IStringStruct, IFixedStruct<String128>
{
    private fixed byte buffer[BYTE_SIZE];
    public readonly int BinarySize => BYTE_SIZE;
    public const int BYTE_SIZE = 128;
    public const int STR_SIZE = BYTE_SIZE / 2;

    public String128(string value) => this = (String128)value;
    public override readonly string ToString() => (string)this;

    public static explicit operator String128(string value)
    {
        byte[] bytes = IStringStruct.StringToFixedBinary(value, STR_SIZE);
        String128 result = default;
        for (int i = 0; i < BYTE_SIZE; i++) result.buffer[i] = bytes[i];
        return result;
    }

    public static implicit operator string(String128 value)
        => IStringStruct.FixedBinaryToString(new ReadOnlySpan<byte>(value.buffer, BYTE_SIZE));
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public unsafe struct String256 : IStringStruct, IFixedStruct<String256>
{
    private fixed byte buffer[BYTE_SIZE];
    public readonly int BinarySize => BYTE_SIZE;
    public const int BYTE_SIZE = 256;
    public const int STR_SIZE = BYTE_SIZE / 2;

    public String256(string value) => this = (String256)value;
    public override readonly string ToString() => (string)this;

    public static explicit operator String256(string value)
    {
        byte[] bytes = IStringStruct.StringToFixedBinary(value, STR_SIZE);
        String256 result = default;
        for (int i = 0; i < BYTE_SIZE; i++) result.buffer[i] = bytes[i];
        return result;
    }

    public static implicit operator string(String256 value)
        => IStringStruct.FixedBinaryToString(new ReadOnlySpan<byte>(value.buffer, BYTE_SIZE));
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public unsafe struct String512 : IStringStruct, IFixedStruct<String512>
{
    private fixed byte buffer[BYTE_SIZE];
    public readonly int BinarySize => BYTE_SIZE;
    public const int BYTE_SIZE = 512;
    public const int STR_SIZE = BYTE_SIZE / 2;

    public String512(string value) => this = (String512)value;
    public override readonly string ToString() => (string)this;

    public static explicit operator String512(string value)
    {
        byte[] bytes = IStringStruct.StringToFixedBinary(value, STR_SIZE);
        String512 result = default;
        for (int i = 0; i < BYTE_SIZE; i++) result.buffer[i] = bytes[i];
        return result;
    }

    public static implicit operator string(String512 value)
        => IStringStruct.FixedBinaryToString(new ReadOnlySpan<byte>(value.buffer, BYTE_SIZE));
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public unsafe struct String1024 : IStringStruct, IFixedStruct<String1024>
{
    private fixed byte buffer[BYTE_SIZE];
    public readonly int BinarySize => BYTE_SIZE;
    public const int BYTE_SIZE = 1024;
    public const int STR_SIZE = BYTE_SIZE / 2;

    public String1024(string value) => this = (String1024)value;
    public override readonly string ToString() => (string)this;

    public static explicit operator String1024(string value)
    {
        byte[] bytes = IStringStruct.StringToFixedBinary(value, STR_SIZE);
        String1024 result = default;
        for (int i = 0; i < BYTE_SIZE; i++) result.buffer[i] = bytes[i];
        return result;
    }

    public static implicit operator string(String1024 value)
        => IStringStruct.FixedBinaryToString(new ReadOnlySpan<byte>(value.buffer, BYTE_SIZE));
}

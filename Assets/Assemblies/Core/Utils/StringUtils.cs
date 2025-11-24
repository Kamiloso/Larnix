using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using Larnix.Core.Serialization;

namespace Larnix.Core.Utils
{
    internal static class StringUtils
    {
        internal static byte[] StringToFixedBinary(string str, int stringSize)
        {
            int bytesSize = sizeof(char) * stringSize;
            byte[] bytes = new byte[bytesSize];

            Span<byte> target = new Span<byte>(bytes);
            Span<byte> source = Encoding.Unicode.GetBytes(str);

            if (source.Length > bytesSize)
                source = source.Slice(0, bytesSize);

            source.CopyTo(target);
            return bytes;
        }

        internal static string FixedBinaryToString(ReadOnlySpan<byte> span)
        {
            return Encoding.Unicode.GetString(span).TrimEnd('\0');
        }
    }

    #region StringN structs

    public interface IStringStruct : IIgnoresEndianness
    {
        public int BinarySize { get; }
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public unsafe struct String32 : IStringStruct
    {
        public int BinarySize => BYTE_SIZE;
        const int BYTE_SIZE = 32;
        const int STR_SIZE = BYTE_SIZE / 2;
        fixed byte buffer[BYTE_SIZE];

        public static implicit operator String32(string value)
        {
            byte[] bytes = StringUtils.StringToFixedBinary(value, STR_SIZE);
            String32 result = default;
            for (int i = 0; i < BYTE_SIZE; i++) result.buffer[i] = bytes[i];
            return result;
        }

        public static implicit operator string(String32 value)
            => StringUtils.FixedBinaryToString(new ReadOnlySpan<byte>(value.buffer, BYTE_SIZE));
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public unsafe struct String64 : IStringStruct
    {
        public int BinarySize => BYTE_SIZE;
        const int BYTE_SIZE = 64;
        const int STR_SIZE = BYTE_SIZE / 2;
        fixed byte buffer[BYTE_SIZE];

        public static implicit operator String64(string value)
        {
            byte[] bytes = StringUtils.StringToFixedBinary(value, STR_SIZE);
            String64 result = default;
            for (int i = 0; i < BYTE_SIZE; i++) result.buffer[i] = bytes[i];
            return result;
        }

        public static implicit operator string(String64 value)
            => StringUtils.FixedBinaryToString(new ReadOnlySpan<byte>(value.buffer, BYTE_SIZE));
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public unsafe struct String128 : IStringStruct
    {
        public int BinarySize => BYTE_SIZE;
        const int BYTE_SIZE = 128;
        const int STR_SIZE = BYTE_SIZE / 2;
        fixed byte buffer[BYTE_SIZE];

        public static implicit operator String128(string value)
        {
            byte[] bytes = StringUtils.StringToFixedBinary(value, STR_SIZE);
            String128 result = default;
            for (int i = 0; i < BYTE_SIZE; i++) result.buffer[i] = bytes[i];
            return result;
        }

        public static implicit operator string(String128 value)
            => StringUtils.FixedBinaryToString(new ReadOnlySpan<byte>(value.buffer, BYTE_SIZE));
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public unsafe struct String256 : IStringStruct
    {
        public int BinarySize => BYTE_SIZE;
        const int BYTE_SIZE = 256;
        const int STR_SIZE = BYTE_SIZE / 2;
        fixed byte buffer[BYTE_SIZE];

        public static implicit operator String256(string value)
        {
            byte[] bytes = StringUtils.StringToFixedBinary(value, STR_SIZE);
            String256 result = default;
            for (int i = 0; i < BYTE_SIZE; i++) result.buffer[i] = bytes[i];
            return result;
        }

        public static implicit operator string(String256 value)
            => StringUtils.FixedBinaryToString(new ReadOnlySpan<byte>(value.buffer, BYTE_SIZE));
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public unsafe struct String512 : IStringStruct
    {
        public int BinarySize => BYTE_SIZE;
        const int BYTE_SIZE = 512;
        const int STR_SIZE = BYTE_SIZE / 2;
        fixed byte buffer[BYTE_SIZE];

        public static implicit operator String512(string value)
        {
            byte[] bytes = StringUtils.StringToFixedBinary(value, STR_SIZE);
            String512 result = default;
            for (int i = 0; i < BYTE_SIZE; i++) result.buffer[i] = bytes[i];
            return result;
        }

        public static implicit operator string(String512 value)
            => StringUtils.FixedBinaryToString(new ReadOnlySpan<byte>(value.buffer, BYTE_SIZE));
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public unsafe struct String1024 : IStringStruct
    {
        public int BinarySize => BYTE_SIZE;
        const int BYTE_SIZE = 1024;
        const int STR_SIZE = BYTE_SIZE / 2;
        fixed byte buffer[BYTE_SIZE];

        public static implicit operator String1024(string value)
        {
            byte[] bytes = StringUtils.StringToFixedBinary(value, STR_SIZE);
            String1024 result = default;
            for (int i = 0; i < BYTE_SIZE; i++) result.buffer[i] = bytes[i];
            return result;
        }

        public static implicit operator string(String1024 value)
            => StringUtils.FixedBinaryToString(new ReadOnlySpan<byte>(value.buffer, BYTE_SIZE));
    }

    #endregion
}

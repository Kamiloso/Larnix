using System;
using System.Runtime.InteropServices;
using System.Text;
using Larnix.Core.Binary;

namespace Larnix.GameCore.Utils
{
    public interface IStringStruct : IEndianSafe
    {
        public int BinarySize { get; }

        public static T[] Cut<T>(string str, Func<string, T> constructor) where T : IStringStruct, new()
        {
            str = str.TrimEnd('\0');

            int binSize = new T().BinarySize;
            int strSize = binSize / 2;
            
            int count = str.Length / strSize;
            if (str.Length % strSize != 0 || count == 0)
            {
                count++;
            }

            T[] result = new T[count];
            for (int i = 0; i < count; i++)
            {
                int maxRem = str.Length - i * strSize;
                string substr = str.Substring(i * strSize, Math.Min(maxRem, strSize));
                result[i] = (T)(IStringStruct)constructor(substr);
            }
            return result;
        }

        protected static byte[] StringToFixedBinary(string str, int stringSize)
        {
            int bytesSize = sizeof(char) * stringSize;
            byte[] bytes = new byte[bytesSize];

            Span<byte> target = new Span<byte>(bytes);
            Span<byte> source = Encoding.Unicode.GetBytes(str);

            if (source.Length > bytesSize)
                source = source[..bytesSize];

            source.CopyTo(target);
            return bytes;
        }

        protected static string FixedBinaryToString(ReadOnlySpan<byte> span)
        {
            return Encoding.Unicode.GetString(span).TrimEnd('\0');
        }
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public unsafe struct String32 : IStringStruct
    {
        public int BinarySize => BYTE_SIZE;
        public const int BYTE_SIZE = 32;
        public const int STR_SIZE = BYTE_SIZE / 2;
        fixed byte buffer[BYTE_SIZE];

        public String32(string value) => this = (String32)value;
        public override string ToString() => (string)this;

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
    public unsafe struct String64 : IStringStruct
    {
        public int BinarySize => BYTE_SIZE;
        public const int BYTE_SIZE = 64;
        public const int STR_SIZE = BYTE_SIZE / 2;
        fixed byte buffer[BYTE_SIZE];

        public String64(string value) => this = (String64)value;
        public override string ToString() => (string)this;

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
    public unsafe struct String128 : IStringStruct
    {
        public int BinarySize => BYTE_SIZE;
        public const int BYTE_SIZE = 128;
        public const int STR_SIZE = BYTE_SIZE / 2;
        fixed byte buffer[BYTE_SIZE];

        public String128(string value) => this = (String128)value;
        public override string ToString() => (string)this;

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
    public unsafe struct String256 : IStringStruct
    {
        public int BinarySize => BYTE_SIZE;
        public const int BYTE_SIZE = 256;
        public const int STR_SIZE = BYTE_SIZE / 2;
        fixed byte buffer[BYTE_SIZE];

        public String256(string value) => this = (String256)value;
        public override string ToString() => (string)this;

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
    public unsafe struct String512 : IStringStruct
    {
        public int BinarySize => BYTE_SIZE;
        public const int BYTE_SIZE = 512;
        public const int STR_SIZE = BYTE_SIZE / 2;
        fixed byte buffer[BYTE_SIZE];

        public String512(string value) => this = (String512)value;
        public override string ToString() => (string)this;

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
    public unsafe struct String1024 : IStringStruct
    {
        public int BinarySize => BYTE_SIZE;
        public const int BYTE_SIZE = 1024;
        public const int STR_SIZE = BYTE_SIZE / 2;
        fixed byte buffer[BYTE_SIZE];

        public String1024(string value) => this = (String1024)value;
        public override string ToString() => (string)this;

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
}

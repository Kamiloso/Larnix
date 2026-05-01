#nullable enable
using Larnix.Core;
using Larnix.Core.Serialization;
using System;
using System.Runtime.InteropServices;

namespace Larnix.Socket.Payload;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
internal readonly struct PayloadStruct<T> : ISanitizable<PayloadStruct<T>> where T : unmanaged
{
    public readonly short CmdId;
    public readonly T Contents;

    public static short ExpectedCmdId => Cmd.Id<T>();

    static PayloadStruct()
    {
        const int WARNING_SIZE = 1400;
        const int MAX_SIZE = 65_000;

        int size = Binary<PayloadStruct<T>>.Size;

        if (size > MAX_SIZE)
        {
            throw new InvalidOperationException(
                $"PayloadStruct<{typeof(T).Name}> size {size} exceeds maximum allowed size {MAX_SIZE}.");
        }
        else if (size > WARNING_SIZE)
        {
            Echo.LogWarning(
                $"PayloadStruct<{typeof(T).Name}> size {size} exceeds warning threshold {WARNING_SIZE}.");
        }
    }

    public PayloadStruct(in T contents)
    {
        CmdId = Sanitizer.Filter(ExpectedCmdId);
        Contents = Sanitizer.Filter(contents);
    }

    public PayloadStruct<T> Sanitize()
    {
        return new PayloadStruct<T>(Contents);
    }

    public override string ToString()
    {
        string contents = BitConverter.ToString(Binary<T>.Serialize(Contents));
        return $"PayloadStruct<{typeof(T).Name}> payload=({contents})";
    }
}

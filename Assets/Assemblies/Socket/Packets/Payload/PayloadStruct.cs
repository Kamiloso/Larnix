#nullable enable
using Larnix.Core;
using Larnix.Core.Serialization;
using System;
using System.Runtime.InteropServices;

namespace Larnix.Socket.Packets.Payload;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
internal readonly record struct PayloadStruct<T> where T : unmanaged
{
    public readonly short CmdId;
    public readonly T Contents;

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
        CmdId = Cmd.Value<T>();
        Contents = contents;
    }

    public override string ToString()
    {
        string contents = BitConverter.ToString(Binary<T>.Serialize(Contents));
        return $"PayloadStruct<{typeof(T).Name}> payload=({contents})";
    }
}

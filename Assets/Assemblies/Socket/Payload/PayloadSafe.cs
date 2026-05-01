#nullable enable
using Larnix.Core.Serialization;
using System.Runtime.InteropServices;

namespace Larnix.Socket.Payload;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
internal readonly struct PayloadSafe<T> : ISanitizable<PayloadSafe<T>> where T : unmanaged
{
    public readonly PayloadHeader Header;
    public readonly PayloadStruct<T> Payload;

    public PayloadSafe(in PayloadHeader header, in PayloadStruct<T> payload)
    {
        Header = Sanitizer.Filter(header);
        Payload = Sanitizer.Filter(payload);
    }

    public PayloadSafe<T> Sanitize()
    {
        return new PayloadSafe<T>(Header, Payload);
    }
}

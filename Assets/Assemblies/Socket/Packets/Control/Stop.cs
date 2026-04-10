#nullable enable

namespace Larnix.Socket.Packets.Control;

public sealed class Stop : Payload_Legacy
{
    private const int SIZE = 0;

    public Stop(byte code = 0)
    {
        InitializePayload(new byte[0], code);
    }

    protected override bool IsValid()
    {
        return Bytes.Length == SIZE;
    }
}

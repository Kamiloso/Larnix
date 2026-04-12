#nullable enable
using System.Net;

namespace Larnix.Socket.Networking;

internal record DataBox(IPEndPoint Target, byte[] Data);

internal interface INetworkInteractions
{
    void Send(DataBox payload);
    bool TryReceive(out DataBox result);
}

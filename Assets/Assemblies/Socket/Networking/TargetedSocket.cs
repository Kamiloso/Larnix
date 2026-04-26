#nullable enable
using System.Net;

namespace Larnix.Socket.Networking;

internal interface ITargetedSocket
{
    IPEndPoint Target { get; }
    public void Send(byte[] data);
    public bool TryReceive(out byte[] result);
}

internal class TargetedSocket : ITargetedSocket
{
    public ISocket Socket { get; }
    public IPEndPoint Target { get; }

    public TargetedSocket(ISocket socket, IPEndPoint target)
    {
        Socket = socket;
        Target = target;
    }

    public void Send(byte[] data)
    {
        Socket.Send(new DataBox(Target, data));
    }

    public bool TryReceive(out byte[] result)
    {
        if (Socket.TryReceive(out var payload) && payload.Target.Equals(Target))
        {
            result = payload.Data;
            return true;
        }

        result = null!;
        return false;
    }
}

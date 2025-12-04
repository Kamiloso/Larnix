using Larnix.Socket.Structs;
using System.Net;

namespace Larnix.Socket.Channel
{
    internal interface INetworkInteractions
    {
        void Send(IPEndPoint remoteEP, byte[] data);
        bool TryReceive(out DataBox result);

        void Send(DataBox box) => Send(box.target, box.data);
    }
}

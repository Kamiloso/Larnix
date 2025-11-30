using System.Collections;
using System.Collections.Generic;
using System.Net;

namespace Larnix.Socket.Channel
{
    internal class SendBox
    {
        public readonly IPEndPoint target;
        public readonly byte[] data;

        public SendBox(IPEndPoint target, byte[] data)
        {
            this.target = target;
            this.data = data;
        }
    }
}

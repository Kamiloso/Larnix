using System;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using Larnix.Socket.Security.Keys;
using Larnix.Socket.Channel;
using Larnix.Socket.Structs;
using Larnix.Core.Utils;
using Larnix.Socket.Packets;

namespace Larnix.Socket.Frontend
{
    internal class Prompter : IDisposable
    {
        private readonly UdpClient2 _udpClient;
        private readonly IPEndPoint _target;

        private bool _disposed;

        private Prompter(IPEndPoint endPoint, UdpClient2 udpClient)
        {
            _target = endPoint;
            _udpClient = udpClient;
        }

        public static async Task<TAnswer> PromptAsync<TAnswer>(string address, Payload prompt, int timeoutMiliseconds = 1500, KeyRSA publicKey = null) where TAnswer : Payload, new()
        {
            IPEndPoint target = await Resolver.ResolveStringAsync(address);
            if (target == null) return null;

            using UdpClient2 udp = new UdpClient2(
                port: 0,
                isListener: false,
                isLoopback: IPAddress.IsLoopback(target.Address),
                isIPv6: target.AddressFamily == AddressFamily.InterNetworkV6,
                recvBufferSize: 16 * 1024,
                destination: target
                );
            var prompter = new Prompter(target, udp);

            return await prompter.SendAndWaitAsync<TAnswer>(prompt, publicKey, timeoutMiliseconds);
        }

        private async Task<TAnswer> SendAndWaitAsync<TAnswer>(Payload prompt, KeyRSA publicKey, int timeoutMiliseconds) where TAnswer : Payload, new()
        {
            int promptID = (int)Common.GetSecureLong();

            PayloadBox payloadBox = new PayloadBox(
                seqNum: promptID,
                ackNum: 0,
                flags: (byte)(PacketFlag.NCN | (publicKey != null ? PacketFlag.RSA : 0)),
                payload: prompt
                );

            byte[] data = publicKey != null ?
                payloadBox.Serialize(publicKey) :
                payloadBox.Serialize(KeyEmpty.GetInstance());

            try
            {
                _udpClient.Send(_target, data);
            }
            catch
            {
                Core.Debug.LogWarning($"Cannot send prompt! CmdID = {prompt.ID}");
                return null; // sending error
            }

            long deadline = Timestamp.GetTimestamp() + timeoutMiliseconds;

            while (Timestamp.GetTimestamp() < deadline)
            {
                while (_udpClient.TryReceive(out var item))
                {
                    byte[] networkBytes = item.data;

                    if (PayloadBox.TryDeserialize(networkBytes, KeyEmpty.GetInstance(), out var incoming) && incoming.SeqNum == promptID)
                    {
                        if (Payload.TryConstructPayload<TAnswer>(incoming.Bytes, out var answer))
                            return answer;
                    }
                }

                await Task.Delay(100);
            }

            return null; // timeout
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _disposed = true;

                _udpClient?.Dispose();
            }
        }
    }
}

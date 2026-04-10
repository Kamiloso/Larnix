using System;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using Larnix.Socket.Security.Keys;
using Larnix.Socket.Helpers.Networking;
using Larnix.Socket.Packets;
using Larnix.Core;
using Larnix.Core.Utils;
using Larnix.Socket.Packets.Payload;

namespace Larnix.Socket.Frontend;

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

    public static async Task<TAnswer> PromptAsync<TAnswer>(string address, Payload_Legacy prompt, int timeoutMiliseconds = 3000, KeyRSA publicKey = null) where TAnswer : Payload_Legacy
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

    private async Task<TAnswer> SendAndWaitAsync<TAnswer>(Payload_Legacy prompt, KeyRSA publicKey, int timeoutMiliseconds) where TAnswer : Payload_Legacy
    {
        int promptID = RandUtils.SecureInt();

        PayloadBox_Legacy payloadBox = new(
            seqNum: promptID,
            ackNum: 0,
            flags: (byte)(PacketFlag.NCN | (publicKey != null ? PacketFlag.RSA : 0)),
            payload: prompt
            );

        byte[] data = publicKey != null ?
            payloadBox.Serialize(publicKey) :
            payloadBox.Serialize(KeyEmpty.Instance);

        try
        {
            _udpClient.Send(_target, data);
        }
        catch
        {
            Echo.LogWarning($"Cannot send prompt! CmdID = {prompt.ID}");
            return null; // sending error
        }

        long deadline = Timestamp.GetTimestamp() + timeoutMiliseconds;

        while (Timestamp.GetTimestamp() < deadline)
        {
            while (_udpClient.TryReceive(out var item))
            {
                byte[] networkBytes = item.data;

                if (PayloadBox_Legacy.TryDeserialize(networkBytes, KeyEmpty.Instance, out var incoming) && incoming.SeqNum == promptID)
                {
                    if (Payload_Legacy.TryConstructPayload<TAnswer>(incoming.Bytes, out var answer))
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

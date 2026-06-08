#nullable enable
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using Larnix.Socket.Networking;
using Larnix.Core.Utils;
using Larnix.Socket.Payload;
using Larnix.Socket.Payload.Structs;
using Larnix.Socket.Session.Tools;
using Larnix.Socket.Security.Encryption;
using Larnix.Core;

namespace Larnix.Socket.Client.Services;

internal static class Prompter
{
    public static async Task<TAnswer?> PromptAsync<TPrompt, TAnswer>(
        string address, TPrompt prompt, RsaPublicKey? key, int timeout = 3000) where TPrompt : unmanaged where TAnswer : unmanaged
    {
        IPEndPoint? target = await DnsResolver.ResolveAsync(address, SocketInfo.DefaultPort);
        if (target == null) return null;

        using UdpClient2 udp = new(
            port: 0,
            isListener: false,
            isLoopback: IPAddress.IsLoopback(target.Address),
            isIPv6: target.AddressFamily == AddressFamily.InterNetworkV6,
            recvBufferSize: 16 * 1024,
            destination: target
            );

        Seq seq = new(RandUtils.SecureInt());

        byte flags = 0;
        flags |= (byte)PacketFlag.NCN;
        flags |= (byte)(key != null ? PacketFlag.RSA : 0);

        PayloadHeader header = new(seq, flags);
        byte[] data = NetworkSerializer.ToBytes(header, prompt, key);

        udp.Send(new DataBox(target, data));

        long deadline = Timestamp.Now() + timeout;
        while (Timestamp.Now() < deadline)
        {
            while (udp.TryReceive(out DataBox item))
            {
                byte[] networkBytes = item.Data;

                if (!NetworkSerializer.TryNetworkBytesAs(networkBytes, null, out PayloadHeader inHeader, out TAnswer answer)) continue;
                if (inHeader.SeqNum != seq) continue;

                return answer;
            }

            await Task.Delay(100);
        }

        return null;
    }
}

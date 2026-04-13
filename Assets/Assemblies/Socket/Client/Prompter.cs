#nullable enable
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using Larnix.Socket.Security.Keys;
using Larnix.Socket.Networking;
using Larnix.Core;
using Larnix.Core.Utils;
using Larnix.Model;
using Larnix.Socket.Payload;

namespace Larnix.Socket.Client;

internal static class Prompter
{
    public static async Task<TAnswer?> PromptAsync<TPrompt, TAnswer>(
        string address, TPrompt prompt, KeyRSA? key, int timeout = 3000) where TPrompt : unmanaged where TAnswer : unmanaged
    {
        IPEndPoint? target = await DnsResolver.ResolveAsync(address, GameInfo.DefaultPort);
        if (target == null) return null;

        using UdpClient2 udp = new(
            port: 0,
            isListener: false,
            isLoopback: IPAddress.IsLoopback(target.Address),
            isIPv6: target.AddressFamily == AddressFamily.InterNetworkV6,
            recvBufferSize: 16 * 1024,
            destination: target
            );

        int id = RandUtils.SecureInt();

        byte flags = 0;
        flags |= (byte)PacketFlag.NCN;
        flags |= (byte)(key != null ? PacketFlag.RSA : 0);

        PayloadHeader header = new(id, 0, flags);
        byte[] data = NetworkSerializer.ToBytes(header, prompt, key);

        udp.Send(new DataBox(target, data));

        long deadline = Timestamp.Now() + timeout;
        while (Timestamp.Now() < deadline)
        {
            while (udp.TryReceive(out DataBox item))
            {
                byte[] networkBytes = item.Data;

                if (!NetworkSerializer.TryNetworkBytesAs(networkBytes, KeyEmpty.Instance, out PayloadHeader inHeader, out TAnswer answer)) continue;
                if (inHeader.SeqNum != id) continue;

                return answer;
            }

            await Task.Delay(100);
        }

        return null;
    }
}

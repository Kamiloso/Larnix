using QuickNet.Channel;
using QuickNet.Processing;
using System;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Threading.Tasks;

namespace QuickNet.Frontend
{
    internal sealed class Prompter : IDisposable
    {
        private readonly UdpClient udpClient;
        private readonly IPEndPoint endPoint;
        private bool _disposed;

        private Prompter(IPEndPoint endPoint, UdpClient udpClient)
        {
            this.endPoint = endPoint;
            this.udpClient = udpClient;
        }

        public static async Task<TAnswer> PromptAsync<TAnswer>(string ipAddress, Packet prompt, int timeoutMiliseconds = 3000, RSA publicKeyRSA = null) where TAnswer : Payload, new()
        {
            var endPoint = await Resolver.ResolveStringAsync(ipAddress);
            if (endPoint == null) return null;

            using var udp = QuickClient.CreateConfiguredClientObject(endPoint);
            var prompter = new Prompter(endPoint, udp);

            return await prompter.SendAndWaitAsync<TAnswer>(prompt, publicKeyRSA, timeoutMiliseconds);
        }

        private async Task<TAnswer> SendAndWaitAsync<TAnswer>(Packet prompt, RSA publicKeyRSA, int timeoutMiliseconds) where TAnswer : Payload, new()
        {
            int promptId = (int)KeyObtainer.GetSecureLong();

            PacketFlag flags = PacketFlag.NCN;
            Func<byte[], byte[]> encrypt = null;
            if (publicKeyRSA != null)
            {
                flags |= PacketFlag.RSA;
                encrypt = bytes => Encryption.EncryptRSA(bytes, publicKeyRSA);
            }

            var safePacket = new QuickPacket(promptId, 0, (byte)flags, prompt)
            {
                Encryption = encrypt
            };

            byte[] data = safePacket.Serialize();

            try
            {
                await udpClient.SendAsync(data, data.Length, endPoint);
            }
            catch
            {
                // ERROR
                return null;
            }

            long deadline = Timestamp.GetTimestamp() + timeoutMiliseconds;

            while (Timestamp.GetTimestamp() < deadline)
            {
                long delay = deadline - Timestamp.GetTimestamp();
                if (delay <= 0) break;

                var receiveTask = udpClient.ReceiveAsync();
                var timeoutTask = Task.Delay((int)delay);
                var completed = await Task.WhenAny(receiveTask, timeoutTask);

                if (completed == timeoutTask)
                {
                    break; // timeout
                }

                try
                {
                    var result = receiveTask.Result;
                    if (!endPoint.Equals(result.RemoteEndPoint)) continue;

                    var incoming = new QuickPacket();
                    if (incoming.TryDeserialize(result.Buffer))
                    {
                        if (incoming.SeqNum == promptId && incoming.HasFlag(PacketFlag.NCN))
                        {
                            var packet = incoming.Packet;
                            if (Payload.TryConstructPayload<TAnswer>(packet, out var answer))
                            {
                                return answer;
                            }
                        }
                    }
                }
                catch
                {
                    break;
                }
            }

            // ERROR
            return null;
        }

        public void Dispose()
        {
            if (_disposed) return;
            udpClient?.Dispose();
            _disposed = true;
        }
    }
}

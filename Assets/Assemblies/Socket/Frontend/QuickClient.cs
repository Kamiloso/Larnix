using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System;
using Larnix.Socket.Packets;
using System.Threading.Tasks;
using Larnix.Socket.Security;
using Larnix.Socket.Channel;
using Larnix.Socket.Security.Keys;
using Larnix.Socket.Packets.Control;
using Larnix.Socket.Channel.Networking;

namespace Larnix.Socket.Frontend
{
    public class QuickClient : IDisposable
    {
        private readonly UdpClient2 _udpClient;
        private readonly Connection _connection;
        private readonly IPEndPoint _target;
        private readonly KeyRSA _rsaKey;

        private readonly Dictionary<CmdID, Action<HeaderSpan>> Subscriptions = new();

        public static async Task<QuickClient> CreateClientAsync(string address, string authcode, string nickname, string password)
        {
            IPEndPoint endPoint = await Resolver.ResolveStringAsync(address);
            if (endPoint == null) return null;

            EntryTicket ticket = (await Resolver.GetEntryTicketAsync(address, authcode, nickname)).ticket;
            if (ticket == null) return null;

            Cacher.RemoveRecord(authcode, nickname);

            try
            {
                return new QuickClient(endPoint, ticket, address, authcode, nickname, password);
            }
            catch (Exception ex)
            {
                Core.Debug.LogError(ex.Message);
                return null;
            }
        }

        private QuickClient(IPEndPoint target, EntryTicket ticket, string address, string authcode, string nickname, string password)
        {
            _target = target;
            _udpClient = new UdpClient2(
                port: 0,
                isListener: false,
                isLoopback: IPAddress.IsLoopback(target.Address),
                isIPv6: target.AddressFamily == AddressFamily.InterNetworkV6,
                recvBufferSize: 256 * 1024,
                destination: target
                );

            _rsaKey = ticket.CreatePublicKey();

            long serverSecret = Authcode.GetSecretFromAuthCode(authcode);
            long challengeID = ticket.ChallengeID;
            long runID = ticket.RunID;
            long timestamp = Timestamp.GetServerTimestamp(address);

            KeyAES keyAES = new(); // auto-generate
            byte[] aesBytes = keyAES.ExportKey();

            Payload synPacket = new AllowConnection(nickname, password, aesBytes, serverSecret, challengeID, timestamp, runID);
            _connection = Connection.CreateClient(_udpClient, _target, keyAES, _rsaKey, synPacket);
        }

        public void ClientTick(float deltaTime)
        {
            // get packets from UDP client
            while (_udpClient.TryReceive(out var pair))
                _connection.PushFromWeb(pair.data);

            // process received packets
            _connection.Tick(deltaTime);
            Queue<HeaderSpan> received = _connection.Receive();

            while (received.Count > 0)
            {
                HeaderSpan headerSpan = received.Dequeue();
                if (Subscriptions.TryGetValue(headerSpan.ID, out var Execute))
                {
                    Execute(headerSpan);
                }
            }
        }

        public void Subscribe<T>(Action<T> InterpretPacket) where T : Payload, new()
        {
            CmdID ID = Payload.CmdID<T>();
            Subscriptions[ID] = (HeaderSpan packetBytes) =>
            {
                if (Payload.TryConstructPayload<T>(packetBytes, out var message))
                {
                    InterpretPacket(message);
                }
            };
        }

        public void Send(Payload packet, bool safemode = true) => _connection.Send(packet, safemode);
        public void FinishConnection() => _connection.Dispose();
        public bool IsDead() => _connection.IsDead;
        public float GetPing() => _connection.AvgRTT * 1000f; // ms

        public void Dispose()
        {
            FinishConnection();
            _udpClient?.Dispose();
            _rsaKey?.Dispose();
        }
    }
}

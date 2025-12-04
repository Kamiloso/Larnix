using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System;
using Larnix.Socket.Packets;
using System.Threading.Tasks;
using Larnix.Socket.Security;
using Larnix.Socket.Channel;
using Larnix.Socket.Structs;

namespace Larnix.Socket.Frontend
{
    public class QuickClient : IDisposable
    {
        private readonly UdpClient2 UdpClient;
        private readonly Connection Connection;
        private readonly IPEndPoint EndPoint;

        private readonly Dictionary<CmdID, Action<Packet>> Subscriptions = new();

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

        private QuickClient(IPEndPoint endPoint, EntryTicket ticket, string address, string authcode, string nickname, string password)
        {
            EndPoint = endPoint;
            UdpClient = new UdpClient2(
                port: 0,
                isListener: false,
                isLoopback: IPAddress.IsLoopback(endPoint.Address),
                isIPv6: endPoint.AddressFamily == AddressFamily.InterNetworkV6,
                recvBufferSize: 256 * 1024,
                destination: endPoint
                );
            RSA KeyRSA = KeyObtainer.PublicBytesToKey(ticket.PublicKeyRSA);

            long serverSecret = Authcode.GetSecretFromAuthCode(authcode);
            long challengeID = ticket.ChallengeID;
            long runID = ticket.RunID;

            byte[] keyAES = new byte[16];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(keyAES);
            }

            long timestamp = Timestamp.GetServerTimestamp(address);

            Packet synPacket = new AllowConnection(nickname, password, keyAES, serverSecret, challengeID, timestamp, runID);
            Connection = new Connection(bytes => UdpClient.Send(EndPoint, bytes), EndPoint, keyAES, synPacket, KeyRSA);
        }

        public void ClientTick(float deltaTime)
        {
            // get packets from UDP client
            while (UdpClient.TryReceive(out var pair))
                Connection.PushFromWeb(pair.data);

            // process received packets
            Connection.Tick(deltaTime);
            Queue<Packet> received = Connection.Receive();

            while (received.Count > 0)
            {
                Packet packet = received.Dequeue();
                if (packet != null && Subscriptions.TryGetValue(packet.ID, out var Execute))
                {
                    Execute(packet);
                }
            }
        }

        public void Subscribe<T>(Action<T> InterpretPacket) where T : Payload, new()
        {
            CmdID ID = Payload.CmdID<T>();
            Subscriptions[ID] = (Packet packet) =>
            {
                if (Payload.TryConstructPayload<T>(packet, out var message))
                {
                    InterpretPacket(message);
                }
            };
        }

        public void Send(Packet packet, bool safemode = true) => Connection.Send(packet, safemode);
        public void FinishConnection() => Connection.FinishConnection();
        public bool IsDead() => Connection.IsDead;
        public float GetPing() => Connection.AvgRTT * 1000f; // ms

        public void Dispose()
        {
            FinishConnection();
            UdpClient?.Dispose();
        }
    }
}

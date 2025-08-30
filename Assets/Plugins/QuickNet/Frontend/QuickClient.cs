using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System;
using QuickNet.Commands;
using QuickNet.Processing;
using QuickNet.Channel;
using System.Threading.Tasks;

namespace QuickNet.Frontend
{
    public class QuickClient : IDisposable
    {
        private readonly UdpClient UdpClient;
        private readonly Connection Connection;
        private readonly IPEndPoint EndPoint;

        private readonly Dictionary<CmdID, Action<Packet>> Subscriptions = new();

        public static Task<QuickClient> CreateClientAsync(string address, string authcode, string nickname, string password)
        {
            return Task.Run(() =>
            {
                IPEndPoint endPoint = Resolver.ResolveStringSync(address);
                if (endPoint == null) return null;

                EntryTicket ticket = Resolver.GetEntryTicketAsync(address, authcode, nickname).Result;
                if (ticket == null) return null;

                try
                {
                    return new QuickClient(endPoint, ticket, authcode, nickname, password);
                }
                catch
                {
                    return null;
                }
            });
        }

        private QuickClient(IPEndPoint endPoint, EntryTicket ticket, string authcode, string nickname, string password)
        {
            EndPoint = endPoint;
            UdpClient = CreateConfiguredClientObject(EndPoint);
            RSA KeyRSA = KeyObtainer.PublicBytesToKey(ticket.PublicKeyRSA);

            long serverSecret = Authcode.GetSecretFromAuthCode(authcode);
            long challengeID = ticket.ChallengeID;

            byte[] keyAES = new byte[16];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(keyAES);
            }

            long timestamp = IPAddress.IsLoopback(EndPoint.Address) ?
                Timestamp.GetTimestamp() :
                Timestamp.GetServerTimestamp(EndPoint);

            AllowConnection allowConnection = new AllowConnection(nickname, password, keyAES, serverSecret, challengeID, timestamp);
            if (allowConnection.HasProblems)
                throw new Exception("Couldn't construct AllowConnection command.");

            Connection = new Connection(UdpClient, EndPoint, keyAES, allowConnection.GetPacket(), KeyRSA);
        }

        public static UdpClient CreateConfiguredClientObject(IPEndPoint endPoint)
        {
            IPAddress address = endPoint.Address;
            AddressFamily family = endPoint.AddressFamily;

            UdpClient udpClient = new UdpClient(family);

            if (family == AddressFamily.InterNetwork)
            {
                udpClient.Client.Bind(new IPEndPoint(
                    IPAddress.IsLoopback(address) ? IPAddress.Loopback : IPAddress.Any, 0)
                    );
            }
            else if (family == AddressFamily.InterNetworkV6)
            {
                udpClient.Client.Bind(new IPEndPoint(
                    IPAddress.IsLoopback(address) ? IPAddress.IPv6Loopback : IPAddress.IPv6Any, 0)
                    );
            }
            else throw new System.NotSupportedException("Unknown address type.");

            udpClient.Client.Blocking = false;
            udpClient.Client.ReceiveBufferSize = 1024 * 1024; // 1 MB

            return udpClient;
        }

        public void ClientTick(float deltaTime)
        {
            // Get packets from UDP client

            while (UdpClient.Available > 0)
            {
                IPEndPoint remoteEP = new IPEndPoint(IPAddress.Any, 0);
                byte[] bytes = null;

                try
                {
                    bytes = UdpClient.Receive(ref remoteEP);
                }
                catch (SocketException ex)
                {
                    if (ex.SocketErrorCode == SocketError.WouldBlock || ex.SocketErrorCode == SocketError.ConnectionReset)
                        continue;
                    else
                        throw;
                }

                if (bytes != null)
                {
                    if (EndPoint.Equals(remoteEP))
                        Connection.PushFromWeb(bytes);
                }
            }

            // Process received packets

            Connection.Tick(deltaTime);
            Queue<Packet> received = Connection.Receive();

            while (received.Count > 0)
            {
                Packet packet = received.Dequeue();
                if (Subscriptions.TryGetValue(packet.ID, out var Execute))
                {
                    Execute(packet);
                }
            }
        }

        public void Subscribe<T>(Action<T> InterpretPacket) where T : BaseCommand
        {
            CmdID ID = BaseCommand.GetCommandID(typeof(T));
            Subscriptions[ID] = (Packet packet) =>
            {
                T command = BaseCommand.CreateGeneric<T>(packet);
                if (command != null && !command.HasProblems)
                {
                    InterpretPacket(command);
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

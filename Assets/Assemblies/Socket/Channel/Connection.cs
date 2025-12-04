using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Net;
using System.Security.Cryptography;
using Larnix.Socket.Packets;
using System;
using Larnix.Core.Utils;
using Larnix.Socket.Security;
using Larnix.Socket.Structs;

namespace Larnix.Socket.Channel
{
    public class Connection
    {
        private Action<byte[]> SendToEndPoint;
        public readonly IPEndPoint EndPoint;

        private byte[] KeyAES = null;
        private RSA KeyRSA = null;

        private const int MaxSeqTolerance = 128;
        private int SeqNum = 0; // last sent message ID
        private int AckNum = 0; // last acknowledged message ID
        private int GetNum = 0; // last received message ID

        public const uint MaxTransmissions = 8;
        public const uint MaxStayingPackets = 128;
        public bool IsDead { get; private set; } = false; // blocks socket
        public bool IsError { get; private set; } = false; // communiactes that an error occured

        private const float AckCycleTime = 0.1f;
        private float currentAckCycleTime = 0.0f;

        private const float SafeCycleTime = 0.5f;
        private float currentSafeCycleTime = 0.0f;

        private System.Random Rand = new();
        private const float DebugDropChance = 0.0f;

        private List<QuickPacket> sendingPackets = new List<QuickPacket>();
        private List<QuickPacket> receivedPackets = new List<QuickPacket>();
        private Queue<Packet> downloadablePackets = new Queue<Packet>();

        private readonly LinkedList<(int seq, long time)> PacketTimestamps = new();
        private readonly LinkedList<long> PacketRTTs = new();
        private readonly HashSet<int> _RetransmittedNow = new(); // local
        private const int MaxRTTs = 10;
        private const float OffsetRTT = 0.050f; // 50 ms
        private const float DefaultAvgRTT = 0.6f; // assumes 600 ms for safety

        public float AvgRTT { get; private set; } = DefaultAvgRTT;

        public readonly bool IsClient;

        public Connection(Action<byte[]> sendToEndPoint, IPEndPoint endPoint, byte[] keyAES, Packet synPacket = null, RSA keyPublicRSA = null)
        {
            SendToEndPoint = sendToEndPoint;
            EndPoint = endPoint;
            KeyRSA = keyPublicRSA;
            KeyAES = keyAES?.Length == 16 ? keyAES : throw new ArgumentException("Wrong keyAES format!");

            IsClient = synPacket != null;

            if (synPacket != null) // CLIENT, use RSA public key
            {
                // Send SYN packet
                PacketFlag flags = PacketFlag.SYN;
                if(keyPublicRSA != null)
                    flags |= PacketFlag.RSA; // guarantees encryption

                QuickPacket safePacket = new QuickPacket(
                    ++SeqNum,
                    GetNum,
                    (byte)flags,
                    synPacket
                    );

                SendSafePacket(safePacket);
            }
        }

        public void Tick(float deltaTime)
        {
            if (IsDead) return;

            // Make sequential queue out of received packets
            while (true)
            {
                bool foundNextPacket = false;
                for (int i = 0; i < receivedPackets.Count; i++)
                {
                    QuickPacket safePacket = receivedPackets[i];
                    if (safePacket.SeqNum == GetNum + 1)
                    {
                        foundNextPacket = true;
                        try
                        {
                            ReadyPacketTryEnqueue(safePacket, true);
                        }
                        catch (FormatException)
                        {
                            FinishConnection();
                            IsError = true;
                            return;
                        }
                        break;
                    }
                }
                if (foundNextPacket) GetNum++;
                else break;
            }

            // Delete packets that are no longer needed
            receivedPackets.RemoveAll(p => (p.SeqNum - GetNum <= 0));

            // Check for retransmissions
            AvgRTT = (float)AverageRTT();
            _RetransmittedNow.Clear();
            for (int i = 0; i < sendingPackets.Count; i++)
            {
                QuickPacket safePacket = sendingPackets[i];
                safePacket.ReduceTime(deltaTime);
                if (safePacket.TimeToRetransmission == 0f)
                {
                    if (safePacket.SeqNum - AckNum <= 0)
                    {
                        sendingPackets.RemoveAt(i--);
                    }
                    else
                    {
                        if (safePacket.TransmissionCount < MaxTransmissions)
                        {
                            Transmit(safePacket);
                            _RetransmittedNow.Add(safePacket.SeqNum);
                        }
                        else
                        {
                            FinishConnection();
                            return;
                        }
                    }
                }
            }

            // Ignore retransmitted when calculating RTT
            if (_RetransmittedNow.Count > 0)
            {
                PacketTimestamps.ForEachRemove(tuple => _RetransmittedNow.Contains(tuple.seq));
            }

            // Acknowledgements send
            currentAckCycleTime += deltaTime;
            bool ackSent = false;
            while (currentAckCycleTime >= AckCycleTime)
            {
                if (!ackSent)
                {
                    Send(new None(0), false); // ACK packet (empty packet using fast mode)
                    ackSent = true;
                }
                currentAckCycleTime -= AckCycleTime;
            }

            // Safe empty packet send
            currentSafeCycleTime += deltaTime;
            if(currentSafeCycleTime > SafeCycleTime)
            {
                // Send safe packets to ensure that the other side is constantly alive
                Send(new None(0), true);
                currentSafeCycleTime = 0f;
            }
        }

        public void Send(Packet packet, bool safemode = true)
        {
            if (IsDead) return;

            if (safemode)
            {
                SendSafePacket(new QuickPacket(++SeqNum, GetNum, 0, packet));
            }
            else
            {
                Transmit(new QuickPacket(SeqNum, GetNum, (byte)PacketFlag.FAS, packet));
            }
        }

        public Queue<Packet> Receive()
        {
            if (IsDead) return new Queue<Packet>();

            Queue<Packet> readyToRead = downloadablePackets;
            downloadablePackets = new Queue<Packet>();
            return readyToRead;
        }

        public void PushFromWeb(byte[] bytes, bool hasSYN = false)
        {
            if (IsDead) return;

            // Drop packets if too many
            if (receivedPackets.Count >= MaxStayingPackets ||
               downloadablePackets.Count >= MaxStayingPackets) return;

            QuickPacket safePacket = new QuickPacket();
            if (!hasSYN) safePacket.Encryption = bytes => Encryption.DecryptAES(bytes, KeyAES);
            if (!safePacket.TryDeserialize(bytes)) return;

            int diff = safePacket.SeqNum - GetNum;

            if (!safePacket.HasFlag(PacketFlag.FAS)) // safe mode
            {
                // range drop
                if (diff <= 0 || diff > MaxSeqTolerance)
                    return;

                // duplication drop
                if (receivedPackets.Any(p => p.SeqNum == safePacket.SeqNum))
                    return;

                // safe enqueue
                receivedPackets.Add(safePacket);
            }
            else // fast mode
            {
                // range drop
                if (diff < -MaxSeqTolerance || diff > MaxSeqTolerance)
                    return;

                // enqueue
                try
                {
                    ReadyPacketTryEnqueue(safePacket, false);
                }
                catch (FormatException)
                {
                    FinishConnection();
                    IsError = true;
                    return;
                }
            }

            // best received acknowledgement get
            if (safePacket.AckNum - AckNum > 0)
            {
                AckNum = safePacket.AckNum;
                PongRTT(safePacket.AckNum);
            }

            // end connection
            if (safePacket.HasFlag(PacketFlag.FIN))
                IsDead = true;
        }

        public void FinishConnection()
        {
            if(IsDead) return;

            // Send 3 FIN flags (to ensure they arrive).
            // If they don't, protocol will automatically disconnect after a few seconds.

            QuickPacket safePacket = new QuickPacket(
                SeqNum,
                GetNum,
                (byte)PacketFlag.FAS | (byte)PacketFlag.FIN,
                new None(0)
                );

            const int FIN_COUNT = 3;
            for (int i = 0; i < FIN_COUNT; i++)
                Transmit(safePacket);

            IsDead = true;
        }

        private void SendSafePacket(QuickPacket safePacket)
        {
            Transmit(safePacket);
            PingRTT(safePacket.SeqNum);
            sendingPackets.Add(safePacket);
        }

        private void Transmit(QuickPacket safePacket)
        {
            if (!safePacket.HasFlag(PacketFlag.SYN))
            {
                safePacket.Encryption = bytes => Encryption.EncryptAES(bytes, KeyAES);
            }
            else
            {
                if (safePacket.HasFlag(PacketFlag.RSA))
                {
                    safePacket.Encryption = bytes => Encryption.EncryptRSA(bytes, KeyRSA);
                    Core.Debug.Log("Transmiting RSA-encrypted SYN.");
                }
                else
                {
                    if (IPAddress.IsLoopback(EndPoint.Address))
                        Core.Debug.LogWarning("Transmiting unencrypted SYN to localhost.");
                    else
                        Core.Debug.LogWarning("Transmiting unencrypted SYN!");
                }
            }

            // Set control sequence
            safePacket.Packet.ControlSequence = safePacket.MakeControlSequence();

            // Retransmission time reset
            if (!safePacket.HasFlag(PacketFlag.FAS))
                safePacket.Transmited(AvgRTT + OffsetRTT);

            // Drop simulate
            if (DebugDropChance != 0f && Rand.NextDouble() < DebugDropChance)
                return;

            byte[] payload = safePacket.Serialize();
            SendToEndPoint(payload);
        }

        private bool ReadyPacketTryEnqueue(QuickPacket safePacket, bool is_safe)
        {
            // No payload is not allowed
            if (safePacket.Packet == null)
                throw new FormatException();

            // AllowConnection packets only with SYN flag allowed
            if (safePacket.Packet.ID == Payload.CmdID<AllowConnection>() &&
                !safePacket.HasFlag(PacketFlag.SYN))
                return false;

            // Stop packets generate on server side, they cannot be sent through network
            if (safePacket.Packet.ID == Payload.CmdID<Stop>())
                return false;

            // Control order and throw exception if something wrong
            if (safePacket.Packet.ControlSequence != safePacket.MakeControlSequence())
                throw new FormatException();

            // Enqueue if everything ok
            downloadablePackets.Enqueue(safePacket.Packet);
            return true;
        }

        private void PingRTT(int seq)
        {
            PacketTimestamps.ForEachRemove(tuple => !Timestamp.InTimestamp(tuple.time));
            PacketTimestamps.AddLast((seq, Timestamp.GetTimestamp()));
        }

        private void PongRTT(int seq)
        {
            PacketTimestamps.ForEachRemove(tuple => tuple.seq - seq <= 0, tuple =>
            {
                long delta = Timestamp.GetTimestamp() - tuple.time;

                PacketRTTs.AddLast(delta);
                if (PacketRTTs.Count > MaxRTTs)
                    PacketRTTs.RemoveFirst();
            });
        }

        private double AverageRTT()
        {
            if (PacketRTTs.Count == 0)
                return DefaultAvgRTT; // not enough data

            return PacketRTTs.Median() / 1000.0;
        }
    }
}

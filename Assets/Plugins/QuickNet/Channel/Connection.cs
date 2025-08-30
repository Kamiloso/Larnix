using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Net;
using System.Security.Cryptography;
using System.Threading.Tasks;
using QuickNet.Commands;
using QuickNet.Processing;
using QuickNet;

namespace QuickNet.Channel
{
    public class Connection
    {
        private UdpClient Udp = null;
        public IPEndPoint EndPoint { get; private set; } = null;

        private byte[] KeyAES = null;

        private uint SeqNum = 0; // last sent message ID
        private uint AckNum = 0; // last acknowledged message ID
        private uint GetNum = 0; // last received message ID

        private uint LastSendSequence = 0; // control number to check packet order
        private uint LastReceiveSequence = 0; // control number to check packet order

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

        private List<SafePacket> sendingPackets = new List<SafePacket>();
        private List<SafePacket> receivedPackets = new List<SafePacket>();
        private Queue<Packet> downloadablePackets = new Queue<Packet>();

        private readonly List<Task> pendingSendingTasks = new List<Task>();
        private const int MAX_PENDING_SENDING = 64;

        private readonly LinkedList<(uint seq, long time)> PacketTimestamps = new();
        private readonly LinkedList<long> PacketRTTs = new();
        private readonly HashSet<uint> _RetransmittedNow = new(); // local
        private const int MaxRTTs = 10;
        private const float OffsetRTT = 0.050f; // 50 ms
        private const float DefaultAvgRTT = 0.6f;

        public float AvgRTT { get; private set; } = DefaultAvgRTT;

        public readonly bool IsClient;

        public Connection(UdpClient udp, IPEndPoint endPoint, byte[] keyAES, Packet synPacket = null, RSA keyPublicRSA = null)
        {
            Udp = udp;
            EndPoint = endPoint;

            if (keyAES != null && keyAES.Length == 16)
                KeyAES = keyAES;
            else
                throw new System.Exception("Wrong AES key format.");

            IsClient = synPacket != null;

            if (synPacket != null) // CLIENT, use RSA public key
            {
                // Send SYN packet
                SafePacket.PacketFlag flags = SafePacket.PacketFlag.SYN;
                Encryption.Settings encrypt = null;
                if(keyPublicRSA != null)
                {
                    flags |= SafePacket.PacketFlag.RSA;
                    encrypt = new Encryption.Settings(Encryption.Settings.Type.RSA, keyPublicRSA);
                }

                SafePacket safePacket = new SafePacket(
                    ++SeqNum,
                    GetNum,
                    (byte)flags,
                    synPacket
                    );
                safePacket.Encrypt = encrypt;

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
                    SafePacket safePacket = receivedPackets[i];
                    if (safePacket.SeqNum == GetNum + 1)
                    {
                        foundNextPacket = true;
                        try
                        {
                            ReadyPacketTryEnqueue(safePacket, true);
                        }
                        catch(System.Exception e)
                        {
                            if (e.Message == "WRONG_PACKET_ORDER")
                            {
                                FinishConnection();
                                IsError = true;
                                return;
                            }
                            else throw;
                        }
                        break;
                    }
                }
                if (foundNextPacket) GetNum++;
                else break;
            }

            // Delete packets that are no longer needed
            receivedPackets.RemoveAll(p => p.SeqNum <= GetNum);

            // Check for retransmissions
            AvgRTT = (float)AverageRTT();
            _RetransmittedNow.Clear();
            for (int i = 0; i < sendingPackets.Count; i++)
            {
                SafePacket safePacket = sendingPackets[i];
                safePacket.ReduceTime(deltaTime);
                if (safePacket.TimeToRetransmission == 0f)
                {
                    if (safePacket.SeqNum <= AckNum)
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
                    Send(null, false); // ACK packet (empty packet using fast mode)
                    ackSent = true;
                }
                currentAckCycleTime -= AckCycleTime;
            }

            // Safe empty packet send
            currentSafeCycleTime += deltaTime;
            if(currentSafeCycleTime > SafeCycleTime)
            {
                // Send safe packets to ensure that the other side is constantly alive
                Send(new Packet(CmdID.None, 0, null), true);
                currentSafeCycleTime = 0f;
            }
        }

        public void Send(Packet packet, bool safemode = true)
        {
            if (IsDead) return;

            if (safemode)
            {
                packet.ControlSequence = ++LastSendSequence;
                SendSafePacket(new SafePacket(
                    ++SeqNum,
                    GetNum,
                    0,
                    packet
                    ));
            }
            else
            {
                Transmit(new SafePacket(
                    0,
                    GetNum,
                    (byte)SafePacket.PacketFlag.FAS,
                    packet
                    ));
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

            SafePacket safePacket = new SafePacket();
            if (!hasSYN)
                safePacket.Encrypt = new Encryption.Settings(Encryption.Settings.Type.AES, KeyAES);

            if (!safePacket.TryDeserialize(bytes))
                return;

            if (!safePacket.HasFlag(SafePacket.PacketFlag.FAS)) // safe mode
            {
                if (safePacket.SeqNum <= GetNum)
                    return;

                if (receivedPackets.Any(p => p.SeqNum == safePacket.SeqNum))
                    return;

                receivedPackets.Add(safePacket);
            }
            else // fast mode
            {
                try
                {
                    ReadyPacketTryEnqueue(safePacket, false);
                }
                catch (System.Exception e)
                {
                    if (e.Message == "WRONG_PACKET_ORDER")
                    {
                        FinishConnection();
                        IsError = true;
                        return;
                    }
                    else throw;
                }
            }

            // best received acknowledgement get
            if (safePacket.AckNum > AckNum)
            {
                AckNum = safePacket.AckNum;
                PongRTT(safePacket.AckNum);
            }

            // end connection
            if (safePacket.HasFlag(SafePacket.PacketFlag.FIN))
                IsDead = true;
        }

        public void FinishConnection()
        {
            if(IsDead) return;

            // Send 3 FIN flags (to ensure they arrive).
            // If they don't, protocol will automatically disconnect after a few seconds.

            SafePacket safePacket = new SafePacket(
                0,
                GetNum,
                (byte)SafePacket.PacketFlag.FAS | (byte)SafePacket.PacketFlag.FIN,
                null
                );

            const int FIN_COUNT = 3;
            for (int i = 0; i < FIN_COUNT; i++)
                Transmit(safePacket);

            IsDead = true;

            WaitForPendingPackets();
        }

        public void WaitForPendingPackets()
        {
            Task.WaitAll(pendingSendingTasks.ToArray());
            pendingSendingTasks.RemoveAll(t => t.IsCompleted);
        }

        private void SendSafePacket(SafePacket safePacket)
        {
            Transmit(safePacket);
            PingRTT(safePacket.SeqNum);
            sendingPackets.Add(safePacket);
        }

        private void Transmit(SafePacket safePacket)
        {
            if (!safePacket.HasFlag(SafePacket.PacketFlag.SYN))
                safePacket.Encrypt = new Encryption.Settings(Encryption.Settings.Type.AES, KeyAES);
            else
            {
                if (safePacket.HasFlag(SafePacket.PacketFlag.RSA))
                    Debug.Log("Transmiting RSA-encrypted SYN.");
                else if (IPAddress.IsLoopback(EndPoint.Address))
                    Debug.LogWarning("Transmiting unencrypted SYN to localhost.");
                else
                    Debug.LogWarning("Transmiting unencrypted SYN!");
            }

            // Retransmission time reset
            if (!safePacket.HasFlag(SafePacket.PacketFlag.FAS))
                safePacket.Transmited(AvgRTT + OffsetRTT);

            // Drop simulate
            if (DebugDropChance != 0f && Rand.NextDouble() < DebugDropChance)
                return;

            byte[] payload = safePacket.Serialize();
            Task task = Udp.SendAsync(payload, payload.Length, EndPoint);
            pendingSendingTasks.Add(task);

            pendingSendingTasks.RemoveAll(t => t.IsCompleted);
            while (pendingSendingTasks.Count > MAX_PENDING_SENDING)
            {
                Task completed = Task.WhenAny(pendingSendingTasks).GetAwaiter().GetResult();
                pendingSendingTasks.Remove(completed);
            }
        }

        private bool ReadyPacketTryEnqueue(SafePacket safePacket, bool is_safe)
        {
            // No payload, no problem
            if (safePacket.Payload == null)
                return false;

            // AllowConnection packets only with SYN flag allowed
            if (safePacket.Payload.ID == CmdID.AllowConnection &&
                !safePacket.HasFlag(SafePacket.PacketFlag.SYN))
                return false;

            // Stop packets generate on server side, they cannot be sent through network
            if (safePacket.Payload.ID == CmdID.Stop)
                return false;

            // Control order and throw exception if something wrong
            if(safePacket.Payload.ControlSequence != 0 && (!is_safe || safePacket.Payload.ControlSequence != ++LastReceiveSequence))
                throw new System.Exception("WRONG_PACKET_ORDER");

            // Enqueue and RTTs manage if everything ok
            downloadablePackets.Enqueue(safePacket.Payload);
            return true;
        }

        private void PingRTT(uint seq)
        {
            PacketTimestamps.ForEachRemove(tuple => !Timestamp.InTimestamp(tuple.time));
            PacketTimestamps.AddLast((seq, Timestamp.GetTimestamp()));
        }

        private void PongRTT(uint seq)
        {
            PacketTimestamps.ForEachRemove(tuple => tuple.seq <= seq, tuple =>
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

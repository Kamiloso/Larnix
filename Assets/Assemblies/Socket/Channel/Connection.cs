using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using Larnix.Socket.Packets;
using System;
using Larnix.Core.Utils;
using Larnix.Socket.Structs;
using Larnix.Socket.Security.Keys;

namespace Larnix.Socket.Channel
{
    internal class Connection
    {
        public const uint MaxStayingPackets = 128;
        private const int MaxSeqTolerance = 128;

        private const float AckCycleTime = 0.1f;
        private float currentAckCycleTime = 0.0f;

        private const float SafeCycleTime = 0.5f;
        private float currentSafeCycleTime = 0.0f;

        private const float DebugDropChance = 0.0f;

        // -------------------------------------------

        private readonly Retransmitter _retransmitter = new Retransmitter(8);

        public readonly IPEndPoint EndPoint;
        public readonly bool IsClient;

        private readonly Action<byte[]> SendBytes;
        private readonly KeyRSA _rsaKey;
        private readonly KeyAES _aesKey;

        private int SeqNum = 0; // last sent message ID
        private int AckNum = 0; // last acknowledged message ID
        private int GetNum = 0; // last received message ID

        public bool IsDead { get; private set; } = false; // blocks socket
        public bool IsError { get; private set; } = false; // communiactes that an error occured

        private List<PayloadBox> sendingBoxes = new();
        private List<PayloadBox> receivedBoxes = new();
        private Queue<HeaderSpan> readyPackets = new();

        private readonly LinkedList<(int seq, long time)> PacketTimestamps = new();
        private readonly LinkedList<long> PacketRTTs = new();
        private readonly HashSet<int> _RetransmittedNow = new(); // local

        private const int MaxRTTs = 10;
        private const float OffsetRTT = 0.050f; // 50 ms
        private const float DefaultAvgRTT = 0.6f; // assumes 600 ms for safety

        public float AvgRTT { get; private set; } = DefaultAvgRTT;

        public Connection(INetworkInteractions udp, IPEndPoint target, KeyAES aesKey, Payload synPacket = null, KeyRSA rsaKey = null)
        {
            EndPoint = target;
            IsClient = synPacket != null;

            SendBytes = b => udp.Send(target, b);
            _rsaKey = rsaKey;
            _aesKey = aesKey;

            if (synPacket != null) // send SYN packet (encrypted or not)
            {
                PayloadBox synBox = new PayloadBox(
                    seqNum: ++SeqNum,
                    ackNum: GetNum,
                    flags: (byte)(PacketFlag.SYN | (rsaKey != null ? PacketFlag.RSA : 0)),
                    payload: synPacket
                    );

                SendSafePacket(synBox);
            }
        }

        public void Tick(float deltaTime)
        {
            if (IsDead) return;

            // Make sequential queue out of received packets
            while (true)
            {
                bool foundNextPacket = false;
                for (int i = 0; i < receivedBoxes.Count; i++)
                {
                    PayloadBox box = receivedBoxes[i];
                    if (box.SeqNum == GetNum + 1)
                    {
                        foundNextPacket = true;
                        try
                        {
                            ReadyPacketTryEnqueue(box, true);
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
            receivedBoxes.RemoveAll(p => (p.SeqNum - GetNum <= 0));

            // Check for retransmissions
            AvgRTT = (float)AverageRTT();
            _RetransmittedNow.Clear();
            for (int i = 0; i < sendingBoxes.Count; i++)
            {
                PayloadBox box = sendingBoxes[i];

                try
                {
                    if (box.SeqNum - AckNum <= 0)
                    {
                        sendingBoxes.RemoveAt(i--);
                        _retransmitter.Discard(box);
                    }
                    else
                    {
                        if (_retransmitter.AllowRetransmission(box, (long)Math.Round((AvgRTT + OffsetRTT) * 1000f)))
                        {
                            Transmit(box);
                            _RetransmittedNow.Add(box.SeqNum);
                        }
                    }
                }
                catch (TimeoutException)
                {
                    FinishConnection();
                    return;
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

        public void Send(Payload packet, bool safemode = true)
        {
            if (IsDead) return;

            if (safemode)
            {
                PayloadBox box = new PayloadBox(
                    seqNum: ++SeqNum,
                    ackNum: GetNum,
                    flags: 0,
                    payload: packet
                    );

                SendSafePacket(box);
            }
            else
            {
                PayloadBox box = new PayloadBox(
                    seqNum: SeqNum,
                    ackNum: GetNum,
                    flags: (byte)PacketFlag.FAS,
                    payload: packet
                    );

                Transmit(box);
            }
        }

        public Queue<HeaderSpan> Receive()
        {
            if (IsDead) return new();

            var ready = readyPackets;
            readyPackets = new();
            return ready;
        }

        public void PushFromWeb(byte[] networkBytes, bool hasSYN = false)
        {
            if (IsDead) return;

            // Drop packets if too many
            if (receivedBoxes.Count >= MaxStayingPackets ||
                readyPackets.Count >= MaxStayingPackets)
                return;

            if (PayloadBox.TryDeserialize(networkBytes, !hasSYN ? _aesKey : KeyEmpty.GetInstance(), out PayloadBox box))
            {
                int diff = box.SeqNum - GetNum;

                if (!box.HasFlag(PacketFlag.FAS))
                {
                    if (diff <= 0 || diff > MaxSeqTolerance) // range drop
                        return;

                    if (receivedBoxes.Any(p => p.SeqNum == box.SeqNum)) // duplication drop
                        return;

                    receivedBoxes.Add(box);
                }
                else
                {
                    if (diff < -MaxSeqTolerance || diff > MaxSeqTolerance) // range drop
                        return;

                    try
                    {
                        ReadyPacketTryEnqueue(box, false);
                    }
                    catch (FormatException)
                    {
                        FinishConnection();
                        IsError = true;
                        return;
                    }
                }

                // best received acknowledgement get
                if (box.AckNum - AckNum > 0)
                {
                    AckNum = box.AckNum;
                    PongRTT(box.AckNum);
                }

                // end connection
                if (box.HasFlag(PacketFlag.FIN))
                    IsDead = true;
            }
        }

        public void FinishConnection()
        {
            if(IsDead) return;

            // Send 3 FIN flags (to ensure they arrive).
            // If they don't, protocol will automatically disconnect after a few seconds.

            PayloadBox box = new PayloadBox(
                seqNum: SeqNum,
                ackNum: GetNum,
                flags: (byte)PacketFlag.FAS | (byte)PacketFlag.FIN,
                payload: new None(0)
                );

            const int Retries = 3;
            for (int i = 0; i < Retries; i++)
                Transmit(box);

            IsDead = true;
        }

        private void SendSafePacket(PayloadBox box)
        {
            Transmit(box);
            PingRTT(box.SeqNum);
            sendingBoxes.Add(box);

            _retransmitter.Add(box, (long)Math.Round((AvgRTT + OffsetRTT) * 1000f));
        }

        private void Transmit(PayloadBox box)
        {
            byte[] payload;

            if (!box.HasFlag(PacketFlag.SYN))
            {
                payload = box.Serialize(_aesKey);
            }
            else
            {
                if (box.HasFlag(PacketFlag.RSA))
                {
                    payload = box.Serialize(_rsaKey);
                    Core.Debug.Log("Transmiting RSA-encrypted SYN.");
                }
                else
                {
                    payload = box.Serialize(KeyEmpty.GetInstance());
                    Core.Debug.LogWarning("Transmiting unencrypted SYN!");
                }
            }

            // Drop simulate
            if (DebugDropChance != 0f && Common.Rand().NextDouble() < DebugDropChance)
                return;

            SendBytes(payload);
        }

        private bool ReadyPacketTryEnqueue(PayloadBox box, bool isSafe)
        {
            // AllowConnection packets only with SYN flag allowed
            if (Payload.TryConstructPayload<AllowConnection>(box.Bytes, out _) &&
                !box.HasFlag(PacketFlag.SYN))
                return false;

            // Stop packets generate on server side, they cannot be sent through network
            if (Payload.TryConstructPayload<Stop>(box.Bytes, out _))
                return false;

            // Enqueue if everything ok
            readyPackets.Enqueue(new HeaderSpan(box.Bytes));
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

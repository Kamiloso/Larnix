using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using Larnix.Socket.Packets;
using System;
using Larnix.Core.Utils;
using Larnix.Socket.Security.Keys;
using Larnix.Socket.Packets.Control;
using Larnix.Socket.Channel.Networking;
using Larnix.Socket.Helpers;
using Larnix.Core;

namespace Larnix.Socket.Channel
{
    internal class Connection : IDisposable
    {
        // --- Constants ---
        public const uint BUFFER_LIMIT = 128;
        private const int SEQ_TOLERANCE_FAST = 128;
        private const int SEQ_TOLERANCE_SAFE = 128;
        private const float DEBUG_DROP_RATE = 0.0f;

        // --- Role ---
        public enum ConnectionRole { Client, Server }
        public readonly ConnectionRole Role;

        // --- Public properties ---
        public IPEndPoint EndPoint => new IPEndPoint(endPoint.Address, endPoint.Port);
        public bool IsDead { get; private set; } = false; // blocks socket
        public float AvgRTT => rttTracker.AvgRTT; // ping basically

        // --- References ---
        private readonly IPEndPoint endPoint;
        private readonly Action<byte[]> sendBytes;
        private readonly Retransmitter retransmitter;
        private readonly RTTTracker rttTracker;
        private readonly CycleTimer ackTimer, safeAckTimer;

        // --- Buffers ---
        private List<PayloadBox> _sendBuffer = new();
        private Dictionary<int, PayloadBox> _recvBuffer = new();
        private Queue<HeaderSpan> _readyPackets = new();

        // --- Encryption keys ---
        private readonly KeyRSA _rsaKey;
        private readonly KeyAES _aesKey;

        // --- Sequence numbers ---
        public int SeqNum { get; private set; } = 0; // last sent message ID
        public int AckNum { get; private set; } = 0; // last acknowledged message ID
        public int GetNum { get; private set; } = 0; // last received message ID

        private Connection(INetworkInteractions udp, IPEndPoint target, KeyAES aesKey, ConnectionRole role, Payload synPacket = null, KeyRSA rsaKey = null)
        {
            endPoint = target;
            sendBytes = b => udp.Send(target, b);

            retransmitter = new Retransmitter(8);
            rttTracker = new RTTTracker(600, 100);
            _rsaKey = rsaKey; _aesKey = aesKey;
            Role = role;

            ackTimer = new CycleTimer(0.1f, () => Send(new None(0), false));
            safeAckTimer = new CycleTimer(0.5f, () => Send(new None(0), true));

            if (Role == ConnectionRole.Client)
            {
                PayloadBox synBox = new PayloadBox(
                    seqNum: ++SeqNum,
                    ackNum: GetNum,
                    flags: (byte)(PacketFlag.SYN | PacketFlag.RSA),
                    payload: synPacket
                    );

                SendSafe(synBox);
            }
        }

        public static Connection CreateClient(INetworkInteractions udp, IPEndPoint target, KeyAES aesKey, KeyRSA rsaKey, Payload synPacket)
        {
            return new Connection(udp, target, aesKey, ConnectionRole.Client, synPacket, rsaKey);
        }

        public static Connection CreateServer(INetworkInteractions udp, IPEndPoint target, KeyAES aesKey)
        {
            return new Connection(udp, target, aesKey, ConnectionRole.Server, null, null);
        }

        public void Tick(float deltaTime)
        {
            if (IsDead) return;

            // received dict --> ready queue
            FlushReceivedIntoReady();

            // retransmissions
            HashSet<int> retransmitted = RetransmitAll();
            if (retransmitted == null) return; // abort info
            rttTracker.ForgetSequences(retransmitted);

            // frequent & safe ACK send
            ackTimer.Tick(deltaTime);
            safeAckTimer.Tick(deltaTime);
        }

        private void FlushReceivedIntoReady()
        {
            while (true)
            {
                int nextSeq = GetNum + 1;
                if (_recvBuffer.TryGetValue(nextSeq, out PayloadBox box))
                {
                    ReadyPacketTryEnqueue(box);
                    _recvBuffer.Remove(nextSeq);
                    GetNum++;
                }
                else break;
            }

            // remove old
            _recvBuffer.Keys
                .Where(k => k - GetNum <= 0)
                .ToList()
                .ForEach(k => _recvBuffer.Remove(k));
        }

        private HashSet<int> RetransmitAll()
        {
            HashSet<int> report = new();

            for (int i = 0; i < _sendBuffer.Count; i++)
            {
                PayloadBox box = _sendBuffer[i];

                if (box.SeqNum - AckNum <= 0)
                {
                    _sendBuffer.RemoveAt(i--);
                    retransmitter.Discard(box);
                }
                else
                {
                    if (retransmitter.AllowRetransmission(box, rttTracker.WaitingTimeMs(), out bool isTimeout))
                    {
                        Transmit(box);
                        report.Add(box.SeqNum);
                    }

                    if (isTimeout)
                    {
                        Dispose();
                        return null; // abort signal
                    }
                }
            }

            return report;
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
                    payload: packet);

                SendSafe(box);
            }
            else
            {
                PayloadBox box = new PayloadBox(
                    seqNum: SeqNum,
                    ackNum: GetNum,
                    flags: (byte)PacketFlag.FAS,
                    payload: packet);

                Transmit(box);
            }
        }

        private void SendSafe(PayloadBox box)
        {
            Transmit(box);

            _sendBuffer.Add(box); // remember box
            retransmitter.Add(box, rttTracker.WaitingTimeMs()); // track time

            rttTracker.Ping(box.SeqNum);
        }

        private void Transmit(PayloadBox box)
        {
            bool isSyn = box.HasFlag(PacketFlag.SYN);
            bool isRsa = box.HasFlag(PacketFlag.RSA);

            if (isSyn != isRsa) throw new ArgumentException("Unsupported flag combination!");

            if (DEBUG_DROP_RATE > 0f && Common.Rand().NextDouble() < DEBUG_DROP_RATE)
                return; // simulate network problems
            
            byte[] payload = box.Serialize(isSyn ? _rsaKey : _aesKey);
            sendBytes(payload);
        }

        public void PushFromWeb(byte[] networkBytes, bool hasSYN = false)
        {
            if (IsDead) return;

            if (_recvBuffer.Count >= BUFFER_LIMIT ||
                _readyPackets.Count >= BUFFER_LIMIT)
                return; // too many -> drop

            IEncryptionKey key = hasSYN ? KeyEmpty.GetInstance() : _aesKey;
            if (PayloadBox.TryDeserialize(networkBytes, key, out PayloadBox box))
            {
                int diff = box.SeqNum - GetNum;

                if (!box.HasFlag(PacketFlag.FAS))
                {
                    if (diff <= 0 || diff > SEQ_TOLERANCE_SAFE)
                        return; // wrong range -> drop

                    _recvBuffer.TryAdd(box.SeqNum, box);
                }
                else
                {
                    if (diff < -SEQ_TOLERANCE_FAST || diff > SEQ_TOLERANCE_FAST)
                        return; // wrong range -> drop

                    ReadyPacketTryEnqueue(box);
                }

                if (box.AckNum - AckNum > 0)
                {
                    AckNum = box.AckNum;
                    rttTracker.Pong(box.AckNum);
                }

                if (box.HasFlag(PacketFlag.FIN))
                {
                    IsDead = true;
                }
            }
        }

        private bool ReadyPacketTryEnqueue(PayloadBox box)
        {
            // AllowConnection packets only with SYN flag allowed
            if (Payload.TryConstructPayload<AllowConnection>(box.Bytes, out _) &&
                !box.HasFlag(PacketFlag.SYN))
                return false;

            // Stop packets generate on server side, they cannot be sent through network
            if (Payload.TryConstructPayload<Stop>(box.Bytes, out _))
                return false;

            // Enqueue if everything ok
            _readyPackets.Enqueue(new HeaderSpan(box.Bytes));
            return true;
        }

        public Queue<HeaderSpan> Receive()
        {
            if (IsDead) return new();

            Queue<HeaderSpan> ready = _readyPackets;
            _readyPackets = new Queue<HeaderSpan>();
            return ready;
        }

        public HeaderSpan GenerateStop()
        {
            var nextGetNum = GetNum + 1;
            var payload = new Stop(0);

            return new HeaderSpan(payload.Serialize(nextGetNum));
        }

        public void Dispose()
        {
            if (IsDead) return;

            // Send 3 FIN flags (to ensure they arrive).
            // If they don't, protocol will automatically disconnect after a few seconds.

            PayloadBox box = new PayloadBox(
                seqNum: SeqNum,
                ackNum: GetNum,
                flags: (byte)PacketFlag.FAS | (byte)PacketFlag.FIN,
                payload: new None(0)
                );

            const int REPEATS = 3;
            for (int i = 0; i < REPEATS; i++)
                Transmit(box);

            IsDead = true;
        }
    }
}

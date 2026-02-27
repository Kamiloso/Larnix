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
    internal class Connection : ITickable, IDisposable
    {
        public const uint BUFFER_LIMIT = 128;
        private const int SEQ_TOLERANCE_FAST = 128;
        private const int SEQ_TOLERANCE_SAFE = 128;
        private const float DEBUG_DROP_RATE = 0.0f;

        public enum ConnectionRole { Client, Server }
        public ConnectionRole Role { get; }

        public int SeqNum { get; private set; } = 0; // last sent message ID
        public int AckNum { get; private set; } = 0; // last acknowledged message ID
        public int GetNum { get; private set; } = 0; // last received message ID

        public bool IsDead { get; private set; } // received everything already?
        public float AvgRTT => _rttTracker.AvgRTT; // basically ping

        private readonly Action<byte[]> _sendBytes;
        private readonly Retransmitter _retransmitter;
        private readonly RTTTracker _rttTracker;
        private readonly CycleTimer _ackTimer, _safeAckTimer;

        private readonly List<PayloadBox> _sendBuffer = new();
        private readonly Dictionary<int, PayloadBox> _recvBuffer = new();
        private readonly LinkedList<HeaderSpan> _readyPackets = new();

        private readonly KeyRSA _rsaKey;
        private readonly KeyAES _aesKey;

        private bool _connClosed = false; // blocks socket
        private bool _disposed = false;

        public static Connection CreateClient(
            INetworkInteractions udp, IPEndPoint target, KeyAES aesKey, KeyRSA rsaKey, Payload synPacket)
        {
            return new(udp, target, aesKey, ConnectionRole.Client, synPacket, rsaKey);
        }

        public static Connection CreateServer(
            INetworkInteractions udp, IPEndPoint target, KeyAES aesKey)
        {
            return new(udp, target, aesKey, ConnectionRole.Server, null, null);
        }

        private Connection(
            INetworkInteractions udp, IPEndPoint target, KeyAES aesKey,
            ConnectionRole role, Payload synPacket = null, KeyRSA rsaKey = null)
        {
            Role = role;

            _sendBytes = b => udp.Send(target, b);
            _retransmitter = new Retransmitter(8);
            _rttTracker = new RTTTracker(600, 100);
            _rsaKey = rsaKey; _aesKey = aesKey;

            _ackTimer = new CycleTimer(0.1f, () => Send(new None(0), false));
            _safeAckTimer = new CycleTimer(0.5f, () => Send(new None(0), true));

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

        public void Send(Payload packet, bool safemode = true)
        {
            if (_connClosed) return;

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

        // Tick trio while(...) (1)
        public void PushFromWeb(byte[] networkBytes, bool isSyn)
        {
            if (_connClosed) return;

            if (_recvBuffer.Count >= BUFFER_LIMIT) return;
            if (_readyPackets.Count >= BUFFER_LIMIT) return;

            IEncryptionKey key = isSyn ? KeyEmpty.GetInstance() : _aesKey; // RSA already removed
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
                    _rttTracker.Pong(box.AckNum);
                }

                if (box.HasFlag(PacketFlag.FIN))
                {
                    _connClosed = true;
                }
            }
        }

        // Tick trio (2)
        public void Tick(float deltaTime)
        {
            _frameBegin = true;

            FlushReceivedIntoReady();

            if (!_connClosed)
            {
                // retransmissions
                var retransmitted = RetransmitAll(out bool abort);
                if (abort)
                {
                    return; // connection closed, timeout, evacuate from method
                }
                _rttTracker.ForgetSequences(retransmitted);

                // frequent & safe ACK send
                _ackTimer.Tick(deltaTime);
                _safeAckTimer.Tick(deltaTime);
            }
        }

        // Tick trio while(...) (3)
        private bool _frameBegin = false;
        public bool TryReceive(out HeaderSpan packet, out bool stopSignal)
        {
            bool firstInFrame = _frameBegin;
            _frameBegin = false;

            if (_connClosed && _readyPackets.Count == 0)
            {
                if (!IsDead) // stop generate
                {
                    if (!firstInFrame) // delay stop until next frame
                    {
                        stopSignal = false;
                        packet = null;
                        return false;
                    }

                    int nextGetNum = GetNum + 1;
                    Payload payload = new Stop(0);
                    byte[] bytes = payload.Serialize(nextGetNum);

                    IsDead = true;

                    packet = new HeaderSpan(bytes);
                    stopSignal = true;
                    return true;
                }
                else
                {
                    throw new InvalidOperationException("Cannot receive from closed connection.");
                }
            }

            stopSignal = false;
            return _readyPackets.TryPopFirst(out packet);
        }

        public void Close()
        {
            if (_connClosed) return;

            // Send 3 FIN flags (to ensure they arrive).
            // If they don't, protocol will automatically disconnect after a few seconds.

            PayloadBox box = new PayloadBox(
                seqNum: SeqNum,
                ackNum: GetNum,
                flags: (byte)(PacketFlag.FAS | PacketFlag.FIN),
                payload: new None(0)
                );

            const int REPEATS = 3;
            for (int i = 0; i < REPEATS; i++)
                Transmit(box);

            _connClosed = true;
        }

        private void FlushReceivedIntoReady()
        {
            int nextSeq = GetNum + 1;
            while (_recvBuffer.TryGetValue(nextSeq, out PayloadBox box))
            {
                ReadyPacketTryEnqueue(box);
                _recvBuffer.Remove(nextSeq);

                GetNum++;
                nextSeq++;
            }

            // remove old
            _recvBuffer.Keys
                .Where(k => k - GetNum <= 0)
                .ToList()
                .ForEach(k => _recvBuffer.Remove(k));
        }

        private HashSet<int> RetransmitAll(out bool abort)
        {
            abort = false;
            HashSet<int> report = new();

            for (int i = 0; i < _sendBuffer.Count; i++)
            {
                PayloadBox box = _sendBuffer[i];
                if (box.SeqNum - AckNum <= 0)
                {
                    _sendBuffer.RemoveAt(i--);
                    _retransmitter.Discard(box);
                }
                else
                {
                    if (_retransmitter.AllowRetransmission(box, _rttTracker.WaitingTimeMs(), out bool isTimeout))
                    {
                        Transmit(box);
                        report.Add(box.SeqNum);
                    }

                    if (isTimeout)
                    {
                        Close();
                        abort = true;
                        return null;
                    }
                }
            }

            return report;
        }

        private void SendSafe(PayloadBox box)
        {
            Transmit(box);

            _sendBuffer.Add(box); // remember box
            _retransmitter.Add(box, _rttTracker.WaitingTimeMs()); // track time

            _rttTracker.Ping(box.SeqNum);
        }

        private void Transmit(PayloadBox box)
        {
            bool isSyn = box.HasFlag(PacketFlag.SYN);
            bool isRsa = box.HasFlag(PacketFlag.RSA);

            if (isSyn != isRsa)
                throw new ArgumentException("Unsupported flag combination!");

            if (DEBUG_DROP_RATE > 0f && Common.Rand().NextDouble() < DEBUG_DROP_RATE)
                return; // simulate network problems
            
            byte[] payload = box.Serialize(isSyn ? _rsaKey : _aesKey);
            _sendBytes(payload);
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
            _readyPackets.AddLast(new HeaderSpan(box.Bytes));
            return true;
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _disposed = true;
                Close();
            }
        }
    }
}

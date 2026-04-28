#nullable enable
using Larnix.Core;
using Larnix.Socket.Payload;
using Larnix.Socket.Payload.Structs;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Larnix.Socket.Channel.Components;

internal class ReliableTransmitter : ITickable
{
    private static int RetransmissionLimit => 5;
    private static int RetransmissionDelay => 50; // ms

    public long AvgRtt => _roundTripTracker.MedianRtt();

    private readonly Seqs _seqs;
    private readonly Action<byte[]> _sendAction;
    private readonly Action _closeAction;
    private readonly Dictionary<Seq, PendingPacket> _pending = new();
    private readonly RoundTripTracker _roundTripTracker = new();

    public ReliableTransmitter(Seqs seqs, Action<byte[]> sendAction, Action closeAction)
    {
        _seqs = seqs;
        _sendAction = sendAction;
        _closeAction = closeAction;
    }

    public void Transmit(in PayloadHeader header, byte[] payload)
    {
        _sendAction.Invoke(payload);

        if (!header.HasFlag(PacketFlag.FAS))
        {
            Seq seqNew = header.SeqNum;
            PendingPacket packet = new(payload);

            _pending.Add(seqNew, packet);
            _roundTripTracker.StartMeasure(packet);
        }
    }

    private readonly List<Seq> _acksToRemove = new();
    public void Acknowledge(Seq seqAck)
    {
        if (_seqs.AckNum >= seqAck) return;

        _seqs.AckNum = seqAck;

        _acksToRemove.Clear();
        _acksToRemove.AddRange(_pending.Keys.Where(seq => seq <= seqAck));

        foreach (Seq seq in _acksToRemove)
        {
            _pending.Remove(seq, out PendingPacket packet);

            Action<PendingPacket> delete = packet.Retries == 0
                ? _roundTripTracker.StopMeasure
                : _roundTripTracker.TerminateMeasure;

            delete.Invoke(packet);
        }
    }

    public void Tick(float deltaTime)
    {
        long waitTime = AvgRtt + RetransmissionDelay;

        foreach (var (seq, packet) in _pending)
        {
            if (packet.ReadyForRetransmission(waitTime))
            {
                if (packet.Retries < RetransmissionLimit)
                {
                    packet.RetransmissionApply();
                    _sendAction.Invoke(packet.Data);
                }
                else
                {
                    _closeAction.Invoke();
                }
            }
        }
    }
}

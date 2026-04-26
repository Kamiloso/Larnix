#nullable enable
using Larnix.Core;
using System;

namespace Larnix.Socket.Channel;

// WARNING: This class should be always compared by reference!
// It is used as a dictionary key, so DON'T OVERRIDE Equals, GetHashCode etc.

internal class PendingPacket
{
    public byte[] Data { get; }
    public int Retries { get; private set; }

    private long _lastSendTime = Timestamp.Now();

    public PendingPacket(byte[] data)
    {
        Data = data;
    }

    public bool ReadyForRetransmission(long waitTime)
    {
        long now = Timestamp.Now();
        long allowedDelay = waitTime;

        return now - _lastSendTime >= allowedDelay;
    }

    public void RetransmissionApply()
    {
        _lastSendTime = Timestamp.Now();
        Retries++;
    }
}

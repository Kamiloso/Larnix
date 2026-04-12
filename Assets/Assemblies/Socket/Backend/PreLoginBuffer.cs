#nullable enable
using System.Collections.Generic;
using Larnix.Socket.Security.Keys;
using System;

namespace Larnix.Socket.Backend;

internal class PreLoginBuffer
{
    public const int MAX_PACKETS = 64;
    public AllowConnection AllowConnection => _allowConnection;

    private readonly PayloadBox_Legacy _synBox;
    private readonly AllowConnection _allowConnection;
    private readonly Queue<byte[]> _buffer = new(MAX_PACKETS);
    private bool _receivedAny = false;

    public PreLoginBuffer(PayloadBox_Legacy synBox)
    {
        _synBox = synBox;

        if (!Payload_Legacy.TryConstructPayload(synBox.Bytes, out _allowConnection))
        {
            throw new InvalidOperationException("Failed to construct AllowConnection payload from the given synBox.");
        }
    }

    public void PushFromWeb(byte[] bytes)
    {
        if(_buffer.Count < MAX_PACKETS)
            _buffer.Enqueue(bytes);
    }

    public byte[]? TryReceive(out bool isSyn)
    {
        if (_receivedAny)
        {
            isSyn = false;

            if (_buffer.Count > 0)
                return _buffer.Dequeue();

            return null;
        }
        else
        {
            isSyn = true;

            _receivedAny = true;
            return _synBox.Serialize(KeyEmpty.Instance);
        }
    }
}

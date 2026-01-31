using System.Collections;
using System.Collections.Generic;
using Larnix.Socket.Packets;
using Larnix.Socket.Packets.Control;
using Larnix.Socket.Security.Keys;
using System;

namespace Larnix.Socket.Backend
{
    internal class PreLoginBuffer
    {
        public const int MAX_PACKETS = 64;
        public AllowConnection AllowConnection => _allowConnection;

        private readonly PayloadBox _synBox;
        private readonly AllowConnection _allowConnection;
        private readonly Queue<byte[]> _buffer = new(MAX_PACKETS);
        private bool _receivedAny = false;

        public PreLoginBuffer(PayloadBox synBox)
        {
            _synBox = synBox;

            if (!Payload.TryConstructPayload(synBox.Bytes, out _allowConnection))
            {
                throw new InvalidOperationException("Failed to construct AllowConnection payload from the given synBox.");
            }
        }

        public void PushFromWeb(byte[] bytes)
        {
            if(_buffer.Count < MAX_PACKETS)
                _buffer.Enqueue(bytes);
        }

        public byte[] TryReceive(out bool isSyn)
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
                return _synBox.Serialize(KeyEmpty.GetInstance());
            }
        }
    }
}

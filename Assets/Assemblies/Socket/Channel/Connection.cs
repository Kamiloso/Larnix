#nullable enable
using Larnix.Core;
using Larnix.Socket.Channel.Components;
using Larnix.Socket.Networking;
using Larnix.Socket.Payload;
using Larnix.Socket.Payload.Packets;
using Larnix.Socket.Payload.Structs;
using Larnix.Socket.Security.Keys;
using Larnix.Socket.Tools;
using System;
using System.Collections.Generic;
using System.Net;

namespace Larnix.Socket.Channel;

internal class Connection : ITickable, IDisposable
{
    public long AvgRtt => _transmitter.AvgRtt;
    public bool IsDead { get; private set; }

    public IPEndPoint Target => _socket.Target;

    private readonly Seqs _seqs = new();

    private readonly ITargetedSocket _socket;
    private readonly KeyAes _aes;

    private readonly HeaderProvider _headerProvider;
    private readonly ReliableReceiver _receiver;
    private readonly ReliableTransmitter _transmitter;

    private readonly CycleTimer _timerFast = new(100);
    private readonly CycleTimer _timerSlow = new(500);

    private readonly Queue<byte[]> _readyBuffer = new();
    private byte[] _current = Array.Empty<byte>();

    private bool _disposed;

    public Connection(ITargetedSocket socket, in FixedAes aesKey)
    {
        _socket = socket;
        _aes = KeyAes.FromStruct(aesKey);

        _headerProvider = new HeaderProvider(_seqs);
        _receiver = new ReliableReceiver(_seqs);
        _transmitter = new ReliableTransmitter(_seqs,
            sendAction: _socket.Send,
            closeAction: Close
            );

        _timerFast.OnInterval += () => Send(new None(), safemode: false);
        _timerSlow.OnInterval += () => Send(new None(), safemode: true);
    }

    public void SendHandshake(in AllowConnection payload, KeyRsa rsa)
    {
        if (IsDead) return;

        PayloadHeader header = _headerProvider.NextSyn();

        byte[] bytes = NetworkSerializer.ToBytes(header, payload, rsa);
        _transmitter.Transmit(header, bytes);
    }

    public void Send<T>(in T payload, bool safemode) where T : unmanaged
    {
        if (IsDead) return;

        PayloadHeader header = safemode
            ? _headerProvider.NextSafe()
            : _headerProvider.NextFast();

        byte[] bytes = NetworkSerializer.ToBytes(header, payload, _aes);
        _transmitter.Transmit(header, bytes);
    }

    public void PushFromWeb(byte[] data)
    {
        if (IsDead) return;

        if (!NetworkSerializer.TryPlainHeaderFromBytes(data, out PayloadHeader header)) return;
        if (!NetworkSerializer.TryDecryptNetworkBytes(data, _aes, out byte[] decrypted)) return;

        _receiver.Push(header, decrypted);

        if (header.HasFlag(PacketFlag.FIN))
        {
            Close();
        }
    }

    public void Tick(float deltaTime)
    {
        if (IsDead) return;

        _transmitter.Tick(deltaTime);

        _timerFast.Tick(deltaTime);
        _timerSlow.Tick(deltaTime);

        while (_receiver.TryPop(out byte[] decrypted))
        {
            _readyBuffer.Enqueue(decrypted);
        }
    }

    public bool MoveNext()
    {
        if (_readyBuffer.TryDequeue(out byte[] next))
        {
            _current = next;
            return true;
        }

        _current = Array.Empty<byte>();
        return false;
    }

    public bool TryCastCurrent<T>(out T result) where T : unmanaged
    {
        bool isInternalPacket =
            typeof(T) == typeof(Start) ||
            typeof(T) == typeof(Stop) ||
            typeof(T) == typeof(AllowConnection);

        if (!isInternalPacket) // block such packets, they are server-generated
        {
            result = default;
            return false;
        }

        return NetworkSerializer.TryDecryptedBytesAs(_current, out _, out result);
    }

    public void Close()
    {
        if (IsDead) return;

        const int FINS = 3;
        for (int i = 0; i < FINS; i++) // repeat to ensure that everything arrives
        {
            PayloadHeader header = _headerProvider.NextFin();
            None payload = new();

            byte[] bytes = NetworkSerializer.ToBytes(header, payload, _aes);
            _transmitter.Transmit(header, bytes);
        }

        _readyBuffer.Clear(); // discard any pending packets

        IsDead = true;
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        Close();

        _aes.Dispose();
    }
}

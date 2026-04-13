#nullable enable
using Larnix.Core;
using Larnix.Socket.Networking;
using Larnix.Socket.Payload;
using Larnix.Socket.Payload.Packets;
using Larnix.Socket.Security.Keys;
using System;
using System.Collections.Generic;
using System.Net;

namespace Larnix.Socket.Channel;

internal class QuickConnection : ITickable, IDisposable
{
    public float AvgRTT => _transmitter.AvgRTT;
    public bool IsDead { get; private set; }
    public IPEndPoint Target => _udp.Destination ?? // TODO: create two interfaces instead of this hacky solution
        throw new InvalidOperationException("No target available for the provided socket interface.");

    private readonly Seqs _seqs = new();

    private readonly INetworkInteractions _udp;
    private readonly KeyAES _aes;

    private readonly HeaderProvider _headerProvider;
    private readonly ReliableReceiver _receiver;
    private readonly ReliableTransmitter _transmitter;

    private readonly Queue<byte[]> _readyBuffer = new();
    private byte[] _current = Array.Empty<byte>();
    private CastPermission _castPermission = CastPermission.None;

    private bool _startEmitted;
    private bool _stopEmitted;

    private bool _disposed;

    private enum CastPermission
    {
        None,
        Normal,
        Full
    }

    public QuickConnection(INetworkInteractions udp, KeyAES aes)
    {
        _udp = udp;
        _aes = aes;

        _headerProvider = new HeaderProvider(_seqs);
        _receiver = new ReliableReceiver(_seqs);
        _transmitter = new ReliableTransmitter(_seqs, bytes =>
        {
            _udp.Send(new DataBox(Target, bytes));
        });
    }

    public void SendHandshake(in AllowConnection payload, KeyRSA rsa)
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

        while (_receiver.TryPop(out byte[] decrypted))
        {
            _readyBuffer.Enqueue(decrypted);
        }
    }

    public bool MoveNext()
    {
        if (!_startEmitted)
        {
            _startEmitted = true;
            _current = NetworkSerializer.PackAsIfDecrypted(new Start());
            _castPermission = CastPermission.Full;
            return true;
        }

        if (_readyBuffer.TryDequeue(out byte[] next))
        {
            _current = next;
            _castPermission = CastPermission.Normal;
            return true;
        }

        if (_disposed && !_stopEmitted)
        {
            _stopEmitted = true;
            _current = NetworkSerializer.PackAsIfDecrypted(new Stop());
            _castPermission = CastPermission.Full;
            return true;
        }

        _current = Array.Empty<byte>();
        _castPermission = CastPermission.None;
        return false;
    }

    public bool TryCastCurrent<T>(out T result) where T : unmanaged
    {
        bool deny = false;
        deny |= _castPermission == CastPermission.None;
        deny |= _castPermission == CastPermission.Normal && (typeof(T) == typeof(Start) || typeof(T) == typeof(Stop));

        if (deny)
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

        IsDead = true;
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        Close();
    }
}

#nullable enable
using Larnix.Core;
using Larnix.Socket.Session.Components;
using Larnix.Socket.Networking;
using Larnix.Socket.Payload;
using Larnix.Socket.Payload.Packets;
using Larnix.Socket.Payload.Structs;
using System;
using System.Collections.Generic;
using System.Net;
using Larnix.Socket.Session.Tools;
using Larnix.Socket.Security.Encryption;
using Larnix.Socket.Limiting;

namespace Larnix.Socket.Session;

internal class Connection : ITickable, IDisposable
{
    public long AvgRtt => _transmitter.AvgRtt;
    public IPEndPoint Target => _socket.Target;
    public bool IsClient { get; }
    public bool IsDead { get; private set; }

    private readonly ITargetedSocket _socket;
    private readonly AesKey _aes;

    private readonly Seqs _seqs;

    private readonly HeaderProvider _headerProvider;
    private readonly ReliableReceiver _receiver;
    private readonly ReliableTransmitter _transmitter;

    private readonly CycleTimer _timerFast = new(100);
    private readonly CycleTimer _timerSlow = new(500);

    private readonly Queue<byte[]> _readyBuffer = new();
    private byte[] _current = Array.Empty<byte>();

    private bool _disposed;

    public record HandshakeInfo(Credentials Credentials, RsaPublicKey Rsa);
    public Connection(ITargetedSocket socket, AesKey aes, HandshakeInfo? handshakeInfo = null)
    {
        _socket = socket;
        _aes = aes;

        IsClient = handshakeInfo != null;

        _seqs = IsClient // is client-side?
            ? new Seqs()
            : new Seqs() { RcvNum = new Seq(1) };

        _headerProvider = new HeaderProvider(_seqs);
        _receiver = new ReliableReceiver(_seqs);
        _transmitter = new ReliableTransmitter(_seqs, _socket.Send, Close);

        _timerFast.OnInterval += () => Send(new None(), safemode: false);
        _timerSlow.OnInterval += () => Send(new None(), safemode: true);

        if (IsClient) // client-side: send SYN with handshake payload
        {
            var (credentials, rsa) = handshakeInfo!;
            var fixedAes = FixedAes.FromKey(aes);

            PayloadHeader header = _headerProvider.NextSyn();
            AllowConnection payload = new(credentials, fixedAes);

            byte[] bytes = NetworkSerializer.ToBytes(header, payload, rsa);
            _transmitter.Transmit(header, bytes);
        }
        else // server-side: send anything to confirm receiving SYN
        {
            Send(new None(), safemode: true);
        }
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

        _transmitter.Acknowledge(header.AckNum);
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
        if (_readyBuffer.TryDequeue(out byte[] decrypted))
        {
            _current = decrypted;
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

        if (isInternalPacket) // block such packets, they are server-generated
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
        for (int i = 0; i < FINS; i++) // repeat to ensure everything arrives
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
    }
}

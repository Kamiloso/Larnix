#nullable enable
using Larnix.Core;
using Larnix.Socket.Limiters;
using Larnix.Socket.Server.Utility;
using Larnix.Socket.Networking;
using Larnix.Socket.Payload;
using Larnix.Socket.Security.Keys;
using Larnix.Socket.Tools;
using System;
using System.Net;
using Larnix.Socket.Server.Interfaces;

namespace Larnix.Socket.Server.Receivers;

internal class WebReceiver : ITickable, IDisposable
{
    private record ArriveInfo(IPEndPoint Target, string Cidr, byte[] Data);

    private readonly ISocket _socket;
    private readonly KeyRsa _rsa;
    private readonly IInfoProvider _infoProvider;
    private readonly Coroutines _coroutines;
    private readonly QuickSettings _settings;

    private readonly AsyncDecryptor _asyncDecryptor;
    private readonly ConnReceiver _connReceiver;
    private readonly RequestReceiver _requestReceiver;

    private readonly IBanProvider _bans;

    private readonly TrafficLimiter<string> _heavyLimiter;
    private readonly CycleTimer _heavyCleanupTimer;

    public WebReceiver(
        ISocket socket,
        KeyRsa rsa,
        IClients clients,
        IAsyncLogins asyncLogins,
        IInfoProvider infoProvider,
        Coroutines coroutines,
        QuickSettings settings
        )
    {
        _socket = socket;
        _rsa = rsa;
        _infoProvider = infoProvider;
        _coroutines = coroutines;
        _settings = settings;

        _asyncDecryptor = new AsyncDecryptor(settings);
        _connReceiver = new ConnReceiver(socket, clients, asyncLogins, coroutines, settings);
        _requestReceiver = new RequestReceiver(socket, asyncLogins, infoProvider, coroutines, settings);

        _bans = settings.Interfaces.BanProvider;

        _heavyLimiter = new TrafficLimiter<string>(
            maxTrafficLocal: _settings.Security.Limiters.HeavyPackets.PerNetwork,
            maxTrafficGlobal: _settings.Security.Limiters.HeavyPackets.Global
            );

        _heavyCleanupTimer = new CycleTimer(
            interval: _settings.Security.Limiters.HeavyPackets.ResetPeriodMs
            );

        _heavyCleanupTimer.OnInterval += _heavyLimiter.Reset;
    }

    public void Tick(float deltaTime)
    {
        _heavyCleanupTimer.Tick(deltaTime);

        while (_socket.TryReceive(out DataBox result))
        {
            IPEndPoint target = result.Target;
            string cidr = _infoProvider.GetCIDR(result.Target);
            byte[] data = result.Data;

            ArriveInfo arriveInfo = new(target, cidr, data);

            if (AllowIncoming(arriveInfo))
            {
                InterpretIncoming(arriveInfo);
            }
        }

        _connReceiver.Tick(deltaTime);
    }

    private bool AllowIncoming(ArriveInfo arriveInfo)
    {
        var (target, cidr, data) = arriveInfo;

        if (_bans.IsBannedIp(target.Address))
            return false;

        if (!NetworkSerializer.TryPlainHeaderFromBytes(data, out PayloadHeader header))
            return false;

        bool isHeavy =
            header.HasFlag(PacketFlag.SYN) ||
            header.HasFlag(PacketFlag.RSA) ||
            header.HasFlag(PacketFlag.NCN);

        return !isHeavy || _heavyLimiter.TryAdd(cidr);
    }

    private void InterpretIncoming(ArriveInfo arriveInfo)
    {
        var (target, cidr, data) = arriveInfo;

        if (!NetworkSerializer.TryPlainHeaderFromBytes(data, out PayloadHeader header))
            return;

        bool isSyn = header.HasFlag(PacketFlag.SYN);
        bool isRsa = header.HasFlag(PacketFlag.RSA);
        bool isNcn = header.HasFlag(PacketFlag.NCN);

        bool isTrueSyn = isSyn && !isNcn;
        bool isTrueRequest = isNcn && !isSyn;

        if (!isSyn && !isRsa && !isNcn)
        {
            _connReceiver.PushFromWeb(target, data);
        }
        
        if (isTrueSyn || isTrueRequest)
        {
            IKey? key = isRsa ? _rsa : null;

            _coroutines.Start(
                _asyncDecryptor.Decrypt(cidr, data, key),
                onResult: result =>
                {
                    bool success = result.Success;
                    byte[] decrypted = result.Decrypted;

                    if (success)
                    {
                        if (isTrueSyn) _connReceiver.EstablishConnection(target, cidr, decrypted);
                        if (isTrueRequest) _requestReceiver.HandleRequest(target, cidr, decrypted);
                    }
                });
        }
    }

    public void Dispose()
    {
        _connReceiver.Dispose();
    }
}

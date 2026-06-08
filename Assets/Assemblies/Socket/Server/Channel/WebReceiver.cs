#nullable enable
using Larnix.Core;
using Larnix.Socket.Limiting;
using Larnix.Socket.Networking;
using Larnix.Socket.Payload;
using System;
using System.Net;
using Larnix.Socket.Server.Interfaces;
using Larnix.Socket.Session.Tools;
using Larnix.Socket.Security.Encryption;

namespace Larnix.Socket.Server.Channel;

internal class WebReceiver : ITickable, IDisposable
{
    private record ArriveInfo(IPEndPoint Target, string Cidr, byte[] Data);

    private readonly ISocket _socket;
    private readonly Coroutines _coroutines;
    private readonly SecretProvider _secrets;
    private readonly Limiters _limiters;

    private readonly InfoProvider _infoProvider;
    private readonly AsyncDecryptor _asyncDecryptor;
    private readonly ConnReceiver _connReceiver;
    private readonly RequestReceiver _requestReceiver;

    private readonly IBanProvider _bans;

    public WebReceiver(
        MainServices services,
        InfoProvider infoProvider, AsyncDecryptor asyncDecryptor,
        ConnReceiver connReceiver, RequestReceiver requestReceiver)
    {
        _socket = services.Socket;
        _coroutines = services.Coroutines;
        _secrets = services.Secrets;
        _limiters = services.Limiters;

        _infoProvider = infoProvider;
        _asyncDecryptor = asyncDecryptor;
        _connReceiver = connReceiver;
        _requestReceiver = requestReceiver;

        _bans = services.Interfaces.BanProvider;
    }

    public void Tick(float deltaTime)
    {
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

        if (!_limiters.LightPackets.TryAdd(cidr))
            return false;

        if (!NetworkSerializer.TryPlainHeaderFromBytes(data, out PayloadHeader header))
            return false;

        bool isHeavy =
            header.HasFlag(PacketFlag.SYN) ||
            header.HasFlag(PacketFlag.RSA) ||
            header.HasFlag(PacketFlag.NCN);

        return !isHeavy || _limiters.HeavyPackets.TryAdd(cidr);
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
            IDecryptor? key = isRsa ? _secrets.Rsa : null;

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

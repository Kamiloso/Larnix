#nullable enable
using Larnix.Core;
using Larnix.Core.Limiters;
using Larnix.Socket.Server.Utility;
using Larnix.Socket.Networking;
using Larnix.Socket.Payload;
using Larnix.Socket.Payload.Packets;
using Larnix.Socket.Tools;
using System.Net;
using Larnix.Socket.Payload.Structs;
using Larnix.Core.Utils;

namespace Larnix.Socket.Server.Receivers;

internal class RequestReceiver : ITickable
{
    private readonly ISocket _socket;
    private readonly IAsyncLogins _asyncLogins;
    private readonly IInfoProvider _infoProvider;
    private readonly Coroutines _coroutines;
    private readonly QuickConfig _settings;

    private readonly TrafficLimiter<string> _requestLimiter;
    private readonly CycleTimer _requestCleanupTimer;

    public RequestReceiver(
        ISocket socket,
        IAsyncLogins asyncLogins,
        IInfoProvider infoProvider,
        Coroutines coroutines,
        QuickConfig settings
        )
    {
        _socket = socket;
        _asyncLogins = asyncLogins;
        _infoProvider = infoProvider;
        _coroutines = coroutines;
        _settings = settings;

        _requestLimiter = new TrafficLimiter<string>(
            maxTrafficLocal: settings.Security.Limiters.Requests.PerNetwork,
            maxTrafficGlobal: settings.Security.Limiters.Requests.Global
            );

        _requestCleanupTimer = new CycleTimer(
            interval: settings.Security.Limiters.Requests.ResetPeriodMs
            );

        _requestCleanupTimer.OnInterval += _requestLimiter.Reset;
    }

    public void Tick(float deltaTime)
    {
        _requestCleanupTimer.Tick(deltaTime);
    }

    public void HandleRequest(IPEndPoint target, string cidr, byte[] decrypted)
    {
        if (!_requestLimiter.TryAdd(cidr)) return;

        CheckServerInfo(target, decrypted);
        CheckLoginTry(target, decrypted);
    }

    private void CheckServerInfo(IPEndPoint target, byte[] decrypted)
    {
        if (!NetworkSerializer.TryDecryptedBytesAs(
            decrypted, out PayloadHeader header, out P_ServerInfo serverInfo)) return;

        PayloadHeader aHeader = new(
            seqNum: header.SeqNum,
            flags: (byte)PacketFlag.NCN
            );

        SendAnswer(target, header, new A_ServerInfo(
            info: _infoProvider.ServerInfo,
            challengeId: _asyncLogins.GetChallengeId(serverInfo.Nickname)
            ));
    }

    private void CheckLoginTry(IPEndPoint target, byte[] decrypted)
    {
        if (!NetworkSerializer.TryDecryptedBytesAs(
            decrypted, out PayloadHeader header, out P_LoginTry loginTry)) return;

        void InformResult(bool success)
            => SendAnswer(target, header, new A_LoginTry(success));

        Credentials credentials = loginTry.Credentials;

        long uid = _settings.Interfaces.UserRepository.FindByNickname(credentials.Nickname)?.Uid
            ?? _settings.Interfaces.UserRepository.NextFreeUid();

        _coroutines.Start(
            method: _asyncLogins.Login(uid, credentials),
            onResult: success =>
            {
                if (!success)
                {
                    InformResult(false);
                }
                else if (loginTry.IsPasswordChangeRequest())
                {
                    string nickname = credentials.Nickname;
                    string newPassword = loginTry.NewPassword!.Value;

                    _coroutines.Start(
                        method: _asyncLogins.SetPassword(uid, nickname, newPassword),
                        onResult: success =>
                        {
                            InformResult(success);
                        });
                }
                else
                {
                    InformResult(true);
                }
            });
    }

    private void SendAnswer<T>(IPEndPoint target, in PayloadHeader oldHeader, in T answer) where T : unmanaged
    {
        PayloadHeader aHeader = new(
            seqNum: oldHeader.SeqNum,
            flags: (byte)PacketFlag.NCN
            );

        _socket.Send(new DataBox(
            Target: target,
            Data: NetworkSerializer.ToBytes(aHeader, answer, null) // always plaintext
            ));
    }
}

#nullable enable
using Larnix.Core;
using Larnix.Socket.Networking;
using Larnix.Socket.Payload;
using Larnix.Socket.Payload.Packets;
using System.Net;
using Larnix.Socket.Payload.Structs;
using Larnix.Socket.Session.Tools;
using Larnix.Socket.Server.Users;

namespace Larnix.Socket.Server.Channel;

internal class RequestReceiver
{
    private readonly ISocket _socket;
    private readonly Coroutines _coroutines;
    private readonly InfoProvider _infoProvider;
    private readonly AsyncLogins _asyncLogins;

    public RequestReceiver(MainServices services, InfoProvider infoProvider, AsyncLogins asyncLogins)
    {
        _socket = services.Socket;
        _coroutines = services.Coroutines;
        _infoProvider = infoProvider;
        _asyncLogins = asyncLogins;
    }

    public void HandleRequest(IPEndPoint target, string cidr, byte[] decrypted)
    {
        CheckServerInfo(target, cidr, decrypted);
        CheckLoginTry(target, cidr, decrypted);
    }

    private void CheckServerInfo(IPEndPoint target, string _, byte[] decrypted)
    {
        if (!NetworkSerializer.TryDecryptedBytesAs(
            decrypted, out PayloadHeader header, out P_ServerInfo serverInfo)) return;

        PayloadHeader aHeader = new(
            seqNum: header.SeqNum,
            flags: (byte)PacketFlag.NCN
            );

        SendAnswer(target, aHeader, new A_ServerInfo(
            info: _infoProvider.CreateServerInfo(),
            challengeId: _asyncLogins.GetChallengeId(serverInfo.Nickname)
            ));
    }

    private void CheckLoginTry(IPEndPoint target, string _, byte[] decrypted)
    {
        if (!NetworkSerializer.TryDecryptedBytesAs(
            decrypted, out PayloadHeader header, out P_LoginTry loginTry)) return;

        void InformResult(bool success)
            => SendAnswer(target, header, new A_LoginTry(success));

        Credentials credentials = loginTry.Credentials;
        bool isLoopback = IPAddress.IsLoopback(target.Address);

        _coroutines.Start(
            method: _asyncLogins.LoginOrRegister(credentials, isLoopback),
            onResult: success =>
            {
                if (!success)
                {
                    InformResult(false);
                }
                else if (loginTry.IsPasswordChange())
                {
                    var nickname = credentials.Nickname;
                    var newPassword = loginTry.NewPassword;

                    _coroutines.Start(
                        method: _asyncLogins.SetPassword(nickname, newPassword),
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

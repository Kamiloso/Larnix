#nullable enable
using Larnix.Core;
using Larnix.Core.Serialization;
using Larnix.Core.Utils;
using Larnix.Socket.Payload.Structs;
using Larnix.Socket.Security;
using Larnix.Socket.Security.Encryption;
using Larnix.Socket.Server.Interfaces;
using System;

namespace Larnix.Socket.Server;

internal class SecretProvider
{
    public long RunId { get; } = RandUtils.SecureLong();
    public long Secret { get; }
    public RsaFullKey Rsa { get; }
    public string Authcode { get; }

    private readonly ISecretRepository _secrets;

    public SecretProvider(ISecretRepository secrets)
    {
        _secrets = secrets;

        Secret = ProduceServerSecret();
        Rsa = ProduceRsaKey();
        Authcode = ProduceAuthcode();
    }

    private long ProduceServerSecret()
    {
        try
        {
            string? str = _secrets.ReadSecret(SocketInfo.PathServerSecret)
                ?? throw new();

            return long.Parse(str);
        }
        catch (Exception)
        {
            long secret = RandUtils.SecureLong();

            _secrets.StoreSecret(
                key: SocketInfo.PathServerSecret,
                secret: secret.ToString()
                );

            return secret;
        }
    }

    private RsaFullKey ProduceRsaKey()
    {
        try
        {
            string str = _secrets.ReadSecret(SocketInfo.PathPrivateKey)
                ?? throw new();

            byte[] keyBytes = Convert.FromBase64String(str);
            return new RsaFullKey(keyBytes);
        }
        catch (Exception)
        {
            RsaFullKey rsa = RsaFullKey.Generate();

            _secrets.StoreSecret(
                key: SocketInfo.PathPrivateKey,
                secret: Convert.ToBase64String(rsa.Export())
                );

            return rsa;
        }
    }

    private string ProduceAuthcode()
    {
        return new Authcode(
            ((RsaPublicKey)Rsa).Export(),
            Secret).ToString();
    }

    public Credentials CreateCredentials(in FixedString32 nickname, in FixedString64 password, long challengeId)
    {
        return new Credentials(
            nickname: nickname,
            password: password,
            serverSecret: Secret,
            runId: RunId,
            timestamp: Timestamp.Now(),
            challengeId: challengeId
            );
    }

    public bool CheckGlobalCredentials(in Credentials credentials)
    {
        return credentials.ServerSecret == Secret
            && credentials.RunId == RunId
            && Timestamp.IsWithin(credentials.Timestamp);
    }
}

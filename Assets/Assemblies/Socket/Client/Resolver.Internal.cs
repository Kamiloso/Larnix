#nullable enable
using Larnix.Core.Serialization;
using Larnix.Socket.Payload.Packets;
using Larnix.Socket.Payload.Structs;
using Larnix.Socket.Client.Records;
using Larnix.Socket.Security;
using Larnix.Socket.Security.Keys;
using System.Threading.Tasks;
using ServerInfoStruct = Larnix.Socket.Payload.Structs.ServerInfo;

namespace Larnix.Socket.Client;

public static partial class Resolver
{
    private static async Task<ResolveAnswer<A_ServerInfo>> _DownloadServerInfoAsync(
        ServerDiscovery discovery, bool ignoreCache = false)
    {
        var (address, authcode, nickname) = discovery;

        ServerIdentity identity = discovery.ToServerIdentity();

        if (!ignoreCache && LocalCache.TryGetFullInfo(discovery, out A_ServerInfo cached))
        {
            return cached;
        }

        var prompt = new P_ServerInfo(nickname);
        var n_answer = await Prompter.PromptAsync<P_ServerInfo, A_ServerInfo>(address, prompt, null);

        if (n_answer == null)
        {
            return ResolveError.PromptFailed;
        }

        var answer = n_answer.Value;

        byte[] keyBytes = answer.Info.RsaPublicKey.Bytes264();
        if (!Authcode.VerifyPublicKey(keyBytes, authcode))
        {
            return ResolveError.PublicKeyInvalid;
        }

        LocalCache.Received(discovery, answer);

        return answer;
    }

    private static async Task<ResolveAnswer<bool>> _TryLoginUniversal(
        FullLoginData fullLogin, bool isRegistration, bool ignoreCache = false)
    {
        var (address, authcode, nickname, password) = fullLogin;

        ServerDiscovery discovery = fullLogin.ToServerDiscovery();
        ServerIdentity identity = fullLogin.ToServerIdentity();

        ResolveAnswer<A_ServerInfo> recv = await _DownloadServerInfoAsync(discovery, ignoreCache);
        if (recv.Error != ResolveError.None)
        {
            return recv.Error;
        }

        A_ServerInfo ainfo = recv.Result;
        ServerInfoStruct info = ainfo.Info;

        if (isRegistration != ainfo.FreeUserSlot())
        {
            return ResolveError.LoginNotAllowed;
        }

        FixedRsaPublic keyStruct = ainfo.Info.RsaPublicKey;
        using var rsa = KeyRsa.FromPublicStruct(keyStruct);

        long serverSecret = Authcode.GetSecretFromAuthCode(authcode);
        long timestamp = LocalCache.TryGetTimestamp(identity, out long ts) ? ts : 0;

        Credentials credentials = new(
            nickname: nickname,
            password: password,
            serverSecret: serverSecret,
            challengeId: ainfo.ChallengeId,
            timestamp: timestamp,
            runId: info.RunId
            );

        FixedString64? newPassword = (fullLogin as PasswordChangeData)?.NewPassword;

        var prompt = newPassword.HasValue
            ? P_LoginTry.AsPasswordChange(credentials, newPassword.Value)
            : P_LoginTry.AsLogin(credentials);

        var n_answer = await Prompter.PromptAsync<P_LoginTry, A_LoginTry>(address, prompt, rsa);

        LocalCache.RemoveWhere(disc => disc == discovery); // challengeId may have changed

        if (!n_answer.HasValue)
        {
            return ResolveError.PromptFailed;
        }

        var answer = n_answer.Value;

        return (bool)answer.Success;
    }
}

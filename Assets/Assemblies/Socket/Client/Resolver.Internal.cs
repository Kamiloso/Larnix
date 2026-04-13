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
        try
        {
            var (address, authcode, nickname) = discovery;

            ServerIdentity identity = discovery.ToServerIdentity();

            if (!ignoreCache && _cache.TryGet(discovery, out A_ServerInfo cached))
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

            byte[] keyBytes = answer.Info.RsaPublicKey.Bytes264;
            if (!Authcode.VerifyPublicKey(keyBytes, authcode))
            {
                return ResolveError.PublicKeyInvalid;
            }

            _timestamps.Update(identity, answer.Info.Timestamp);
            _cache.Update(discovery, answer);

            return answer;
        }
        catch
        {
            return ResolveError.Exception;
        }
    }

    private static async Task<ResolveAnswer<bool>> _TryLoginUniversal(
        FullLoginData fullLogin, bool isRegistration)
    {
        try
        {
            var (address, authcode, nickname, password) = fullLogin;

            ServerDiscovery discovery = fullLogin.ToServerDiscovery();

            ResolveAnswer<A_ServerInfo> recv = await _DownloadServerInfoAsync(discovery);
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

            byte[] keyBytes = ainfo.Info.RsaPublicKey.Bytes264;
            using KeyRSA rsa = new(keyBytes);

            long serverSecret = Authcode.GetSecretFromAuthCode(authcode);
            long timestamp = _timestamps.Get(fullLogin.ToServerIdentity());

            Credentials credentials = new(
                nickname: nickname,
                password: password,
                serverSecret: serverSecret,
                challengeId: ainfo.ChallengeId,
                timestamp: timestamp,
                runId: info.RunId
                );

            FixedString64? newPassword = (fullLogin as PasswordChangeData)?.NewPassword ?? null;

            var prompt = new P_LoginTry(credentials, newPassword);
            var n_answer = await Prompter.PromptAsync<P_LoginTry, A_LoginTry>(address, prompt, rsa);

            _cache.Remove(discovery); // challengeId may have changed

            if (!n_answer.HasValue)
            {
                return ResolveError.PromptFailed;
            }

            var answer = n_answer.Value;

            return answer.Success;
        }
        catch
        {
            return ResolveError.Exception;
        }
    }
}

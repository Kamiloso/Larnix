#nullable enable
using Larnix.Core.Serialization;
using Larnix.Socket.Client.Records;
using Larnix.Socket.Client.Services;
using Larnix.Socket.Payload.Packets;
using Larnix.Socket.Payload.Structs;
using Larnix.Socket.Security;
using Larnix.Socket.Security.Encryption;
using System.Threading.Tasks;
using ServerInfo = Larnix.Socket.Client.Records.ServerInfo;
using ServerInfoStruct = Larnix.Socket.Payload.Structs.ServerInfo;

namespace Larnix.Socket.Client;

public enum ResolveError
{
    None,
    ResolveFailed,
    PromptFailed,
    InvalidResponse,
    PublicKeyInvalid,
    LoginNotAllowed,
}

public record ResolveAnswer<T>(T? Result, ResolveError Error)
{
    public static explicit operator T?(ResolveAnswer<T> answer) => answer.Result;
    public static implicit operator ResolveAnswer<T>(T? result) => new(result, ResolveError.None);
    public static implicit operator ResolveAnswer<T>(ResolveError error) => new(default, error);
}

public static partial class Resolver
{
    public static async Task<ResolveAnswer<ServerInfo>> DownloadServerInfoAsync(
        ServerDiscovery discovery, bool ignoreCache = false)
    {
        var (address, _, _) = discovery;

        ResolveAnswer<A_ServerInfo> recv = await _DownloadServerInfoAsync(discovery, ignoreCache);
        if (recv.Error != ResolveError.None)
        {
            return recv.Error;
        }

        A_ServerInfo ainfo = recv.Result;
        ServerInfoStruct info = ainfo.Info;

        return ServerInfo.FromStruct(address, info);
    }

    public static async Task<ResolveAnswer<bool>> UserExistsAsync(
        ServerDiscovery discovery, bool ignoreCache = false)
    {
        ResolveAnswer<A_ServerInfo> recv = await _DownloadServerInfoAsync(discovery, ignoreCache);
        if (recv.Error != ResolveError.None)
        {
            return recv.Error;
        }

        A_ServerInfo ainfo = recv.Result;

        return ainfo.UserExists();
    }

    public static async Task<ResolveAnswer<bool>> TryLoginAsync(
        FullLoginData fullLogin, bool ignoreCache = false)
    {
        return await _TryLoginUniversal(fullLogin, false, ignoreCache);
    }

    public static async Task<ResolveAnswer<bool>> TryRegisterAsync(
        FullLoginData fullLogin, bool ignoreCache = false)
    {
        return await _TryLoginUniversal(fullLogin, true, ignoreCache);
    }

    public static async Task<ResolveAnswer<bool>> TryChangePasswordAsync(
        PasswordChangeData passwordChange, bool ignoreCache = false)
    {
        return await _TryLoginUniversal(passwordChange, false, ignoreCache);
    }

    internal static async Task<ResolveAnswer<EntryTicket>> TryGetEntryTicketAsync(
        ServerDiscovery discovery, bool ignoreCache = false)
    {
        var (_, authcode, _) = discovery;

        ResolveAnswer<A_ServerInfo> recv = await _DownloadServerInfoAsync(discovery, ignoreCache);
        if (recv.Error != ResolveError.None)
        {
            return recv.Error;
        }

        A_ServerInfo ainfo = recv.Result;
        ServerInfoStruct info = ainfo.Info;

        return new EntryTicket(
            ServerSecret: new Authcode(authcode).ExtractSecret(),
            ChallengeId: ainfo.ChallengeId,
            Timestamp: info.Timestamp,
            RunId: info.RunId,
            RsaPublicKey: info.RsaPublicKey
            );
    }

    private static async Task<ResolveAnswer<A_ServerInfo>> _DownloadServerInfoAsync(
        ServerDiscovery discovery, bool ignoreCache = false)
    {
        var (address, authcode, nickname) = discovery;

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

        FixedRsaPublic keyStruct = answer.Info.RsaPublicKey;
        byte[] keyBytes = keyStruct.GetKey().Export();

        if (!new Authcode(authcode).Verify(keyBytes))
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
        RsaPublicKey rsa = keyStruct.GetKey();

        long serverSecret = new Authcode(authcode).ExtractSecret();
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

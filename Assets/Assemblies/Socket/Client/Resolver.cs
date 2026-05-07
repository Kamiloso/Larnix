#nullable enable
using System.Threading.Tasks;
using Larnix.Socket.Payload.Packets;
using Larnix.Socket.Client.Records;
using Larnix.Socket.Security;
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
            ServerSecret: Authcode.GetSecretFromAuthCode(authcode),
            ChallengeId: ainfo.ChallengeId,
            Timestamp: info.Timestamp,
            RunId: info.RunId,
            RsaPublicKey: info.RsaPublicKey
            );
    }
}

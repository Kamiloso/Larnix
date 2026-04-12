#nullable enable
using System.Threading.Tasks;
using Larnix.Socket.Requests.Cache;
using Larnix.Socket.Payload.Packets;
using Larnix.Socket.Helpers.Records;
using Larnix.Socket.Security;
using ServerInfoStruct = Larnix.Socket.Payload.Structs.ServerInfo;

namespace Larnix.Socket.Requests;

public enum ResolveError
{
    None,
    ResolveFailed,
    PromptFailed,
    InvalidResponse,
    PublicKeyInvalid,
    LoginNotAllowed,
    Exception
}

public record ResolveAnswer<T>(T? Result, ResolveError Error)
{
    public static explicit operator T?(ResolveAnswer<T> answer) => answer.Result;
    public static implicit operator ResolveAnswer<T>(T? result) => new(result, ResolveError.None);
    public static implicit operator ResolveAnswer<T>(ResolveError error) => new(default, error);
}

public static partial class Resolver
{
    private static readonly Cacher<ServerDiscovery, A_ServerInfo> _cache = new();
    private static readonly Timestamps<ServerIdentity> _timestamps = new();

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

        return ServerInfo.FromStruct(address, ainfo.Info);
    }

    public static async Task<ResolveAnswer<bool>> UserExistsAsync(
        ServerDiscovery discovery)
    {
        ResolveAnswer<A_ServerInfo> recv = await _DownloadServerInfoAsync(discovery);
        if (recv.Error != ResolveError.None)
        {
            return recv.Error;
        }

        A_ServerInfo answer = recv.Result;
        return answer.UserExists();
    }

    public static async Task<ResolveAnswer<bool>> TryLoginAsync(
        FullLoginData fullLogin)
    {
        return await _TryLoginUniversal(fullLogin, false);
    }

    public static async Task<ResolveAnswer<bool>> TryRegisterAsync(
        FullLoginData fullLogin)
    {
        return await _TryLoginUniversal(fullLogin, true);
    }

    public static async Task<ResolveAnswer<bool>> TryChangePasswordAsync(
        PasswordChangeData passwordChange)
    {
        return await _TryLoginUniversal(passwordChange, false);
    }

    internal static async Task<ResolveAnswer<EntryTicket>> TryGetEntryTicketAsync(
        ServerDiscovery discovery)
    {
        var (_, authcode, _) = discovery;

        ResolveAnswer<A_ServerInfo> recv = await _DownloadServerInfoAsync(discovery);
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

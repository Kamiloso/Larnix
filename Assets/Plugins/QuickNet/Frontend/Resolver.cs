using QuickNet.Channel;
using QuickNet.Channel.Cmds;
using QuickNet.Processing;
using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Threading.Tasks;

namespace QuickNet.Frontend
{
    public enum ResolverError
    {
        None,
        ResolveFailed,
        PromptFailed,
        InvalidResponse,
        PublicKeyInvalid,
        LoginNotAllowed,
        Exception
    }

    internal class EntryTicket
    {
        internal long ChallengeID;
        internal byte[] PublicKeyRSA;
    }

    public static class Resolver
    {
        public static async Task<IPEndPoint> ResolveStringAsync(string address)
        {
            if (address == null) return null;

            if (address.EndsWith(']') || !address.Contains(':'))
                address += ":27682";

            if (address.Count(c => c == ':') >= 2 && !address.StartsWith("[") && !address.EndsWith("]"))
                address = '[' + address + "]:27682";

            string iface = null;
            int p1 = address.IndexOf('%');
            if (p1 != -1)
            {
                int p2 = p1 + 1;
                while (p2 < address.Length && char.IsDigit(address[p2])) p2++;
                iface = address.Substring(p1 + 1, p2 - p1 - 1);
                address = address.Remove(p1, p2 - p1);
            }

            try
            {
                var uri = new Uri($"udp://{address}");
                var ipAddresses = await Dns.GetHostAddressesAsync(uri.Host);
                if (iface != null) ipAddresses[0].ScopeId = int.Parse(iface);
                return new IPEndPoint(ipAddresses[0], uri.Port);
            }
            catch
            {
                return null;
            }
        }

        public static async Task<(A_ServerInfo info, ResolverError error)> DownloadServerInfoAsync(
            string address, string authcode, string nickname, bool ignoreCache = false)
        {
            try
            {
                if (!ignoreCache && Cacher.TryGetInfo(authcode, nickname, out var cached))
                    return (cached, ResolverError.None);

                var prompt = new P_ServerInfo(nickname);
                var packet = await Prompter.PromptAsync<A_ServerInfo>(address, prompt, timeoutSeconds: 3);
                if (packet == null) return (null, ResolverError.PromptFailed);

                if (!Authcode.VerifyPublicKey(packet.PublicKey, authcode))
                    return (null, ResolverError.PublicKeyInvalid);

                Timestamp.SetServerTimestamp(await ResolveStringAsync(address), packet.Timestamp);
                Cacher.AddInfo(authcode, nickname, packet);

                return (packet, ResolverError.None);
            }
            catch
            {
                return (null, ResolverError.Exception);
            }
        }

        public static async Task<(bool? hasAccount, ResolverError error)> HasAccountAsync(string address, string authcode, string nickname)
        {
            var (info, error) = await DownloadServerInfoAsync(address, authcode, nickname);
            if (info == null) return (null, error);
            return (info.ChallengeID != 0, ResolverError.None);
        }

        public static async Task<(bool? success, ResolverError error)> TryLoginAsync(string address, string authcode, string nickname, string password)
        {
            return await _TryLoginUniversal(address, authcode, nickname, password, false);
        }

        public static async Task<(bool? success, ResolverError error)> TryRegisterAsync(string address, string authcode, string nickname, string password)
        {
            return await _TryLoginUniversal(address, authcode, nickname, password, true);
        }

        public static async Task<(bool? success, ResolverError error)> TryChangePasswordAsync(string address, string authcode, string nickname, string password, string newPassword)
        {
            return await _TryLoginUniversal(address, authcode, nickname, password, false, newPassword);
        }

        private static async Task<(bool? success, ResolverError error)> _TryLoginUniversal(string address, string authcode, string nickname, string password, bool isRegistration, string newPassword = null)
        {
            try
            {
                var (info, err) = await DownloadServerInfoAsync(address, authcode, nickname);
                if (info == null) return (null, err);

                if (!isRegistration && info.ChallengeID == 0)
                    return (null, ResolverError.LoginNotAllowed);
                if (isRegistration && info.ChallengeID != 0)
                    return (null, ResolverError.LoginNotAllowed);

                using RSA rsa = KeyObtainer.PublicBytesToKey(info.PublicKey);
                long serverSecret = Authcode.GetSecretFromAuthCode(authcode);
                long timestamp = Timestamp.GetServerTimestamp(await ResolveStringAsync(address));

                var prompt = new P_LoginTry(nickname, password, serverSecret, info.ChallengeID, timestamp, newPassword);
                var packet = await Prompter.PromptAsync<A_LoginTry>(address, prompt, timeoutSeconds: 3, publicKeyRSA: rsa);

                if (packet == null) return (null, ResolverError.PromptFailed);

                if (packet.Success)
                    Cacher.IncrementChallengeIDs(authcode, nickname, isRegistration ? 2 : 1);

                return (packet.Code == 1, ResolverError.None);
            }
            catch
            {
                return (null, ResolverError.Exception);
            }
        }

        internal static async Task<(EntryTicket ticket, ResolverError error)> GetEntryTicketAsync(string address, string authcode, string nickname)
        {
            var (info, error) = await DownloadServerInfoAsync(address, authcode, nickname);
            if (info == null) return (null, error);

            return (new EntryTicket
            {
                ChallengeID = info.ChallengeID,
                PublicKeyRSA = info.PublicKey
            }, ResolverError.None);
        }
    }
}

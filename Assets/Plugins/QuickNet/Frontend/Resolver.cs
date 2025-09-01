using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using QuickNet.Channel;
using QuickNet.Channel.Cmds;
using System.Threading.Tasks;
using QuickNet.Processing;

namespace QuickNet.Frontend
{
    public class EntryTicket
    {
        public long ChallengeID;
        public byte[] PublicKeyRSA;
    }

    public static class Resolver
    {
        public static IPEndPoint ResolveStringSync(string address)
        {
            if (address == null)
                return null;

            if (address.EndsWith(']') || !address.Contains(':'))
                address += ":27682";

            if (address.Count(c => c == ':') >= 2 && !address.StartsWith("[") && !address.EndsWith("]"))
                address = '[' + address + "]:27682";

            string interface_str = null;

            int p1 = address.IndexOf('%');
            if (p1 != -1)
            {
                int p2 = p1 + 1;
                while (p2 < address.Length && char.IsDigit(address[p2])) p2++;

                interface_str = address.Substring(p1 + 1, p2 - p1 - 1);
                address = address.Remove(p1, p2 - p1);
            }

            try
            {
                var uri = new Uri($"udp://{address}");
                var ipAddresses = Dns.GetHostAddresses(uri.Host);
                if(interface_str != null) ipAddresses[0].ScopeId = int.Parse(interface_str);
                return new IPEndPoint(ipAddresses[0], uri.Port);
            }
            catch
            {
                return null;
            }
        }

        public static Task<(A_ServerInfo info, bool publicKeyProblems)> DownloadServerInfoAsync(string address, string authcode, string nickname)
        {
            return Task.Run(() =>
            {
                return (_DownloadServerInfo(address, authcode, nickname, true, out bool keyProblems), keyProblems);
            });
        }

        public static Task<bool?> HasAccountAsync(string address, string authcode, string nickname)
        {
            return Task.Run(() =>
            {
                A_ServerInfo info = _DownloadServerInfo(address, authcode, nickname);
                return (bool?)(info == null ? null : info.ChallengeID == 0);
            });
        }

        public static Task<bool?> TryLoginAsync(string address, string authcode, string nickname, string password)
        {
            return Task.Run(() =>
            {
                A_LoginTry login = _TryLoginWrap(address, authcode, nickname, password, false);
                return (bool?)(login == null ? null : login.Code == 1);
            });
        }

        public static Task<bool?> TryRegisterAsync(string address, string authcode, string nickname, string password)
        {
            return Task.Run(() =>
            {
                A_LoginTry login = _TryLoginWrap(address, authcode, nickname, password, true);
                return (bool?)(login == null ? null : login.Code == 1);
            });
        }

        public static Task<bool?> TryChangePasswordAsync(string address, string authcode, string nickname, string oldPassword, string newPassword)
        {
            return Task.Run(() =>
            {
                A_LoginTry login = _TryLoginWrap(address, authcode, nickname, oldPassword, false, newPassword);
                return (bool?)(login == null ? null : login.Code == 1);
            });
        }

        internal static Task<EntryTicket> GetEntryTicketAsync(string address, string authcode, string nickname)
        {
            return Task.Run(() =>
            {
                A_ServerInfo info = _DownloadServerInfo(address, authcode, nickname);
                return new EntryTicket
                {
                    ChallengeID = info?.ChallengeID ?? 0,
                    PublicKeyRSA = info?.PublicKey ?? new byte[256 + 8],
                };
            });
        }

        private static A_ServerInfo _DownloadServerInfo(string address, string authcode, string nickname, bool ignoreCache = false)
        {
            return _DownloadServerInfo(address, authcode, nickname, ignoreCache, out var bin);
        }
        private static A_ServerInfo _DownloadServerInfo(string address, string authcode, string nickname, bool ignoreCache, out bool keyProblems)
        {
            keyProblems = false;

            if (!ignoreCache && Cacher.TryGetInfo(address, authcode, nickname, out var info))
            {
                return info;
            }

            Packet prompt;
            try
            {
                prompt = new P_ServerInfo(nickname);
            }
            catch (Exception ex)
            {
                QuickNet.Debug.LogError(ex.Message);
                return null;
            }

            Prompter prompter = _WaitForPrompter(new Prompter(address, prompt));

            if (prompter.State == Prompter.PrompterState.Ready)
            {
                if (Payload.TryConstructPayload<A_ServerInfo>(prompter.AnswerPacket, out var answer))
                {
                    if (!Authcode.VerifyPublicKey(answer.PublicKey, authcode))
                    {
                        keyProblems = true;
                        return null; // drop unsafe servers (remove this line and welcome scammers)
                    }

                    Timestamp.SetServerTimestamp(prompter.GetEndPoint(), answer.Timestamp);
                    Cacher.AddInfo(address, authcode, nickname, answer);
                    return answer;
                }
            }

            return null;
        }

        private static A_LoginTry _TryLoginWrap(string address, string authcode, string nickname, string password, bool isRegistration, string optionalNewPassword = null)
        {
            A_ServerInfo info = _DownloadServerInfo(address, authcode, nickname);

            long serverSecret = Authcode.GetSecretFromAuthCode(authcode);
            long challengeID = info.ChallengeID;
            byte[] publicKey = info.PublicKey;

            if (!isRegistration && challengeID == 0)
                return null; // login must have challengeID != 0

            if (isRegistration && challengeID != 0)
                return null; // registration must have challengeID == 0

            using RSA rsa = KeyObtainer.PublicBytesToKey(publicKey);
            long timestamp = Timestamp.GetServerTimestamp(ResolveStringSync(address));

            Prompter prompter = _WaitForPrompter(new Prompter(address, new P_LoginTry(
                nickname, password, serverSecret, challengeID, timestamp, optionalNewPassword),
                rsa));

            if (prompter.State == Prompter.PrompterState.Ready)
            {
                if (Payload.TryConstructPayload<A_LoginTry>(prompter.AnswerPacket, out var answer))
                {
                    if (answer.Success)
                        Cacher.IncrementChallengeIDs(address, authcode, nickname, isRegistration ? 2 : 1);

                    return answer;
                }
            }
            
            return null;
        }

        private static Prompter _WaitForPrompter(Prompter prompter)
        {
            while (true)
            {
                if (prompter.State != Prompter.PrompterState.Waiting)
                    break;

                System.Threading.Thread.Sleep(100);
                prompter.Tick(0.1f);
            }

            prompter.Clean();
            return prompter;
        }
    }
}

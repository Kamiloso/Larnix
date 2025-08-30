using QuickNet.Commands;
using QuickNet.Processing;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using QuickNet.Channel;

namespace QuickNet.Backend
{
    internal class ManagerNCN
    {
        private readonly QuickServer Server;
        private float MinuteCounter = 0f;

        private readonly List<IEnumerator> coroutines = new();

        private readonly Dictionary<InternetID, uint> LoginAmount = new();
        private const uint MAX_HASHING_AMOUNT = 6; // max hashing amount per minute per client
        private const uint MAX_PARALLEL_HASHINGS = 6; // max hashing amount at the time globally
        private uint CurrentHashingAmount = 0;

        internal ManagerNCN(QuickServer server)
        {
            Server = server;
        }

        internal void Tick(float deltaTime)
        {
            RunEveryTick();

            MinuteCounter += deltaTime;
            if (MinuteCounter > 60f)
            {
                MinuteCounter %= 60f;
                RunEveryMinute();
            }
        }

        private void RunEveryTick()
        {
            for (int i = coroutines.Count - 1; i >= 0; i--)
            {
                var coroutine = coroutines[i];
                bool hasNext = coroutine.MoveNext();
                if (!hasNext)
                {
                    coroutines.RemoveAt(i);
                }
            }
        }

        private void RunEveryMinute()
        {
            LoginAmount.Clear();
        }

        private void StartCoroutine(IEnumerator coroutine)
        {
            coroutines.Add(coroutine);
        }

        internal void ProcessNCN(IPEndPoint remoteEP, uint ncnID, Packet packet)
        {
            if (packet == null)
                return;

            if (packet.ID == CmdID.P_ServerInfo)
            {
                P_ServerInfo prompt = new P_ServerInfo(packet);
                if (prompt.HasProblems) return;

                string checkNickname = prompt.Nickname;
                byte[] publicKey = KeyObtainer.KeyToPublicBytes(Server.KeyRSA);

                A_ServerInfo answer = new A_ServerInfo(
                    publicKey,
                    Server.CountPlayers(),
                    Server.MaxClients,
                    Server.GameVersion,
                    Server.UserManager.GetChallengeID(checkNickname),
                    Timestamp.GetTimestamp(),
                    Server.UserText1,
                    Server.UserText2,
                    Server.UserText3
                    );
                if (answer.HasProblems)
                    throw new Exception("Error making server info answer.");

                Server.SendNCN(remoteEP, ncnID, answer.GetPacket());
            }

            else if (packet.ID == CmdID.P_LoginTry)
            {
                P_LoginTry prompt = new P_LoginTry(packet);
                if (prompt.HasProblems) return;

                Action<byte> AnswerLoginTry = (byte b) =>
                {
                    A_LoginTry answer = new A_LoginTry(b);
                    if (!answer.HasProblems)
                    {
                        Server.SendNCN(remoteEP, ncnID, answer.GetPacket());
                    }
                };

                StartCoroutine(
                    LoginCoroutine(remoteEP, prompt.Nickname, prompt.Password,
                    prompt.ServerSecret, prompt.ChallengeID, prompt.Timestamp, prompt.Password != prompt.NewPassword,
                    ExecuteSuccess: () =>
                    {
                        Server.UserManager.IncrementChallengeID(prompt.Nickname);

                        if (prompt.Password == prompt.NewPassword) // normal login try
                        {
                            AnswerLoginTry(1);
                        }
                        else // change password mode
                        {
                            StartCoroutine(ChangePasswordCoroutine(prompt.Nickname, prompt.NewPassword, () => AnswerLoginTry(1)));
                        }
                    },
                    ExecuteFailed: () =>
                    {
                        AnswerLoginTry(0);
                    }
                    ));
            }
        }

        internal void TryLogin(IPEndPoint remoteEP, string username, string password, long serverSecret, long challengeID, long timestamp)
        {
            StartCoroutine(
                LoginCoroutine(remoteEP, username, password, serverSecret, challengeID, timestamp, false,
                ExecuteSuccess: () =>
                {
                    Server.UserManager.IncrementChallengeID(username);
                    Server.LoginAccept(remoteEP);
                },
                ExecuteFailed: () =>
                {
                    Server.LoginDeny(remoteEP);
                }
                ));
        }

        private IEnumerator LoginCoroutine(
            IPEndPoint remoteEP, string username, string password, long serverSecret, long challengeID, long timestamp, bool isPasswordChange,
            Action ExecuteSuccess, Action ExecuteFailed
            )
        {
            long timeNow = Timestamp.GetTimestamp();
            uint hashCost = (uint)(isPasswordChange ? 2 : 1);

            if (
                serverSecret != Server.Secret || // wrong server secret
                !Timestamp.InTimestamp(timestamp) || // login message is outdated
                CurrentHashingAmount + hashCost > MAX_PARALLEL_HASHINGS || // hashing slots full
                Server.ReservedNicknames.Contains(username) || // reserved nickname, cannot be used
                (password == QuickServer.LoopbackOnlyPassword && !IPAddress.IsLoopback(remoteEP.Address))) // loopback-only password
            {
                ExecuteFailed();
                yield break;
            }

            InternetID internetID = new InternetID(remoteEP.Address);
            if (!LoginAmount.ContainsKey(internetID))
                LoginAmount[internetID] = 0;

            if (LoginAmount[internetID] + hashCost > MAX_HASHING_AMOUNT || // too many hashing tries in this minute
                challengeID != Server.UserManager.GetChallengeID(username)) // wrong challengeID
            {
                ExecuteFailed();
                yield break;
            }

            if (isPasswordChange) // no-matter-what incrementation
                LoginAmount[internetID]++;

            if (Server.UserManager.UserExists(username))
            {
                string password_hash = Server.UserManager.GetPasswordHash(username);

                if (!Hasher.InCache(password, password_hash))
                    LoginAmount[internetID]++; // hash will be calculated

                Task<bool> verifyTask = Hasher.VerifyPasswordAsync(password, password_hash);

                CurrentHashingAmount++;
                while (!verifyTask.IsCompleted) yield return null;
                CurrentHashingAmount--;

                if (verifyTask.Result)
                {
                    ExecuteSuccess();
                    yield break; // good password
                }
                else
                {
                    ExecuteFailed();
                    yield break; // wrong password
                }
            }
            else
            {
                Task<string> hashing = Hasher.HashPasswordAsync(password);

                LoginAmount[internetID]++; // hash will be calculated

                CurrentHashingAmount++;
                while (!hashing.IsCompleted) yield return null;
                CurrentHashingAmount--;

                string hashed_password = hashing.Result;
                Server.UserManager.AddUser(username, hashed_password);
                QuickNet.Debug.Log($"{username} created an account from {remoteEP}");

                ExecuteSuccess();
                yield break; // created new account
            }
        }

        private IEnumerator ChangePasswordCoroutine(string username, string newPassword, Action Finally)
        {
            Task<string> hashing = Hasher.HashPasswordAsync(newPassword);

            CurrentHashingAmount++;
            while (!hashing.IsCompleted) yield return null;
            CurrentHashingAmount--;

            string hash = hashing.Result;
            Server.UserManager.ChangePassword(username, hash);

            Finally();
        }
    }
}

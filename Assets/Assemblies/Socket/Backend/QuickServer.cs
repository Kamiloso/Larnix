using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Net;
using Larnix.Socket.Packets;
using System.Threading.Tasks;
using System;
using Larnix.Core.Utils;
using Larnix.Socket.Security.Keys;
using Larnix.Socket.Channel;
using Larnix.Socket.Structs;
using Larnix.Socket.Security;

namespace Larnix.Socket.Backend
{
    public class QuickServer : IDisposable
    {
        public readonly ushort Port, MaxClients;
        public readonly bool IsLoopback;
        public readonly string Motd;
        public readonly string HostUser;

        public readonly long Secret;
        public readonly string Authcode;

        internal readonly long RunID;

        private bool InitializedMasks = false;
        internal int MaskIPv4 = 32;
        internal int MaskIPv6 = 56;

        public readonly UserManager UserManager;
        public readonly KeyRSA _keyRSA;

        private readonly TripleSocket _udpSocket;
        internal readonly HashSet<string> ReservedNicknames = new();

        private readonly Dictionary<string, Connection> _connections = new();
        private readonly Dictionary<IPEndPoint, Connection> _connectionsByEndPoint = new();
        private readonly Dictionary<IPEndPoint, PreLoginBuffer> _preLoginBuffers = new();

        private readonly Dictionary<CmdID, Action<HeaderSpan, string>> Subscriptions = new();

        private const float CON_RESET_TIME = 1f; // seconds
        private float timeToResetCon = CON_RESET_TIME;
        private uint MAX_CON = 5; // limit heavy packets per IP mask (internetID)
        private uint MAX_GLOBAL_CON = 50; // limit heavy packets globally
        private Dictionary<InternetID, uint> recentConCount = new();
        private uint globalConCount = 0;

        private const float KEEP_ALIVE_PERIOD = 5f;
        private float relayTimeToKeepAlive = KEEP_ALIVE_PERIOD;

        private bool _disposed;

        public QuickServer(ushort port, ushort maxClients, bool isLoopback, string dataPath, IUserAPI userAPI, string motd, string hostUser)
        {
            if (!Validation.IsGoodText<String256>(motd))
                throw new ArgumentException("Wrong motd format! Cannot be larger than 128 characters or end with NULL (0x00).");

            if(!Validation.IsGoodText<String32>(hostUser))
                throw new ArgumentException("Wrong host nickname format! Cannot be larger than 16 characters or end with NULL (0x00).");

            // managed objects
            _udpSocket = new TripleSocket(port, isLoopback);
            _keyRSA = new KeyRSA(dataPath, "private_key.pem");
            UserManager = new UserManager(userAPI);

            // other configuration
            Port = _udpSocket.Port;
            MaxClients = maxClients;
            IsLoopback = isLoopback;
            Secret = Security.Authcode.ObtainSecret(dataPath, "server_secret.txt");
            Authcode = Security.Authcode.ProduceAuthCodeRSA(_keyRSA.ExportPublicKey(), Secret);
            Motd = motd;
            HostUser = hostUser;

            // run random
            RunID = Common.GetSecureLong();
        }

        public void ConfigureMasks(int maskIPv4, int maskIPv6)
        {
            if (InitializedMasks)
                throw new InvalidOperationException("Masks were already initialized! " +
                    "This method should be called from directly after QuickServer constructor.");

            InitializedMasks = true;
            MaskIPv4 = maskIPv4;
            MaskIPv6 = maskIPv6;
        }

        public async Task<ushort?> ConfigureRelay(string relayAddress)
        {
            return await _udpSocket.StartRelay(relayAddress);
        }

        private void RememberConnection(string nickname, Connection conn)
        {
            _connections.Add(nickname, conn);
            _connectionsByEndPoint.Add(conn.EndPoint, conn);
        }

        private void ForgetConnection(string nickname)
        {
            IPEndPoint endPoint = _connections[nickname].EndPoint;
            _connections.Remove(nickname);
            _connectionsByEndPoint.Remove(endPoint);
        }

        private bool CanAcceptSYN(IPEndPoint endPoint, string nickname)
        {
            if (_connections.Count >= MaxClients)
                return false;

            return !_connections.Any(kvp =>
                kvp.Key == nickname || kvp.Value.EndPoint.Equals(endPoint));
        }

        internal void LoginAccept(IPEndPoint target)
        {
            if (!_preLoginBuffers.ContainsKey(target))
                throw new InvalidOperationException("Couldn't find login request to accept.");

            PreLoginBuffer preLoginBuffer = _preLoginBuffers[target];
            AllowConnection allowConnection = preLoginBuffer.AllowConnection;
            string nickname = allowConnection.Nickname;

            if (!CanAcceptSYN(target, nickname))
            {
                LoginDeny(target);
                return;
            }

            KeyAES localAES = new KeyAES(allowConnection.KeyAES);
            Connection connection = new Connection(
                _udpSocket,
                target,
                localAES
                );

            RememberConnection(nickname, connection);
            _preLoginBuffers.Remove(target);

            bool hasSyn = true;
            byte[] bytes;
            while ((bytes = preLoginBuffer.Pop()) != null)
            {
                connection.PushFromWeb(bytes, hasSyn);
                hasSyn = false;
            }
        }

        internal void LoginDeny(IPEndPoint remoteEP)
        {
            if (!_preLoginBuffers.ContainsKey(remoteEP))
                throw new InvalidOperationException("Couldn't find login request to deny.");

            _preLoginBuffers.Remove(remoteEP);
        }

        public void ServerTick(float deltaTime)
        {
            // Block initializing masks
            InitializedMasks = true;

            // Tick database
            Tick(deltaTime);

            // Interpret bytes from socket
            while (_udpSocket.TryReceive(out var item))
            {
                IPEndPoint remoteEP = item.target;
                byte[] bytes = item.data;

                InterpretBytes(remoteEP, bytes);
            }

            // Tick every connection
            foreach (var conn in _connections.Values)
            {
                conn.Tick(deltaTime);
            }

            // Randomize client order
            List<string> nicknames = _connections.Keys
                .OrderBy(x => Common.Rand().Next())
                .ToList();

            // Receive packets
            Queue<(HeaderSpan, string)> packetList = new();
            foreach (string nickname in nicknames)
            {
                Connection conn = _connections[nickname];
                Queue<HeaderSpan> packets = conn.Receive();
                while (packets.Count > 0)
                    packetList.Enqueue((packets.Dequeue(), nickname));
            }

            // Remove dead connections
            foreach (string nickname in nicknames)
            {
                Connection conn = _connections[nickname];
                if (conn.IsDead)
                {
                    // add finishing message
                    Payload packet = new Stop(0);
                    packetList.Enqueue((new HeaderSpan(packet.Serialize(default)), nickname));

                    // reset player slots
                    ForgetConnection(nickname);
                }
            }

            // Reset RSA counter
            timeToResetCon -= deltaTime;
            if(timeToResetCon < 0)
            {
                timeToResetCon = CON_RESET_TIME;
                recentConCount.Clear();
                globalConCount = 0;
            }

            // Relay keep alive
            relayTimeToKeepAlive -= deltaTime;
            if (relayTimeToKeepAlive < 0)
            {
                relayTimeToKeepAlive = KEEP_ALIVE_PERIOD;
                _udpSocket?.KeepAlive();
            }

            // Interpret packets
            while (packetList.Count > 0)
            {
                var element = packetList.Dequeue();
                HeaderSpan headerSpan = element.Item1;
                string owner = element.Item2;

                if (Subscriptions.TryGetValue(headerSpan.ID, out var Execute))
                    Execute(headerSpan, owner);
            }
        }

        private void InterpretBytes(IPEndPoint target, byte[] bytes)
        {
            if (PayloadBox.TryDeserialize(bytes, null, out var header))
            {
                InternetID internetID = new InternetID(
                    target.Address,
                    target.AddressFamily == AddressFamily.InterNetwork ? MaskIPv4 : MaskIPv6
                    );
                uint conCount = recentConCount.ContainsKey(internetID) ? recentConCount[internetID] : 0;

                // Limit packets with specific flags
                if (header.HasFlag(PacketFlag.SYN) ||
                    header.HasFlag(PacketFlag.RSA) ||
                    header.HasFlag(PacketFlag.NCN))
                {
                    if (conCount < MAX_CON) recentConCount[internetID] = ++conCount;
                    else return;

                    if (globalConCount < MAX_GLOBAL_CON) globalConCount++;
                    else return;
                }

                if (header.HasFlag(PacketFlag.RSA)) // encrypted with RSA
                {
                    // Deserialize with decryption and serialize without encryption
                    if (_keyRSA != null && PayloadBox.TryDeserialize(bytes, _keyRSA, out var decrypted))
                    {
                        bytes = decrypted.Serialize(KeyEmpty.GetInstance());
                    }
                    else return;
                }

                if (header.HasFlag(PacketFlag.NCN)) // fast question, fast answer
                {
                    // Check packet type and answer properly
                    if (PayloadBox.TryDeserialize(bytes, KeyEmpty.GetInstance(), out var ncnBox))
                    {
                        ProcessNCN(target, ncnBox.SeqNum, new HeaderSpan(ncnBox.Bytes));
                    }
                }
                else
                {
                    if (header.HasFlag(PacketFlag.SYN)) // start connection
                    {
                        if (PayloadBox.TryDeserialize(bytes, KeyEmpty.GetInstance(), out var synBox))
                        {
                            if (Payload.TryConstructPayload<AllowConnection>(synBox.Bytes, out var allowcon))
                            {
                                string nickname = allowcon.Nickname;
                                string password = allowcon.Password;
                                long serverSecret = allowcon.ServerSecret;
                                long challengeID = allowcon.ChallengeID;
                                long timestamp = allowcon.Timestamp;
                                long runID = allowcon.RunID;

                                if (!_preLoginBuffers.ContainsKey(target))
                                {
                                    if (CanAcceptSYN(target, nickname))
                                    {
                                        PreLoginBuffer preLoginBuffer = new PreLoginBuffer(allowcon);
                                        preLoginBuffer.Push(bytes);
                                        _preLoginBuffers.Add(target, preLoginBuffer);

                                        TryLogin(target, nickname, password, serverSecret, challengeID, timestamp, runID);
                                    }
                                }
                            }
                        }
                    }
                    else // receive connection packet
                    {
                        if (_preLoginBuffers.TryGetValue(target, out var preBuffer))
                        {
                            preBuffer.Push(bytes);
                        }
                        else
                        {
                            if (_connectionsByEndPoint.TryGetValue(target, out var conn))
                            {
                                conn.PushFromWeb(bytes);
                            }
                        }
                    }
                }
            }
        }

        public void Subscribe<T>(Action<T, string> InterpretPacket) where T : Payload, new()
        {
            CmdID ID = Payload.CmdID<T>();
            Subscriptions[ID] = (HeaderSpan headerSpan, string owner) =>
            {
                if (Payload.TryConstructPayload<T>(headerSpan, out var message))
                {
                    InterpretPacket(message, owner);
                }
            };
        }

        public void Send(string nickname, Payload packet, bool safemode = true)
        {
            if (_connections.TryGetValue(nickname, out var conn))
                conn.Send(packet, safemode);
        }

        public void Broadcast(Payload packet, bool safemode = true)
        {
            foreach (var conn in _connections.Values.ToList())
            {
                conn.Send(packet, safemode);
            }
        }

        public void FinishConnection(string nickname)
        {
            if (_connections.TryGetValue(nickname, out var conn))
            {
                conn.FinishConnection();
                ForgetConnection(nickname);
            }
        }

        public void FinishAllConnections()
        {
            foreach (string nickname in _connections.Keys.ToList())
            {
                FinishConnection(nickname);
            }
        }

        internal void SendNCN(IPEndPoint target, int ncnID, Payload packet)
        {
            PayloadBox safeAnswer = new PayloadBox(
                seqNum: ncnID,
                ackNum: 0,
                flags: (byte)PacketFlag.NCN,
                payload: packet
                );

            byte[] payload = safeAnswer.Serialize(KeyEmpty.GetInstance()); // answer always plaintext
            _udpSocket.Send(target, payload);
        }

        public ushort CountPlayers()
        {
            return (ushort)_connections.Count;
        }

        public IPEndPoint GetClientEndPoint(string nickname)
        {
            if (_connections.TryGetValue(nickname, out var conn))
                return conn.EndPoint;
            return null;
        }

        public float GetPing(string nickname)
        {
            if (_connections.TryGetValue(nickname, out var conn))
                return conn.AvgRTT * 1000f;
            return 0.0f;
        }

        private float MinuteCounter = 0f;

        private readonly List<IEnumerator> coroutines = new();

        private readonly Dictionary<InternetID, uint> LoginAmount = new();
        private const uint MAX_HASHING_AMOUNT = 6; // max hashing amount per minute per client
        private const uint MAX_PARALLEL_HASHINGS = 6; // max hashing amount at the time globally
        private uint CurrentHashingAmount = 0;

        public void Tick(float deltaTime)
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

        internal void ProcessNCN(IPEndPoint remoteEP, int ncnID, HeaderSpan headerSpan)
        {
            if (Payload.TryConstructPayload<P_ServerInfo>(headerSpan, out var infoask))
            {
                string checkNickname = infoask.Nickname;
                KeyRSA publicKey = _keyRSA;

                SendNCN(remoteEP, ncnID, new A_ServerInfo(
                    publicKey.ExportPublicKey(),
                    CountPlayers(),
                    MaxClients,
                    Core.Version.Current,
                    UserManager.GetChallengeID(checkNickname),
                    Timestamp.GetTimestamp(),
                    RunID,
                    Motd,
                    HostUser
                    ));
            }

            else if (Payload.TryConstructPayload<P_LoginTry>(headerSpan, out var logtry))
            {
                Action<bool> AnswerLoginTry = (bool success) =>
                {
                    SendNCN(remoteEP, ncnID, new A_LoginTry(success));
                };

                string nickname = logtry.Nickname;
                string password = logtry.Password;
                string newPassword = logtry.NewPassword;

                StartCoroutine(
                    LoginCoroutine(remoteEP, nickname, password,
                    logtry.ServerSecret, logtry.ChallengeID, logtry.Timestamp, logtry.RunID,
                    password != newPassword,
                    ExecuteSuccess: () =>
                    {
                        UserManager.IncrementChallengeID(logtry.Nickname);

                        if (password == newPassword) // normal login try
                        {
                            AnswerLoginTry(true);
                        }
                        else // change password mode
                        {
                            StartCoroutine(ChangePasswordCoroutine(nickname, newPassword, () => AnswerLoginTry(true)));
                        }
                    },
                    ExecuteFailed: () =>
                    {
                        AnswerLoginTry(false);
                    }
                    ));
            }
        }

        public void TryLogin(IPEndPoint remoteEP, string username, string password,
            long serverSecret, long challengeID, long timestamp, long runID)
        {
            StartCoroutine(
                LoginCoroutine(remoteEP, username, password, serverSecret, challengeID, timestamp, runID, false,
                ExecuteSuccess: () =>
                {
                    UserManager.IncrementChallengeID(username);
                    LoginAccept(remoteEP);
                },
                ExecuteFailed: () =>
                {
                    LoginDeny(remoteEP);
                }
                ));
        }

        private IEnumerator LoginCoroutine(
            IPEndPoint remoteEP, string username, string password,
            long serverSecret, long challengeID, long timestamp, long runID,
            bool isPasswordChange, Action ExecuteSuccess, Action ExecuteFailed
            )
        {
            long timeNow = Timestamp.GetTimestamp();
            uint hashCost = (uint)(isPasswordChange ? 2 : 1);
            bool isLoopback = IPAddress.IsLoopback(remoteEP.Address);

            if (
                serverSecret != Secret || // wrong server secret
                runID != RunID || // wrong runID
                !Timestamp.InTimestamp(timestamp) || // login message is outdated
                CurrentHashingAmount + hashCost > MAX_PARALLEL_HASHINGS || // hashing slots full
                (username == Common.LoopbackOnlyNickname && !isLoopback) || // loopback-only nickname
                (password == Common.LoopbackOnlyPassword && !isLoopback)) // loopback-only password
            {
                ExecuteFailed();
                yield break;
            }

            InternetID internetID = new InternetID(
                remoteEP.Address,
                remoteEP.AddressFamily == AddressFamily.InterNetwork ?
                    MaskIPv4 : MaskIPv6
                );

            if (!LoginAmount.ContainsKey(internetID))
                LoginAmount[internetID] = 0;

            if (LoginAmount[internetID] + hashCost > MAX_HASHING_AMOUNT || // too many hashing tries in this minute
                challengeID != UserManager.GetChallengeID(username)) // wrong challengeID
            {
                ExecuteFailed();
                yield break;
            }

            if (isPasswordChange) // no-matter-what incrementation
                LoginAmount[internetID]++;

            if (UserManager.UserExists(username))
            {
                string password_hash = UserManager.GetPasswordHash(username);

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
                UserManager.AddUser(username, hashed_password);
                Core.Debug.Log($"{username} created an account from {remoteEP}");

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
            UserManager.SetPasswordHash(username, hash);

            Finally();
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _disposed = true;

                FinishAllConnections();
                _udpSocket?.Dispose();
                _keyRSA?.Dispose();
            }
        }
    }
}

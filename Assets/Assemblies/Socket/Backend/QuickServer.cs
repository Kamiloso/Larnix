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
using Larnix.Socket.Security;
using Larnix.Socket.Packets.Control;
using Larnix.Socket.Channel.Networking;
using Larnix.Socket.Channel.Helpers;

namespace Larnix.Socket.Backend
{
    public class QuickServer : IDisposable
    {
        // --- Server properties ---
        public readonly long Secret;
        public readonly string Authcode;
        public readonly long RunID;

        // --- Public classes ---
        public readonly QuickConfig Config;
        public readonly UserManager UserManager;

        // --- Private classes ---
        private readonly KeyRSA _keyRSA;
        private readonly TripleSocket _udpSocket;

        // --- Timers ---
        private readonly AckTimer _packetLimiterReset;
        private readonly AckTimer _relayKeepAlive;

        private readonly Dictionary<string, Connection> _connections = new();
        private readonly Dictionary<IPEndPoint, Connection> _connectionsByEndPoint = new();
        private readonly Dictionary<IPEndPoint, PreLoginBuffer> _preLoginBuffers = new();

        private uint HEAVY_LOCAL_LIMIT = 5;
        private Dictionary<InternetID, uint> localHeavyCounter = new();
        
        private uint HEAVY_GLOBAL_LIMIT = 50;
        private uint _globalHeavyCounter = 0;

        private const uint HASH_LOCAL_LIMIT = 6; // per minute per client
        private readonly Dictionary<InternetID, uint> _localHashCounter = new();

        private const uint MAX_PARALLEL_HASHINGS = 6; // at the time globally
        private uint _currentHashings = 0;

        private float MinuteCounter = 0f;
        private readonly List<IEnumerator> coroutines = new();

        // --- Other ---
        private readonly Dictionary<CmdID, Action<HeaderSpan, string>> Subscriptions = new();
        private bool _disposed;

        public QuickServer(QuickConfig serverConfig)
        {
            // classes
            _udpSocket = new TripleSocket(serverConfig.Port, serverConfig.IsLoopback);
            _keyRSA = new KeyRSA(serverConfig.DataPath, "private_key.pem");

            // public API
            UserManager = new UserManager(serverConfig.UserAPI);
            Config = serverConfig.WithPort(_udpSocket.Port);

            Secret = Security.Authcode.ObtainSecret(serverConfig.DataPath, "server_secret.txt");
            Authcode = Security.Authcode.ProduceAuthCodeRSA(_keyRSA.ExportPublicKey(), Secret);
            RunID = Common.GetSecureLong();

            // cyclic objects
            _packetLimiterReset = new AckTimer(1f, () =>
            {
                localHeavyCounter.Clear();
                _globalHeavyCounter = 0;
            });

            _relayKeepAlive = new AckTimer(5f, () =>
            {
                _udpSocket?.KeepAlive();
            });
        }

        public async Task<ushort?> ConfigureRelayAsync(string relayAddress)
        {
            return await _udpSocket.StartRelayAsync(relayAddress);
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
            if (_connections.Count >= Config.MaxClients)
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
            Connection connection = Connection.CreateServer(_udpSocket, target, localAES);

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
            // Tick database
            RunEveryTick();

            MinuteCounter += deltaTime;
            if (MinuteCounter > 60f)
            {
                MinuteCounter %= 60f;
                RunEveryMinute();
            }

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

            // AckTimers tick
            _packetLimiterReset.Tick(deltaTime);
            _relayKeepAlive.Tick(deltaTime);

            // Interpret packets
            while (packetList.Count > 0)
            {
                var element = packetList.Dequeue();
                HeaderSpan headerSpan = element.Item1;
                string owner = element.Item2;

                if (Subscriptions.TryGetValue(headerSpan.ID, out var Execute))
                {
                    Execute(headerSpan, owner);
                }
            }
        }

        private void InterpretBytes(IPEndPoint target, byte[] bytes)
        {
            if (PayloadBox.TryDeserializeOnlyHeader(bytes, out var header))
            {
                InternetID internetID = new InternetID(
                    target.Address,
                    target.AddressFamily == AddressFamily.InterNetwork ? Config.MaskIPv4 : Config.MaskIPv6
                    );
                uint conCount = localHeavyCounter.ContainsKey(internetID) ? localHeavyCounter[internetID] : 0;

                // Limit packets with specific flags
                if (header.HasFlag(PacketFlag.SYN) ||
                    header.HasFlag(PacketFlag.RSA) ||
                    header.HasFlag(PacketFlag.NCN))
                {
                    if (conCount < HEAVY_LOCAL_LIMIT) localHeavyCounter[internetID] = ++conCount;
                    else return;

                    if (_globalHeavyCounter < HEAVY_GLOBAL_LIMIT) _globalHeavyCounter++;
                    else return;
                }

                if (header.HasFlag(PacketFlag.RSA)) // encrypted with RSA
                {
                    // Deserialize with decryption and serialize without encryption
                    if (PayloadBox.TryDeserialize(bytes, _keyRSA, out var decrypted))
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
                        if (PayloadBox.TryDeserialize(bytes, KeyEmpty.GetInstance(), out var synBox) &&
                            Payload.TryConstructPayload<AllowConnection>(synBox.Bytes, out var allowcon) &&
                            !_preLoginBuffers.ContainsKey(target) &&
                            CanAcceptSYN(target, allowcon.Nickname))
                        {
                            PreLoginBuffer preLoginBuffer = new PreLoginBuffer(allowcon);
                            preLoginBuffer.Push(bytes);
                            _preLoginBuffers.Add(target, preLoginBuffer);

                            TryLogin(target,
                                username: allowcon.Nickname,
                                password: allowcon.Password,
                                serverSecret: allowcon.ServerSecret,
                                challengeID: allowcon.ChallengeID,
                                timestamp: allowcon.Timestamp,
                                runID: allowcon.RunID);
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
                conn.Send(packet, safemode);
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
            _localHashCounter.Clear();
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

                SendNCN(remoteEP, ncnID, new A_ServerInfo(
                    publicKey: _keyRSA.ExportPublicKey(),
                    currentPlayers: CountPlayers(),
                    maxPlayers: Config.MaxClients,
                    gameVersion: Core.Version.Current,
                    challengeID: UserManager.GetChallengeID(checkNickname),
                    timestamp: Timestamp.GetTimestamp(),
                    runID: RunID,
                    motd: Config.Motd,
                    hostUser: Config.HostUser
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
                _currentHashings + hashCost > MAX_PARALLEL_HASHINGS || // hashing slots full
                (username == Common.LoopbackOnlyNickname && !isLoopback) || // loopback-only nickname
                (password == Common.LoopbackOnlyPassword && !isLoopback)) // loopback-only password
            {
                ExecuteFailed();
                yield break;
            }

            InternetID internetID = new InternetID(
                remoteEP.Address,
                remoteEP.AddressFamily == AddressFamily.InterNetwork ?
                    Config.MaskIPv4 : Config.MaskIPv6
                );

            if (!_localHashCounter.ContainsKey(internetID))
                _localHashCounter[internetID] = 0;

            if (_localHashCounter[internetID] + hashCost > HASH_LOCAL_LIMIT || // too many hashing tries in this minute
                challengeID != UserManager.GetChallengeID(username)) // wrong challengeID
            {
                ExecuteFailed();
                yield break;
            }

            if (isPasswordChange) // no-matter-what incrementation
                _localHashCounter[internetID]++;

            if (UserManager.UserExists(username))
            {
                string password_hash = UserManager.GetPasswordHash(username);

                if (!Hasher.InCache(password, password_hash))
                    _localHashCounter[internetID]++; // hash will be calculated

                Task<bool> verifyTask = Hasher.VerifyPasswordAsync(password, password_hash);

                _currentHashings++;
                while (!verifyTask.IsCompleted) yield return null;
                _currentHashings--;

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

                _localHashCounter[internetID]++; // hash will be calculated

                _currentHashings++;
                while (!hashing.IsCompleted) yield return null;
                _currentHashings--;

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

            _currentHashings++;
            while (!hashing.IsCompleted) yield return null;
            _currentHashings--;

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
                _udpSocket.Dispose();
                _keyRSA.Dispose();
            }
        }
    }
}

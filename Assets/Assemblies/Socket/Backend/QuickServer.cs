using System.Collections;
using System.Collections.Generic;
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
using Larnix.Socket.Helpers;

namespace Larnix.Socket.Backend
{
    public class QuickServer : IDisposable
    {
        // --- Server properties ---
        public string Authcode { get; init; }
        internal long Secret { get; init; }
        internal long RunID { get; init; }
        public ushort PlayerCount => (ushort)_connDict.Count;

        // --- Public classes ---
        public QuickConfig Config => _config;
        public UserManager UserManager => _userManager;

        // --- Private classes ---
        private readonly KeyRSA _keyRSA;
        private readonly TripleSocket _udpSocket;
        private readonly ConnDict _connDict;
        private readonly CoroutineRunner _coroutineRunner;
        private readonly UserManager _userManager;
        private readonly QuickConfig _config;

        // --- Timers ---
        private readonly CycleTimer _packetLimiterReset;
        private readonly CycleTimer _registerReset;
        private readonly CycleTimer _hashCountClean;
        private readonly CycleTimer _relayKeepAlive; // for udp socket

        // --- Limiters ---
        private readonly TrafficLimiter<InternetID> _heavyPacketLimiter = new(
            maxTrafficLocal: 5, // max heavy packets (SYN, RSA, NCN) per 1 second
            maxTrafficGlobal: 50 // max heavy packets globally per 1 second
            );
        private readonly TrafficLimiter<InternetID> _registerLimiter = new(
            maxTrafficLocal: 6, // max registrations per internet ID per 6 hours
            ulong.MaxValue // no global limit
            );
        private readonly WeirdConcurrentLimiter<InternetID> _hashLimiter = new(
            maxConcurrentLocal: 6, // max hashings per internet ID per minute
            maxConcurrentGlobal: 6 // max hashings globally concurrently
            );

        // --- Other ---
        private enum LoginMode { Discovery, Establishment, PasswordChange }
        private readonly Dictionary<CmdID, Action<HeaderSpan, string>> Subscriptions = new();
        private bool _disposed;

        public QuickServer(QuickConfig serverConfig)
        {
            // Managed classes
            _keyRSA = new KeyRSA(serverConfig.DataPath, "private_key.pem"); // 1
            _udpSocket = new TripleSocket(serverConfig.Port, serverConfig.IsLoopback); // 2
            _connDict = new ConnDict(_udpSocket, this, serverConfig.MaxClients); // 3
            _coroutineRunner = new CoroutineRunner(); // 4

            // Classes
            _userManager = new UserManager(serverConfig.UserAPI);
            _config = serverConfig.WithPort(_udpSocket.Port);

            // Constant properties
            Secret = Security.Authcode.ObtainSecret(serverConfig.DataPath, "server_secret.txt");
            Authcode = Security.Authcode.ProduceAuthCodeRSA(_keyRSA.ExportPublicKey(), Secret);
            RunID = Common.GetSecureLong();

            // Cyclic objects
            _packetLimiterReset = new CycleTimer(1f /* 1 SECOND */, () => _heavyPacketLimiter.Reset());
            _hashCountClean = new CycleTimer(60f /* 1 MINUTE */, () => _hashLimiter.ResetLocal());
            _relayKeepAlive = new CycleTimer(5f /* 5 SECONDS */, () => _udpSocket.KeepAlive());
            _registerReset = new CycleTimer(60f * 60f * 6f /* 6 HOURS */, () => _registerLimiter.Reset());
        }

        public async Task<ushort?> ConfigureRelayAsync(string relayAddress)
        {
            return await _udpSocket.StartRelayAsync(relayAddress);
        }

        public void ServerTick(float deltaTime)
        {
            // AckTimers tick
            _packetLimiterReset.Tick(deltaTime);
            _relayKeepAlive.Tick(deltaTime);
            _hashCountClean.Tick(deltaTime);
            _registerReset.Tick(deltaTime);

            // Interpret bytes from socket
            while (_udpSocket.TryReceive(out var item))
            {
                IPEndPoint remoteEP = item.target;
                byte[] bytes = item.data;

                InterpretBytes(remoteEP, bytes);
            }

            // Tick & Receive from connections
            Queue<PacketPair> packets = _connDict.TickAndReceive(deltaTime);
            while (packets.Count > 0)
            {
                var element = packets.Dequeue();
                var packet = element.Packet;
                string owner = element.Owner;

                if (Subscriptions.TryGetValue(packet.ID, out var Execute))
                {
                    Execute(packet, owner);
                }
            }

            // Tick coroutines
            _coroutineRunner.Tick();
        }

        private void InterpretBytes(IPEndPoint target, byte[] bytes)
        {
            if (PayloadBox.TryDeserializeHeader(bytes, out var header))
            {
                InternetID internetID = MakeInternetID(target);

                // Heavy packet limiter
                if (header.HasFlag(PacketFlag.SYN) ||
                    header.HasFlag(PacketFlag.RSA) ||
                    header.HasFlag(PacketFlag.NCN))
                {
                    if (!_heavyPacketLimiter.TryIncrease(internetID))
                        return; // drop heavy packet
                }

                // Decrypt RSA
                if (header.HasFlag(PacketFlag.RSA))
                {
                    if (!PayloadBox.TryDeserialize(bytes, _keyRSA, out var decrypted))
                        return; // drop invalid RSA packet
                    
                    decrypted.UnsetFlag(PacketFlag.RSA);
                    bytes = decrypted.Serialize(KeyEmpty.GetInstance());
                }

                if (header.HasFlag(PacketFlag.NCN))
                {
                    // Non-connection packet - NCN
                    if (PayloadBox.TryDeserialize(bytes, KeyEmpty.GetInstance(), out var box))
                        ProcessNCN(target, box.SeqNum, new HeaderSpan(box.Bytes));
                }
                else
                {
                    if (header.HasFlag(PacketFlag.SYN))
                    {
                        // Start new connection
                        if (PayloadBox.TryDeserialize(bytes, KeyEmpty.GetInstance(), out var synBox) &&
                            Payload.TryConstructPayload<AllowConnection>(synBox.Bytes, out var allowcon) &&
                            _connDict.TryAddPreLogin(target, synBox))
                        {
                            P_LoginTry logtry = allowcon.ToLoginTry();
                            StartLogin(target, logtry, LoginMode.Establishment);
                        }
                    }
                    else
                    {
                        // Established connection packet
                        _connDict.EnqueueReceivedPacket(target, bytes);
                    }
                }
            }
        }

        private void ProcessNCN(IPEndPoint target, int ncnID, HeaderSpan headerSpan)
        {
            Action<Payload> SendNCN = (Payload packet) =>
            {
                PayloadBox safeAnswer = new PayloadBox(
                    seqNum: ncnID,
                    ackNum: 0,
                    flags: (byte)PacketFlag.NCN,
                    payload: packet
                    );

                // answer always as plaintext
                byte[] payload = safeAnswer.Serialize(KeyEmpty.GetInstance());
                _udpSocket.Send(target, payload);
            };

            if (Payload.TryConstructPayload<P_ServerInfo>(headerSpan, out var infoask))
            {
                string checkNickname = infoask.Nickname;
                A_ServerInfo srvInfo = MakeServerInfo(checkNickname);

                SendNCN(srvInfo);
            }
            
            else if (Payload.TryConstructPayload<P_LoginTry>(headerSpan, out var logtry))
            {
                string nickname = logtry.Nickname;
                string password = logtry.Password;
                string newPassword = logtry.NewPassword;

                Action<bool> SendAnswer = success =>
                    SendNCN(new A_LoginTry(success));

                StartLogin(target, logtry, password == newPassword ?
                    LoginMode.Discovery : LoginMode.PasswordChange,
                    SendAnswer);
            }
        }

        /// <summary>
        /// AllowConnection packet always starts the connection.
        /// Stop packet always ends the connection.
        /// Stop packet can only appear once in a returned packet queue.
        /// </summary>
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

        public bool IsActiveConnection(string nickname)
        {
            return TryGetClientEndPoint(nickname, out _);
        }

        public bool TryGetClientEndPoint(string nickname, out IPEndPoint endPoint)
        {
            IPEndPoint endPoint1 = _connDict.EndPointOf(nickname);
            if (endPoint1 != null)
            {
                endPoint = endPoint1;
                return true;
            }
            endPoint = null;
            return false;
        }

        public void Send(string nickname, Payload packet, bool safemode = true)
        {
            if (TryGetClientEndPoint(nickname, out IPEndPoint endPoint))
            {
                _connDict.SendTo(endPoint, packet, safemode);
            }
        }

        public void Broadcast(Payload packet, bool safemode = true)
        {
            _connDict.SendToAll(packet, safemode);
        }

        public float GetPing(string nickname)
        {
            if (TryGetClientEndPoint(nickname, out IPEndPoint endPoint))
            {
                Connection conn = _connDict.GetConnectionObject(endPoint);
                return conn.AvgRTT;
            }
            return 0f;
        }

        public void FinishConnectionRequest(string nickname)
        {
            if (TryGetClientEndPoint(nickname, out IPEndPoint endPoint))
            {
                _connDict.KickRequest(endPoint);
            }
        }

        private void StartLogin(IPEndPoint target, P_LoginTry logtry, LoginMode mode, object ad1 = null)
        {
            string nickname = logtry.Nickname;

            if (mode == LoginMode.Establishment)
            {
                _coroutineRunner.Start(
                    LoginCoroutine(target, logtry,
                    ExecuteSuccess: () =>
                    {
                        if (_connDict.TryPromoteConnection(target))
                            _userManager.IncrementChallengeID(nickname);
                        else
                            _connDict.DiscardIncoming(target);
                    },
                    ExecuteFailed: () =>
                    {
                        _connDict.DiscardIncoming(target);
                    }
                    ));
            }

            else if (mode == LoginMode.Discovery)
            {
                if (ad1 is not Action<bool> SendAnswer)
                    throw new ArgumentException("For Discovery mode, ad1 must be of type Action<bool>.");

                _coroutineRunner.Start(
                    LoginCoroutine(target, logtry,
                    ExecuteSuccess: () =>
                    {
                        _userManager.IncrementChallengeID(nickname);
                        SendAnswer(true);
                    },
                    ExecuteFailed: () =>
                    {
                        SendAnswer(false);
                    }
                    ));
            }

            else if (mode == LoginMode.PasswordChange)
            {
                if (ad1 is not Action<bool> SendAnswer)
                    throw new ArgumentException("For PasswordChange mode, ad1 must be of type Action<bool>.");

                string newPassword = logtry.NewPassword;

                _coroutineRunner.Start(
                    LoginCoroutine(target, logtry,
                    ExecuteSuccess: () =>
                    {
                        _userManager.IncrementChallengeID(nickname);

                        _coroutineRunner.Start(
                            ChangePasswordCoroutine(target, nickname, newPassword,
                            ExecuteSuccess: () =>
                            {
                                SendAnswer(true);
                            },
                            ExecuteFailed: () =>
                            {
                                SendAnswer(false);
                            }
                            ));
                    },
                    ExecuteFailed: () =>
                    {
                        SendAnswer(false);
                    }
                    ));
            }
        }

        private IEnumerator LoginCoroutine(IPEndPoint target, P_LoginTry logtry,
            Action ExecuteSuccess, Action ExecuteFailed)
        {
            string nickname = logtry.Nickname;
            string password = logtry.Password;
            long serverSecret = logtry.ServerSecret;
            long challengeID = logtry.ChallengeID;
            long timestamp = logtry.Timestamp;
            long runID = logtry.RunID;

            long timeNow = Timestamp.GetTimestamp();
            bool isLoopback = IPAddress.IsLoopback(target.Address);

            InternetID internetID = MakeInternetID(target);

            if (
                serverSecret != Secret || // wrong server secret
                runID != RunID || // wrong runID
                !Timestamp.InTimestamp(timestamp) || // login message is outdated
                (!isLoopback && nickname == Common.LOOPBACK_ONLY_NICKNAME) || // loopback-only nickname
                (!isLoopback && password == Common.LOOPBACK_ONLY_PASSWORD) || // loopback-only password
                challengeID != _userManager.GetChallengeID(nickname)) // wrong challengeID
            {
                ExecuteFailed(); // invalid login data
                yield break;
            }

            if (_userManager.UserExists(nickname))
            {
                string hashedPassword = _userManager.GetPasswordHash(nickname);

                bool limiterDone = false;
                if (Hasher.InCache(password, hashedPassword) || (limiterDone = _hashLimiter.TryIncrease(internetID)))
                {
                    Task<bool> verifyTask = Hasher.VerifyPasswordAsync(password, hashedPassword);
                    while (!verifyTask.IsCompleted) yield return null;
                    if (limiterDone) _hashLimiter.DecreaseGlobal();

                    if (verifyTask.Result)
                    {
                        ExecuteSuccess(); // correct password
                        yield break;
                    }
                    else
                    {
                        ExecuteFailed(); // wrong password
                        yield break;
                    }
                }
                else
                {
                    ExecuteFailed(); // too many hash attempts
                    yield break;
                }
            }
            else
            {
                if (_config.AllowRegistration &&
                    _hashLimiter.TryIncrease(internetID) &&
                    _registerLimiter.TryIncrease(internetID)
                    )
                {
                    Task<string> hashing = Hasher.HashPasswordAsync(password);
                    while (!hashing.IsCompleted) yield return null;
                    _hashLimiter.DecreaseGlobal();

                    string hashedPassword = hashing.Result;
                    _userManager.AddUser(nickname, hashedPassword);
                    Core.Debug.Log($"{nickname} created an account from {target}");

                    ExecuteSuccess(); // new user created
                    yield break;
                }
                else
                {
                    ExecuteFailed(); // too many hash attempts
                    yield break;
                }
            }
        }

        private IEnumerator ChangePasswordCoroutine(IPEndPoint target, string username, string newPassword,
            Action ExecuteSuccess, Action ExecuteFailed)
        {
            InternetID internetID = MakeInternetID(target);

            if (_hashLimiter.TryIncrease(internetID))
            {
                Task<string> hashing = Hasher.HashPasswordAsync(newPassword);
                while (!hashing.IsCompleted) yield return null;
                _hashLimiter.DecreaseGlobal();

                string hash = hashing.Result;
                _userManager.SetPasswordHash(username, hash);

                ExecuteSuccess(); // password changed
                yield break;
            }
            else
            {
                ExecuteFailed(); // too many hash attempts
                yield break;
            }
        }

        private A_ServerInfo MakeServerInfo(string nickname)
        {
            return new A_ServerInfo(
                publicKey: _keyRSA.ExportPublicKey(),
                currentPlayers: PlayerCount,
                maxPlayers: _config.MaxClients,
                gameVersion: Core.Version.Current,
                challengeID: _userManager.GetChallengeID(nickname),
                timestamp: Timestamp.GetTimestamp(),
                runID: RunID,
                motd: _config.Motd,
                hostUser: _config.HostUser,
                mayRegister: _config.AllowRegistration
                );
        }

        internal InternetID MakeInternetID(IPEndPoint endPoint)
        {
            return new InternetID(
                endPoint.Address,
                endPoint.AddressFamily == AddressFamily.InterNetwork ?
                    _config.MaskIPv4 : _config.MaskIPv6
                );
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _disposed = true;

                _coroutineRunner.Dispose(); // 4
                _connDict.Dispose(); // 3
                _udpSocket.Dispose(); // 2
                _keyRSA.Dispose(); // 1
            }
        }
    }
}
